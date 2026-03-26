# Réseau Routier Procédural — Document de Conception

> Villa Nova — Mars 2026

---

## Table des matières

1. [Le problème de la poule et de l'œuf (Routes ↔ POIs)](#1-le-problème-de-la-poule-et-de-lœuf)
2. [Comment les vrais jeux résolvent ce problème](#2-comment-les-vrais-jeux-résolvent-ce-problème)
3. [Algorithmes de génération de routes](#3-algorithmes-de-génération-de-routes)
4. [Hiérarchie routière](#4-hiérarchie-routière)
5. [Forme de la ville et tissu urbain](#5-forme-de-la-ville-et-tissu-urbain)
6. [Stratégie d'implémentation pour Villa Nova](#6-stratégie-dimplémentation-pour-villa-nova)

---

## 1. Le problème de la poule et de l'œuf

### Situation actuelle

Le pipeline exécute `CityGenerator` → `RoadGenerator` séquentiellement.
La règle `NEAR_ROAD` dans `POIData` ne peut jamais être satisfaite car aucune route
n'existe quand les POIs sont placés. Et inversement, les routes ne peuvent pas être
"attirées" par des POIs qui n'existent pas encore.

```
Pipeline actuel :
  MapGenerator ──→ CityGenerator (POIs) ──→ RoadGenerator (routes)
                         ↑                          ↑
                  Pas de routes ici         POIs existent, mais
                  → NEAR_ROAD échoue       les routes ne font que
                                           connecter point-à-point
```

### Le cœur du problème

Routes et POIs ont une **dépendance mutuelle** :
- Les POIs veulent être près des routes (commerce, accessibilité)
- Les routes veulent passer par les POIs (destinations, carrefours)
- Le réseau routier doit créer du **volume** (blocs, quartiers), pas juste des chemins

---

## 2. Comment les vrais jeux résolvent ce problème

### a) Seed & Grow (Dwarf Fortress, RimWorld)

Poser d'abord un **squelette routier minimal** (routes principales vers les villes
voisines), puis placer les POIs avec `NEAR_ROAD` satisfaisable, puis densifier le
réseau. Les routes attirent les POIs, qui à leur tour justifient de nouvelles routes
secondaires.

```
1. Squelette routier (highways)
2. POIs majeurs (attirés par les routes)
3. Routes secondaires (attirées par les POIs)
4. POIs mineurs (attirés par les routes secondaires)
5. Ruelles et remplissage
```

### b) Itératif / Simulated Annealing (Cities: Skylines interne)

Alterner entre phases de route et phases de placement en boucle.

```
Passe 1 : routes primaires
Passe 2 : POIs majeurs
Passe 3 : routes secondaires vers ces POIs
Passe 4 : POIs secondaires
Passe 5 : routes tertiaires / remplissage
```

### c) Influence Map (académique)

Définir un **champ de potentiel** sur la grille. Les routes existantes augmentent le
score des cellules voisines pour le placement de POI. Les POIs placés créent une
demande de connexion qui guide la prochaine passe de route.

C'est déjà partiellement implémenté dans le projet via `ScoreProximityToType` dans
`POIRulesValidator` et `PoiScoreJob`.

### d) Pre-plan (Medieval Dynasty, Banished)

Placer d'abord des **zones fonctionnelles**, puis générer un réseau connectant les
zones. Les routes ne précèdent pas les bâtiments ; elles sont calculées après
placement complet via **arbre couvrant minimal** (MST) ou **Steiner tree**.

---

## 3. Algorithmes de génération de routes

### a) L-Systems (Parish & Müller, 2001)

> Paper : *"Procedural Modeling of Cities"*

Grammaire formelle de réécriture. Un axiome (segment initial) est expansé par des
règles de production pour créer un réseau hiérarchique.

**Règles typiques :**
```
HIGHWAY(pos, dir, len) →
    Avancer A* de pos vers pos+dir*len
    Si len > MIN_LEN :
        programmer BRANCH(midpoint, perpendicular, len * 0.6)

BRANCH(pos, dir, len) →
    Si urbanité(pos) > 0.3 :
        Avancer A* de pos vers pos+dir*len
    Si len > MIN_BLOCK :
        programmer BRANCH(midpoint, perpendicular, len * 0.5)
```

**Deux modes de croissance :**
- **Radial/concentrique** : les highways forment des cercles concentriques avec des
  rayons partant du centre
- **Grille (Manhattan)** : les rues poussent en alignement orthogonal, modulé par du
  bruit pour l'organicité

**Avantage** : Contrôle hiérarchique naturel (chaque génération de la grammaire = un
tier de route). Résultats bien structurés.

**Inconvénient** : Complexité de la grammaire. Debugging difficile.

**Adaptation grille** : Chaque segment L-system est résolu par A* entre le point
courant et le point cible. Le `PathfindingJob` existant fait exactement ça.

---

### b) Tensor Fields (Chen et al., 2008)

Un champ tensoriel 2D est défini sur la carte. Chaque point a un tenseur qui encode
la **direction idéale** de la route. Les routes sont tracées en suivant les lignes de
flux (streamlines) de ce champ.

**Sources de tenseur :**
- **Radial** : autour du centre-ville, les routes convergent
- **Grille** : dans les zones commerciales, tenseur orthogonal uniforme
- **Hauteur** : le gradient d'élévation influence l'orientation
- **Rivière** : les routes suivent ou croisent perpendiculairement les cours d'eau

**Avantage** : Transitions naturelles entre zones grille et zones organiques.
Excellent contrôle artistique via placement de "sources".

**Inconvénient** : Plus complexe à implémenter. Nécessite streamline tracing et
logique de snapping/intersection.

**Adaptation possible** : Ajouter un tenseur par cellule comme couche de coût dans
`RoadCostCalculator` — biaiser la direction préférée du A*.

---

### c) Agent-Based (Lechner et al., 2006)

Des **agents** se déplacent sur la carte et posent des routes derrière eux.

**Règles d'un agent :**
1. Avancer dans la direction courante
2. Tourner si obstacle (eau, pente)
3. Bifurquer avec une certaine probabilité
4. S'arrêter si le segment atteint une route existante ou le bord de carte

**Types d'agents :**
- **Highway agent** : va loin, bifurque rarement, largeur importante
- **Street agent** : bifurque fréquemment, crée des blocs courts
- **Alley agent** : remplit les blocs, segments très courts

```
Agent(pos, dir, type, stepsLeft):
    Chaque pas :
        nextPos = pos + dir
        Si obstacle ou hors zone : tourner 90°
        Si cellule est déjà ROAD : STOP (connexion créée)
        Stamper ROAD sur nextPos

        Si random < branchProbability :
            Spawner Agent(pos, perpendicular(dir), subType, stepsLeft * 0.6)

        stepsLeft--
        Si stepsLeft <= 0 : STOP
```

**Avantage** : Très intuitif, facile à implémenter sur une grille, résultats
naturellement organiques. **Chaque agent peut être un Job Burst.**

**C'est l'approche recommandée en premier** pour Villa Nova.

---

### d) Voronoï / Relaxation pour districts

Générer N points de seed (POIs, intersections clés), calculer le diagramme de
Voronoï pour partitionner l'espace en districts. Les **arêtes de Voronoï = routes**.

**Adaptation grille** : Flood-fill BFS multi-source depuis chaque seed. Chaque cellule
reçoit l'ID du seed le plus proche (pondéré par coût de terrain). Les frontières
entre régions deviennent des routes.

```
Seeds = CityCenter + POIs + intersections majeures
BFS multi-source (coût pondéré par terrain)
Frontières entre régions → candidats route T2/T3
Chaque région hérite du type de son seed :
    CHURCH → quartier religieux
    MARKET → quartier commercial
```

**Avantage** : Crée naturellement des quartiers avec identité. Les routes épousent
le terrain car le BFS utilise les coûts de `RoadCostCalculator`.

---

### e) Wave Function Collapse (WFC)

Placer des tuiles compatibles selon des contraintes d'adjacence. Chaque tuile encode
un motif urbain (intersection T, virage, ligne droite, cul-de-sac, bloc de maisons).

**Avantage** : Garantit la cohérence locale.

**Limite** : Difficile d'imposer des contraintes globales (hiérarchie, connectivité).
Mieux adapté au **remplissage intra-bloc** qu'au réseau principal.

---

### f) Grid vs Organic

| Pattern | Quand l'utiliser | Comment |
|---------|------------------|---------|
| **Grille** | Centre-ville, marché, quartier planifié | Routes tous les N cellules en X/Y + bruit |
| **Organique** | Faubourgs, village ancien, périphérie | Agents avec bruit Perlin directionnel |
| **Hybride** | Transition naturelle | Grille au centre, organique en périphérie. Un champ tensoriel contrôle la transition |

---

## 4. Hiérarchie routière

| Tier | Nom | Largeur | Rôle | Algorithme |
|------|-----|---------|------|------------|
| **T1** | Highways / routes externes | `roadWidth + 1` | Connecter aux `NearCities` | A* depuis bord de carte vers `CityCenter` |
| **T2** | Artères / routes principales | `roadWidth` | Squelette interne, relier les POIs | MST des POIs + agents radiaux |
| **T3** | Rues secondaires | `roadWidth - 1` | Créer les blocs urbains | Agents perpendiculaires ou subdivision |
| **T4** | Ruelles / passages | 1 | Accès aux maisons dans les blocs | Remplissage grille locale |

**Séquence de génération :**

```
T1 d'abord : définit les axes majeurs et entrées de ville
T2 ensuite : artères radiales/concentriques center → POIs → intersections T1
T3 après   : subdivision des espaces entre T1/T2 en blocs
T4 dernier : remplissage des blocs pour le détail
```

Le `RoadGraph.EdgeType` existant a déjà `EXTERNAL`, `MAIN`, `SECONDARY`.
Il suffit d'ajouter `TERTIARY` et `ALLEY`.

---

## 5. Forme de la ville et tissu urbain

### Le problème actuel

Les routes sont des chemins point-à-point. Il n'y a pas de notion de **bloc**,
**parcelle** ou **quartier**. La ville n'a pas de forme organique.

### Approche : "Routes d'abord, blocs ensuite"

#### Étape 1 — Définir l'emprise urbaine

Utiliser un bruit radial depuis `CityCenter` (ellipse déformée par Perlin) pour
délimiter la zone "ville". Chaque cellule reçoit un score `urbanité` de 0 à 1.

```
urbanité(cell) = max(0, 1 - distance(cell, CityCenter) / maxRadius)
               * perlinNoise(cell.x * scale, cell.y * scale)

Si urbanité > 0.5 → zone urbaine dense (T3, T4)
Si urbanité > 0.2 → zone péri-urbaine (T2 seulement)
Si urbanité < 0.2 → campagne (T1 seulement)
```

Ceci donne une **forme organique** à la ville sans la limiter à un cercle parfait.

#### Étape 2 — Réseau routier dans l'emprise

Les algorithmes (agents, L-system) ne posent des routes que dans les zones avec
urbanité suffisante. Les routes T1 traversent librement ; les routes T3 ne poussent
que dans la zone urbaine.

#### Étape 3 — Extraction des blocs

Après stamping de toutes les routes, flood-fill sur les cellules non-route :

```
Pour chaque cellule PLAIN non visitée :
    BFS → composante connexe = un bloc
    Stocker blockId sur chaque cellule du bloc

Filtrer :
    Blocs < 4 cellules    → fusionner avec voisin
    Blocs > 200 cellules  → subdiviser (ajouter route T3 au milieu)
```

#### Étape 4 — Classification des blocs

Par taille, proximité au centre, adjacence à un POI :
- Bloc adjacent à CHURCH → quartier religieux
- Bloc adjacent à MARKET → quartier commercial
- Grand bloc en périphérie → résidentiel / agricole
- Petit bloc central → dense / multi-étage

#### Étape 5 — Remplissage des blocs

Chaque bloc reçoit des bâtiments selon son type. `PlaceHousesCoroutine` travaille
**par bloc** au lieu de par rayon fixe.

---

## 6. Stratégie d'implémentation pour Villa Nova

### Pipeline proposé

```
MapGenerator
  │
  ├─ UrbanBoundaryGenerator       → champ urbanité sur la grille
  │
  ├─ CityGenerator Passe 1        → POIs sans NEAR_ROAD (TownHall, Church, Well)
  │
  ├─ RoadGenerator Passe T1       → highways vers NearCities
  │
  ├─ RoadGenerator Passe T2       → artères center → POIs + radiales
  │
  ├─ CityGenerator Passe 2        → POIs avec NEAR_ROAD (Market, etc.)
  │
  ├─ RoadGenerator Passe T2b      → connecter les nouveaux POIs
  │
  ├─ RoadGenerator Passe T3       → subdivision en blocs (agents)
  │
  ├─ BlockExtractor                → flood-fill → block IDs + classification
  │
  └─ HouseGenerator                → remplissage par bloc
```

### Recommandation d'algorithme par tier

| Tier | Algorithme recommandé | Pourquoi |
|------|-----------------------|----------|
| T1 | **A* existant** | Déjà implémenté, fonctionne bien |
| T2 | **Agents + MST** | Facile à implémenter, résultats organiques |
| T3 | **Agents perpendiculaires** | Crée naturellement des blocs |
| T4 | **Grille locale par bloc** | Simple et déterministe |

### Pourquoi Agent-Based en premier

1. **Mappe directement sur la grille** `Cell[,]` existante
2. **Chaque agent = un Job Burst** (parallélisable)
3. **Utilise le A* existant** (`PathfindingJob`) pour résoudre chaque segment
4. **Intuitif à débugger** (visualiser les agents sur le `DebugRenderer`)
5. **Progression incrémentale** : peut commencer simple et enrichir les règles

### Changements code nécessaires

1. **`RoadGraph.EdgeType`** : ajouter `TERTIARY` et `ALLEY`
2. **`WorldGrid.Cell`** : ajouter `float Urbanity` et `int BlockId`
3. **`RoadGenerator`** : scinder `Generate()` en sous-passes (T1, T2, T3, T4)
4. **`GenerationPipeline`** : supporter les passes intercalées routes/POIs
5. **Nouveau `BlockExtractor`** : flood-fill + classification
6. **`RoadSettings`** : paramètres par tier (largeur, bruit, probabilité de branche)

### Priorité d'implémentation suggérée

```
Phase 1 : Pipeline multi-passes (T1 → POIs → T2 → T2b)
          → Résout le problème NEAR_ROAD
          → Effort modéré, gros impact

Phase 2 : Agents pour T3 (subdivision en blocs)
          → Donne du volume à la ville
          → Effort modéré

Phase 3 : BlockExtractor + HouseGenerator par bloc
          → Quartiers avec identité
          → Effort modéré

Phase 4 : Urbanité + forme organique
          → Belle forme de ville
          → Effort faible

Phase 5 : Tensor fields ou L-systems (optionnel)
          → Contrôle artistique fin
          → Effort élevé, bénéfice marginal
```

---

## Références

- Parish, Y. I. H., & Müller, P. (2001). *Procedural Modeling of Cities.* SIGGRAPH.
- Chen, G., et al. (2008). *Interactive Procedural Street Modeling.* SIGGRAPH.
- Lechner, T., et al. (2006). *Procedural City Modeling.* Midterm Report, MIT.
- Müller, P., et al. (2006). *Procedural Modeling of Buildings.* SIGGRAPH.
- Greuter, S., et al. (2003). *Real-time Procedural Generation of Pseudo Infinite Cities.* GRAPHITE.

