# Villa Nova

Projet Unity de génération procédurale de villes médiévales.

Description du système de génération procédurale des routes:
- Génération de routes principales à partir de villes proches (Near Cities System), mini-agent avec des règles pour l'organisme de routes
- Génération de routes secondaires à partir de POIs (Points of Interest), mini-agent avec des règles pour l'organisme de routes
- Génération de routes tertiaires à partir de bâtiments, mini-agent avec des règles pour l'organisme de routes
- Application de règles de génération pour éviter les intersections et créer des routes réalistes
- Utilisation de Unity Job System pour la génération asynchrone des routes et éviter les blocages du thread principal

---

## Structure du projet

```
Assets/
├── Content/        # Assets visuels et médias
│   ├── HouseModels/    // Modèles 3D de maisons
│   ├── Materials/      // Matériaux et shaders
│   ├── Textures/       // Textures, lightmaps, HDRIs
│   ├── Import/         // Assets importés
│   └── Shaders/        // Shaders personnalisés
│
├── Data/           # Données configurables
│   ├── Buildings/       // Config de bâtiments
│   ├── POIs/            // Config de POI
│   └── RoadAgents/      // Config d'agent de génération de routes
│
└── Scripts/        # Code source
    ├── Buildings/      // Système de génération de bâtiments
    ├── Camera/         // Système de caméra FSM
    │   ├── CameraController.cs         // Point d'entrée, inputs, FSM
    │   ├── CameraModeConfig.cs         // Config serialisable par mode
    │   └── States/
    │       ├── CameraStartState.cs     // Animation d'intro
    │       ├── CameraMainState.cs      // Vue principale
    │       ├── CameraCloseState.cs     // Vue orthographique
    │       └── CameraFreeState.cs      // Free cam FPS
    ├── Generators/       // Systèmes de génération procédurale
    ├── Grid/             // Système de grille pour la map
    ├── Jobs/             // Unity Job System pour la génération asynchrone
    ├── NearCities/       // Système de villes proches pour la génération de routes
    ├── POIs/            // Système de points d'intérêt
    ├── Renderers/       // Systèmes de rendu debug ou 3D mesh map
    ├── Roads/           // Système de génération de routes
    └── Toolbox/           // Outils génériques pour le projet
```

---

# Controls

| Input   | Action                     |
|---------|----------------------------|
| `Space` | Nouvelle génération        |
| `C`     | Afficher le render Mesh 3D |
| `V`     | Afficher le render debug   |

## Caméra

La caméra est pilotable avec 3 états :

| État    | Touche | Description                                  |
|---------|--------|----------------------------------------------|
| `Main`  | `1`    | Orbite autour du centre de la carte          |
| `Close` | `2`    | Orbite rapprochée, projection orthographique |
| `Free`  | `3`    | Vol FPS                                      |

### Main Cam & Close Cam

| Input                                              | Action                               |
|----------------------------------------------------|--------------------------------------|
| Clic gauche (maintenu) et déplacement de la souris | Tourner autour du centre de la carte |
| Molette                                            | Zoomer avant/arrière                 |

### Free Cam

| Input                 | Action                            |
|-----------------------|-----------------------------------|
| `Z / S / Q / D`       | Déplacement horizontal            |
| `A`                   | Monter verticalement              |
| `E`                   | Descendre verticalement           |
| Clic droit (maintenu) | Regarder autour                   |
| Molette               | Ajuster la vitesse de déplacement |
| `1 / 2 / 3`           | Changer de mode caméra            |

---

## Dépendances

- **Unity** 6000.3.10f1
