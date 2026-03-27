# Plan d'Implémentation — Système Urbain Procédural

> Villa Nova — Mars 2026
>
> Complète le document [RoadSystemDesign.md](RoadSystemDesign.md) avec les détails
> d'implémentation : structures de données, signatures, pseudocode, fichiers, et
> phases de développement.

---

## Table des matières

1. [Choix de l'algorithme](#1-choix-de-lalgorithme)
2. [Pipeline multi-passes](#2-pipeline-multi-passes)
3. [Modifications des structures de données](#3-modifications-des-structures-de-données)
4. [Champ d'urbanité](#4-champ-durbanité)
5. [Génération routière Agent-Based](#5-génération-routière-agent-based)
6. [Extraction des blocs et classification](#6-extraction-des-blocs-et-classification)
7. [Génération des maisons multi-cellules](#7-génération-des-maisons-multi-cellules)
8. [Intégration C# Jobs / Burst](#8-intégration-c-jobs--burst)
9. [Liste des fichiers](#9-liste-des-fichiers)
10. [Phases d'implémentation](#10-phases-dimplémentation)

---

## 1. Choix de l'algorithme

### Algorithme retenu : Agent-Based (Lechner et al., 2006)

Après analyse des candidats (voir `RoadSystemDesign.md` §3), l'approche **Agent-Based**
est retenue comme algorithme principal pour les raisons suivantes :

| Critère                     | Agent-Based | L-System  | Tensor Fields  | Voronoï |
|-----------------------------|:-----------:|:---------:|:--------------:|:-------:|
| Mappe sur grille `Cell[,]`  |  ✅ Direct   | ✅ Via A*  | ⚠️ Streamlines |  ✅ BFS  |
| Complexité d'implémentation |   Faible    |  Moyenne  |     Élevée     | Moyenne |
| Résultats organiques        |  ✅ Naturel  | ⚠️ Rigide |  ✅ Excellent   | ⚠️ Géo  |
| Hiérarchie routière         | ✅ Par type  | ✅ Par gen |   ⚠️ Manuel    |    ❌    |
| Parallélisable (Burst)      | ⚠️ Partiel  |     ❌     |       ✅        |   ⚠️    |
| Debugging visuel            |  ✅ Trivial  |    ⚠️     |       ❌        |    ✅    |
| Compatibilité existante     |   ✅ Tout    |   ✅ A*    |  ⚠️ Refactor   |  ✅ BFS  |

**Stratégie hybride :**

- **T1 (Highways)** → A* existant (`PathfindingProcessJob`), inchangé.
- **T2 (Artères)** → A* pour les connexions center→POI, agents radiaux pour la couverture.
- **T3 (Rues)** → Agents perpendiculaires aux T2, créent les blocs.
- **T4 (Ruelles)** → Agents courts dans les gros blocs restants.

L'approche Voronoï/BFS est utilisée en complément pour l'**extraction des blocs**
(flood-fill sur les cellules non-route après la génération routière).

---

## 2. Pipeline multi-passes

### Pipeline actuel (problématique)

```
MapGenerator → CityGenerator (tous les POIs) → RoadGenerator (toutes les routes)
```

### Nouveau pipeline

```
 Étape  │ Système                  │ Description
────────┼──────────────────────────┼───────────────────────────────────────────────
   1    │ MapGenerator             │ Terrain, eau, rivières (inchangé)
   2    │ UrbanityGenerator        │ Champ urbanité 0-1 sur chaque cellule
   3    │ CityGenerator Passe 1   │ POIs d'ancrage (TownHall, Church, Well)
   4    │ RoadGenerator T1         │ Highways vers NearCities (A*)
   5    │ RoadGenerator T2a        │ Artères center → POIs ancrés (A*)
   6    │ CityGenerator Passe 2   │ POIs road-dependent (Market, etc.)
   7    │ RoadGenerator T2b        │ Artères → nouveaux POIs + radiaux (agents)
   8    │ RoadGenerator T3         │ Rues secondaires, subdivision (agents)
   9    │ BlockExtractor           │ Flood-fill, classification des blocs
  10    │ RoadGenerator T4         │ Ruelles dans les gros blocs (agents)
  11    │ HouseGenerator           │ Remplissage des blocs avec des maisons
  12    │ TerrainRenderer          │ Rebuild du mesh final
```

### Interface de pipeline

```csharp
// Nouvelle interface, plus fine que IGenerator
public interface IPipelineStep
{
    string      Name { get; }
    IEnumerator Execute(WorldGrid _grid);
}
```

`GenerationPipeline` itère une `List<IPipelineStep>` au lieu de `List<IGenerator>`.
Les anciens `IGenerator` restent compatibles via un adaptateur ou en implémentant
aussi `IPipelineStep`.

### Découpage de CityGenerator

```csharp
public class CityGenerator
{
    // Passe 1 : POIs sans règle NEAR_ROAD
    public IEnumerator GenerateAnchorPOIs(WorldGrid _grid);

    // Passe 2 : POIs avec au moins une règle NEAR_ROAD
    public IEnumerator GenerateRoadDependentPOIs(WorldGrid _grid);
}
```

La distinction se fait via une nouvelle propriété sur `POIData` :

```csharp
public bool RequiresRoad => System.Array.Exists(Rules, _r => _r.rule == POIRule.NEAR_ROAD);
```

### Découpage de RoadGenerator

```csharp
public class RoadGenerator
{
    public IEnumerator GenerateT1(WorldGrid _grid);  // Highways via A*
    public IEnumerator GenerateT2(WorldGrid _grid);  // Artères via A* + agents
    public IEnumerator GenerateT3(WorldGrid _grid);  // Rues via agents
    public IEnumerator GenerateT4(WorldGrid _grid);  // Ruelles via agents
}
```

---

## 3. Modifications des structures de données

### WorldGrid.Cell — 3 nouveaux champs

```csharp
public struct Cell
{
    public CellType   Type;
    public Vector2Int Position;
    public float      Height;
    public POIData    POI;
    public bool       IsOccupied;

    // NOUVEAUX
    public float Urbanity;   // 0-1, écrit par UrbanityGenerator
    public int   BlockId;    // -1 = pas de bloc, écrit par BlockExtractor
    public byte  RoadTier;   // 0 = pas de route, 1-4, écrit par StampRoad
}
```

Initialisation : `Urbanity = 0`, `BlockId = -1`, `RoadTier = 0`.

### WorldGrid — nouvelle collection

```csharp
public List<BlockData> Blocks = new();  // Rempli par BlockExtractor
```

### RoadGraph.EdgeType — 2 nouvelles valeurs

```csharp
public enum EdgeType
{
    EXTERNAL,    // T1 : highways vers NearCities
    MAIN,        // T2 : artères principales
    SECONDARY,   // T2b : artères secondaires
    TERTIARY,    // T3 : rues (NOUVEAU)
    ALLEY,       // T4 : ruelles (NOUVEAU)
}
```

### GridJobUtilities.JobCellData — 2 nouveaux champs

```csharp
public struct JobCellData
{
    public WorldGrid.CellType Type;
    public float  Height;
    public bool   IsOccupied;
    public bool   HasPoi;

    // NOUVEAUX
    public float Urbanity;
    public byte  RoadTier;
}
```

### RoadBuilder.StampRoad — nouveau paramètre

```csharp
public static int StampRoad(List<Vector2Int> _path, int _width,
                            WorldGrid _grid, int _maxBridgeLength,
                            byte _roadTier)  // NOUVEAU
```

Règle : ne jamais écraser un tier supérieur (plus petit = plus important).

```csharp
// Dans StampRoad, au moment de marquer la cellule :
if (cell.RoadTier > 0 && cell.RoadTier < _roadTier)
    continue; // Ne pas dégrader une highway en ruelle

cell.RoadTier = _roadTier;
```

---

## 4. Champ d'urbanité

### Objectif

Donner une **forme organique** à la ville. Chaque cellule reçoit un score `Urbanity`
(0-1) qui contrôle où les routes et bâtiments peuvent pousser.

### Formule

```
urbanity(x, y) = radialFalloff(x, y) * noiseMask(x, y)

radialFalloff = max(0, 1 - dist(cell, cityCenter) / maxRadius)
noiseMask     = remap(perlin(x * scale + seed, y * scale + seed), 0.3, 0.7, 0, 1)
```

Le Perlin noise déforme le bord de la ville pour éviter un cercle parfait.

### Seuils d'urbanité

| Urbanité    | Zone         | Routes autorisées | Bâtiments |
|-------------|--------------|-------------------|-----------|
| `> 0.5`     | Centre dense | T1, T2, T3, T4    | Tous      |
| `0.2 – 0.5` | Péri-urbain  | T1, T2            | Clairsemé |
| `< 0.2`     | Campagne     | T1 uniquement     | Fermes    |

### ScriptableObject UrbanitySettings

```csharp
[CreateAssetMenu(menuName = "Settings/Urbanity Settings")]
public class UrbanitySettings : ScriptableObject
{
    [Range(20, 80)]   public float maxRadius       = 40f;
    [Range(0.01f, 0.1f)] public float noiseScale   = 0.03f;
    [Range(0f, 0.5f)] public float noiseAmplitude  = 0.4f;
    [Range(0.3f, 0.7f)] public float denseThreshold = 0.5f;
    [Range(0.1f, 0.4f)] public float suburbanThreshold = 0.2f;
}
```

### Burst Job

```csharp
[BurstCompile]
public struct UrbanityJob : IJobParallelFor
{
    [ReadOnly] public int    GridSize;
    [ReadOnly] public int2   CityCenter;
    [ReadOnly] public float  MaxRadius;
    [ReadOnly] public float  NoiseScale;
    [ReadOnly] public float  NoiseAmplitude;
    [ReadOnly] public float  Seed;

    public NativeArray<float> Results;

    public void Execute(int _index)
    {
        var x = _index % GridSize;
        var y = _index / GridSize;

        var dist = math.distance(new float2(x, y),
                                 new float2(CityCenter.x, CityCenter.y));

        var radial = math.max(0f, 1f - dist / MaxRadius);

        var nx = x * NoiseScale + Seed;
        var ny = y * NoiseScale + Seed;
        var noiseMask = math.saturate(
            noise.cnoise(new float2(nx, ny)) * NoiseAmplitude + 0.5f);

        Results[_index] = radial * noiseMask;
    }
}
```

100% parallélisable, dispatché via `GenerationJobManager.DispatchJob`.

---

## 5. Génération routière Agent-Based

### Structure d'un agent

```csharp
// Assets/Scripts/Roads/RoadAgent.cs
public struct RoadAgent
{
    public Vector2Int Position;
    public Vector2    Direction;      // float pour angles organiques
    public byte       Tier;           // 2, 3 ou 4
    public int        StepsRemaining;
    public bool       IsAlive;
    public float      NoiseSeed;      // Pour la perturbation Perlin unique
}
```

### Comportement d'un pas (pseudocode)

```
RoadAgent.Step(grid, settings):

    // 1. Perturbation organique de la direction
    noiseVal = perlin(Position.x * dirNoiseScale + NoiseSeed,
                      Position.y * dirNoiseScale + NoiseSeed)
    Direction = rotate(Direction, noiseVal * dirNoiseStrength)

    // 2. Calculer la prochaine position
    nextPos = Position + round(Direction)

    // 3. Tests d'arrêt
    si nextPos hors limites grille         → IsAlive = false; return
    si grid[nextPos].Urbanity < minUrbanity → IsAlive = false; return
    si grid[nextPos].Type == WATER          → tourner ±90° aléatoire; return
    si grid[nextPos].RoadTier > 0           → IsAlive = false; return (connexion!)
    si grid[nextPos].IsOccupied             → tourner ±90° aléatoire; return

    // 4. Stamper la route
    StampRoad(Position → nextPos, width, tier)
    Position = nextPos

    // 5. Bifurcation
    si random < branchProbability ET StepsRemaining > minBranchSteps:
        perpDir = perpendicular(Direction) * randomSign
        spawner nouvel agent(Position, perpDir, tier+1, stepsLeft * 0.6)

    // 6. Décrémenter
    StepsRemaining--
    si StepsRemaining <= 0 → IsAlive = false
```

La perturbation Perlin sur la direction (étape 1) est **la clé** pour obtenir des
rues médiévales organiques non-rectilignes. Le scale et le strength contrôlent le
degré de courbure.

### Configuration par tier

```csharp
[CreateAssetMenu(menuName = "Roads/Agent Road Settings")]
public class AgentRoadSettings : ScriptableObject
{
    public byte Tier;

    [Header("Segment")]
    [Range(1, 5)]   public int roadWidth       = 1;
    [Range(5, 60)]  public int minSteps        = 10;
    [Range(10, 100)] public int maxSteps       = 30;

    [Header("Branchement")]
    [Range(0f, 0.5f)] public float branchProbability = 0.2f;
    [Range(3, 20)]    public int   minBranchSteps    = 5;

    [Header("Direction organique")]
    [Range(0f, 0.3f)] public float directionNoiseScale    = 0.1f;
    [Range(0f, 45f)]  public float directionNoiseStrength = 15f;

    [Header("Contraintes")]
    [Range(0f, 1f)] public float minUrbanity = 0.2f;
    [Range(1, 50)]  public int   maxAgents   = 20;

    [Header("Pathfinding (pour T2 A*)")]
    public RoadSettings pathfindingSettings;
}
```

**Valeurs recommandées par tier :**

| Paramètre           | T2 Artères | T3 Rues | T4 Ruelles |
|---------------------|:----------:|:-------:|:----------:|
| `roadWidth`         |     2      |    1    |     1      |
| `minSteps`          |     15     |    8    |     3      |
| `maxSteps`          |     50     |   25    |     10     |
| `branchProbability` |    0.1     |  0.25   |    0.05    |
| `dirNoiseScale`     |    0.05    |   0.1   |    0.08    |
| `dirNoiseStrength`  |    10°     |   20°   |    10°     |
| `minUrbanity`       |    0.15    |   0.4   |    0.5     |
| `maxAgents`         |     12     |   40    |     20     |

### Orchestration des agents

```csharp
// Assets/Scripts/Roads/AgentRoadGenerator.cs
public static class AgentRoadGenerator
{
    // Spawner les agents initiaux selon le tier
    public static List<RoadAgent> SpawnAgents(
        WorldGrid _grid, AgentRoadSettings _settings,
        Vector2Int _cityCenter, List<Vector2Int> _roadNodes);

    // Simuler tous les agents jusqu'à leur mort
    public static IEnumerator RunAgents(
        WorldGrid _grid, List<RoadAgent> _agents,
        AgentRoadSettings _settings);
}
```

**Règles de spawn par tier :**

- **T2 radiaux** : 4-8 agents partant du `CityCenter` dans des directions espacées
  de ~45-90° avec du bruit. Simulés **après** les A* center→POI.
- **T3** : Parcourir toutes les cellules `ROAD` avec `RoadTier <= 2`. Tous les
  `N` cellules (N = `Random.Range(8, 16)` modulé par bruit), spawner un agent
  perpendiculaire. Direction perpendiculaire à la direction locale de la route T2.
- **T4** : Après `BlockExtractor`, pour chaque bloc de taille > `MaxBlockSize`,
  spawner un agent au centroïde du bloc, direction vers le centre du bloc.

### Agents et main-thread

Les agents T3/T4 **restent main-thread** (pas de parallelization Burst) car :

- Chaque agent lit les routes posées par les agents précédents (détection d'intersection).
- La grille est modifiée mutuellement.
- Sur une grille 256×256, ~60 agents × ~25 steps = ~1500 opérations = < 1ms.

Yield tous les `N` agents pour ne pas bloquer le frame.

---

## 6. Extraction des blocs et classification

### BlockData

```csharp
// Assets/Scripts/Grid/BlockData.cs

public enum BlockType
{
    RESIDENTIAL,
    COMMERCIAL,
    RELIGIOUS,
    CIVIC,
    AGRICULTURAL,
}

public struct BlockData
{
    public int              Id;
    public List<Vector2Int> Cells;
    public int              Size;            // = Cells.Count
    public Vector2          Centroid;
    public float            DistanceToCenter;
    public float            AverageUrbanity;
    public BlockType        Type;
    public bool             NeedsSubdivision;
}
```

### Algorithme de flood-fill

```
BlockExtractor.Extract(grid) :

    nextBlockId = 0
    visited = bool[size, size]
    blocks = List<BlockData>

    pour chaque cellule (x, y) :
        si visited[x,y]                → continuer
        si cell.Type != PLAIN          → continuer
        si cell.IsOccupied             → continuer
        si cell.Urbanity < 0.1         → continuer   // Hors ville

        // BFS
        queue = Queue<Vector2Int>
        blockCells = List<Vector2Int>
        queue.Enqueue((x, y))
        visited[x, y] = true

        tant que queue non vide :
            pos = queue.Dequeue()
            blockCells.Add(pos)
            grid.Cells[pos].BlockId = nextBlockId

            pour chaque voisin 4-connexe de pos :
                si non visité ET type PLAIN ET non occupé ET urbanité > 0.1 :
                    visited[voisin] = true
                    queue.Enqueue(voisin)

        // Métadonnées
        bloc = nouveau BlockData
        bloc.Id = nextBlockId
        bloc.Cells = blockCells
        bloc.Size = blockCells.Count
        bloc.Centroid = moyenne(blockCells)
        bloc.DistanceToCenter = distance(centroid, cityCenter)
        bloc.AverageUrbanity = moyenne(urbanity de chaque cellule)
        bloc.NeedsSubdivision = (bloc.Size > MAX_BLOCK_SIZE)
        bloc.Type = ClassifyBlock(bloc, grid)

        blocks.Add(bloc)
        nextBlockId++

    // Post-traitement
    FusionnerPetitsBlocs(blocks, grid, MIN_BLOCK_SIZE = 4)
    grid.Blocks = blocks
```

### Classification des blocs

```csharp
static BlockType ClassifyBlock(BlockData _block, WorldGrid _grid)
{
    // 1. Adjacence aux POIs (priorité haute)
    var adjacentPois = FindAdjacentPOIs(_block, _grid);

    if (adjacentPois.Contains(POIData.POIType.CHURCH))    return BlockType.RELIGIOUS;
    if (adjacentPois.Contains(POIData.POIType.TOWN_HALL)) return BlockType.CIVIC;
    if (adjacentPois.Contains(POIData.POIType.MARKET))    return BlockType.COMMERCIAL;

    // 2. Par urbanité et position
    if (_block.AverageUrbanity < 0.25f)                   return BlockType.AGRICULTURAL;
    if (_block.DistanceToCenter < 20f && _block.Size < 60) return BlockType.COMMERCIAL;

    return BlockType.RESIDENTIAL;
}
```

**FindAdjacentPOIs** : pour chaque cellule en bordure du bloc (ayant au moins un
voisin 4-connexe avec `Type == ROAD`), vérifier les voisins dans un rayon de 3
pour un POI. Plus efficace : itérer les `PlacedPOIPositions` et vérifier si à
distance ≤ 3 d'une cellule du bloc.

### Fusion des petits blocs

Les blocs de taille < `MIN_BLOCK_SIZE` (4) sont fusionnés avec leur voisin le plus
grand (celui qui partage le plus de bordure commune) en mettant à jour `BlockId`
sur toutes les cellules concernées.

---

## 7. Génération des maisons multi-cellules

### Catalogue de maisons

```csharp
// Assets/Scripts/Buildings/HouseCatalog.cs

[CreateAssetMenu(menuName = "Buildings/House Catalog")]
public class HouseCatalog : ScriptableObject
{
    [System.Serializable]
    public struct HouseEntry
    {
        public BuildingData   BuildingData;
        public BlockType[]    AllowedBlockTypes;
        public float          Weight;    // Probabilité relative
        public int            Priority;  // Plus grand = placé en premier
    }

    public HouseEntry[] Entries;

    // Retourne les entrées triées par priorité décroissante,
    // filtrées par type de bloc
    public List<HouseEntry> GetSortedEntries(BlockType _blockType);
}
```

**Catalogue prévu :**

| Asset          | Footprint                    | Priority | Blocs autorisés         |
|----------------|------------------------------|:--------:|-------------------------|
| `House1x1Data` | `(0,0)`                      |    1     | Tous                    |
| `House1x2Data` | `(0,0), (1,0)`               |    2     | Residential, Commercial |
| `House2x2Data` | `(0,0), (1,0), (0,1), (1,1)` |    3     | Residential, Commercial |
| `House2x3Data` | 2×3 = 6 cells                |    4     | Commercial, Civic       |
| `House3x3Data` | 3×3 = 9 cells                |    5     | Religious, Civic        |

### Algorithme de placement (greedy packing)

```
HouseGenerator.FillBlock(block, grid, catalog) :

    entries = catalog.GetSortedEntries(block.Type)  // triées par priorité desc
    cellsRemaining = Set(block.Cells)

    pour chaque cellule du bloc (itérée en spirale depuis le centroïde) :
        si cellule déjà occupée → continuer

        pour chaque entry dans entries :
            rotation = BuildingAreaHelper.FindBestRotation(entry.BuildingData, pos, grid)
            si rotation < 0 → continuer  // Ne rentre pas

            si !HasRoadAdjacency(entry.BuildingData, pos, rotation, grid) → continuer

            // Placement !
            BuildingAreaHelper.MarkCellAsOccupied(entry.BuildingData, pos, rotation, grid)
            cityRenderer.AddBuilding(worldPos, entry.BuildingData, rotation)
            break  // Passer à la cellule suivante

    // Fallback : tenter House1x1 sur les cellules restantes non occupées
```

### Contrainte road-facing

Nouvelle méthode dans `BuildingAreaHelper` :

```csharp
public static bool HasRoadAdjacency(BuildingData _data, Vector2Int _position,
                                    int _rotation, WorldGrid _grid)
{
    var directions = new[]
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    foreach (var offset in _data.buildingArea)
    {
        var rotated = RotateOffset(offset, _rotation);
        var cellPos = _position + rotated;

        foreach (var dir in directions)
        {
            var neighbor = cellPos + dir;
            if (!_grid.IsInBounds(neighbor)) continue;

            var neighborCell = _grid.Cells[neighbor.x, neighbor.y];
            if (neighborCell.Type is WorldGrid.CellType.ROAD
                                  or WorldGrid.CellType.BRIDGE)
                return true;
        }
    }

    return false;
}
```

### Adaptation du CityRenderer

Actuellement, `CityRenderer` ne supporte qu'un seul mesh (houses). Pour supporter
plusieurs types de bâtiments :

```csharp
public class CityRenderer
{
    // Remplacement de la liste unique par un dictionnaire
    private Dictionary<BuildingData, List<Vector3>>   _positionsPerType;
    private Dictionary<BuildingData, List<Matrix4x4>> _matricesPerType;

    public void AddBuilding(Vector3 _position, BuildingData _data, int _rotation)
    {
        var rot = Quaternion.Euler(eulerRotation) * RotationFromIndex(_rotation);
        var matrix = Matrix4x4.TRS(_position, rot, scale);

        if (!_positionsPerType.ContainsKey(_data))
        {
            _positionsPerType[_data] = new List<Vector3>();
            _matricesPerType[_data]  = new List<Matrix4x4>();
        }

        _positionsPerType[_data].Add(_position);
        _matricesPerType[_data].Add(matrix);
    }

    // BakeBatches et Update itèrent par BuildingData
}
```

---

## 8. Intégration C# Jobs / Burst

### Récapitulatif par étape

| Étape                |        Burst Job ?        | Justification                           |
|----------------------|:-------------------------:|-----------------------------------------|
| UrbanityGenerator    |    ✅ `IJobParallelFor`    | Chaque cellule indépendante             |
| CityGenerator Pass 1 |      ✅ `PoiScoreJob`      | Existant, inchangé                      |
| RoadGenerator T1     | ✅ `PathfindingProcessJob` | Existant, inchangé                      |
| RoadGenerator T2a    | ✅ `PathfindingProcessJob` | Existant, inchangé                      |
| CityGenerator Pass 2 |      ✅ `PoiScoreJob`      | Existant, inchangé                      |
| RoadGenerator T2b    |         ⚠️ Mixte          | A* via Job + agents radiaux main-thread |
| RoadGenerator T3     |       ❌ Main-thread       | Agents mutuellement dépendants          |
| BlockExtractor       |       ❌ Main-thread       | BFS séquentiel, < 1ms sur 256×256       |
| RoadGenerator T4     |       ❌ Main-thread       | Agents mutuellement dépendants          |
| HouseGenerator       |       ⚠️ Optionnel        | Scoring parallèle, placement séquentiel |

### Nouveaux Jobs

#### UrbanityJob (détaillé en §4)

Dispatché via `CityGenerationJobRunner` :

```csharp
public static IEnumerator ComputeUrbanity(WorldGrid _grid, UrbanitySettings _settings,
                                          Vector2Int _cityCenter,
                                          Action<NativeArray<float>> _onComplete)
{
    var totalCells = _grid.size * _grid.size;
    var results = new NativeArray<float>(totalCells, Allocator.Persistent);

    var job = new UrbanityJob
    {
        GridSize   = _grid.size,
        CityCenter = new int2(_cityCenter.x, _cityCenter.y),
        MaxRadius  = _settings.maxRadius,
        NoiseScale = _settings.noiseScale,
        NoiseAmplitude = _settings.noiseAmplitude,
        Seed       = Random.Range(0f, 1000f),
        Results    = results
    };

    yield return GenerationJobManager.Instance.StartCoroutine(
        GenerationJobManager.DispatchJob(job, totalCells, 64,
            _completed => _onComplete?.Invoke(_completed.Results),
            results));
}
```

#### HousePlacementScoringJob (optionnel, Phase 5+)

Scorer toutes les cellules d'un bloc en parallèle avant le placement greedy.
Score = `roadAdjacencyBonus + urbanity + variété par bruit`. Le placement final
reste séquentiel (chaque maison modifie `IsOccupied`).

### Performance attendue

Avec la grille 256×256 (65 536 cellules) :

- `UrbanityJob` : ~0.1ms (trivial, une multiplication par cellule)
- `SettleScoreJob` : ~2ms (existant, inchangé)
- `PoiScoreJob` : ~3ms (existant, inchangé)
- `PathfindingProcessJob` : ~5-15ms par batch de routes (existant)
- Agents T3 (main-thread) : ~1ms (~60 agents × ~25 steps)
- `BlockExtractor` (main-thread) : < 1ms (BFS simple)
- `HouseGenerator` (main-thread) : ~2-5ms (yield tous les 50 placements)

**Estimation totale : ~30-50ms** pour la génération complète de la ville, contre
les drops à 5 FPS (200ms+) actuels principalement dus au pipeline séquentiel
non-intercalé.

---

## 9. Liste des fichiers

### Nouveaux fichiers

| Fichier                                          | Contenu                                         |
|--------------------------------------------------|-------------------------------------------------|
| `Assets/Scripts/Generators/UrbanityGenerator.cs` | `IPipelineStep`, dispatch `UrbanityJob`         |
| `Assets/Scripts/Generators/BlockExtractor.cs`    | `IPipelineStep`, flood-fill BFS, classification |
| `Assets/Scripts/Generators/HouseGenerator.cs`    | `IPipelineStep`, packing greedy par bloc        |
| `Assets/Scripts/Roads/RoadAgent.cs`              | struct `RoadAgent`, logique `Step()`            |
| `Assets/Scripts/Roads/AgentRoadGenerator.cs`     | Orchestration : spawn, simulation, yield        |
| `Assets/Scripts/Roads/AgentRoadSettings.cs`      | ScriptableObject config par tier                |
| `Assets/Scripts/Grid/BlockData.cs`               | struct `BlockData`, enum `BlockType`            |
| `Assets/Scripts/Buildings/HouseCatalog.cs`       | ScriptableObject catalogue multi-cell           |
| `Assets/Scripts/Jobs/UrbanityJob.cs`             | Burst job `IJobParallelFor`                     |
| `Assets/Scripts/Settings/UrbanitySettings.cs`    | ScriptableObject paramètres urbanité            |

### Fichiers modifiés

| Fichier                                          | Modifications                                                             |
|--------------------------------------------------|---------------------------------------------------------------------------|
| `Assets/Scripts/Grid/WorldGrid.cs`               | +`Urbanity`, `BlockId`, `RoadTier` sur `Cell` ; +`List<BlockData> Blocks` |
| `Assets/Scripts/GenerationPipeline.cs`           | `IPipelineStep`, pipeline 12 étapes                                       |
| `Assets/Scripts/Generators/IGenerator.cs`        | +interface `IPipelineStep`                                                |
| `Assets/Scripts/Generators/CityGenerator.cs`     | Split `GenerateAnchorPOIs` / `GenerateRoadDependentPOIs`                  |
| `Assets/Scripts/Generators/RoadGenerator.cs`     | Split `GenerateT1`/`T2`/`T3`/`T4`                                         |
| `Assets/Scripts/POIs/POIData.cs`                 | +property `RequiresRoad`                                                  |
| `Assets/Scripts/Roads/RoadGraph.cs`              | +`TERTIARY`, `ALLEY` dans `EdgeType`                                      |
| `Assets/Scripts/Roads/RoadBuilder.cs`            | +param `_roadTier` dans `StampRoad`                                       |
| `Assets/Scripts/Buildings/BuildingAreaHelper.cs` | +`HasRoadAdjacency()`                                                     |
| `Assets/Scripts/Renderers/CityRenderer.cs`       | Support multi-mesh par `BuildingData`                                     |
| `Assets/Scripts/Jobs/GridJobUtilities.cs`        | +`Urbanity`, `RoadTier` dans `JobCellData`                                |
| `Assets/Scripts/Jobs/CityGenerationJobRunner.cs` | +`ComputeUrbanity()`                                                      |

### Nouveaux assets (Data)

| Asset                                          | Type                |
|------------------------------------------------|---------------------|
| `Assets/Data/Buildings/House1x2Data.asset`     | `BuildingData`      |
| `Assets/Data/Buildings/House2x2Data.asset`     | `BuildingData`      |
| `Assets/Data/Buildings/House2x3Data.asset`     | `BuildingData`      |
| `Assets/Data/Buildings/House3x3Data.asset`     | `BuildingData`      |
| `Assets/Data/Buildings/HouseCatalog.asset`     | `HouseCatalog`      |
| `Assets/Data/Roads/AgentRoadSettings_T2.asset` | `AgentRoadSettings` |
| `Assets/Data/Roads/AgentRoadSettings_T3.asset` | `AgentRoadSettings` |
| `Assets/Data/Roads/AgentRoadSettings_T4.asset` | `AgentRoadSettings` |
| `Assets/Data/Settings/UrbanitySettings.asset`  | `UrbanitySettings`  |

---

## 10. Phases d'implémentation

### Phase 1 — Pipeline multi-passes et résolution NEAR_ROAD

> **Priorité** : Critique
> **Effort** : Modéré (~2-3 jours)
> **Impact** : Élevé — débloque toute la suite

**Tâches :**

1. Créer l'interface `IPipelineStep`
2. Refactorer `GenerationPipeline` pour une liste ordonnée de steps
3. Ajouter `RequiresRoad` sur `POIData`
4. Splitter `CityGenerator.Generate()` en `GenerateAnchorPOIs` et `GenerateRoadDependentPOIs`
5. Splitter `RoadGenerator.Generate()` en `GenerateT1`, `GenerateT2`
6. Ajouter `RoadTier` sur `Cell` et modifier `StampRoad`
7. Brancher le pipeline : Map → POIs(1) → T1 → T2a → POIs(2) → T2b → Mesh rebuild

**Critère de validation** : Les POIs Market avec `NEAR_ROAD` sont placés près
des routes T2, visibles sur le `TerrainRenderer`.

---

### Phase 2 — Champ d'urbanité

> **Priorité** : Haute (fondation pour les agents)
> **Effort** : Faible (~1 jour)
> **Impact** : Moyen — donne sa forme à la ville

**Tâches :**

1. Créer `UrbanitySettings` ScriptableObject + asset
2. Créer `UrbanityJob` (Burst)
3. Créer `UrbanityGenerator` (`IPipelineStep`)
4. Ajouter `Urbanity` sur `Cell` et `JobCellData`
5. Ajouter `ComputeUrbanity` dans `CityGenerationJobRunner`
6. Insérer dans le pipeline après `MapGenerator`, avant `CityGenerator Pass 1`
7. Visualiser l'urbanité sur le `DebugRenderer` (couleur gradient)

**Critère de validation** : Le `DebugRenderer` affiche un halo organique autour
du `CityCenter` avec une forme non-circulaire.

---

### Phase 3 — Agents T3 : subdivision en blocs

> **Priorité** : Haute (impact visuel majeur)
> **Effort** : Modéré (~2-3 jours)
> **Impact** : Élevé — donne du volume à la ville

**Tâches :**

1. Créer `AgentRoadSettings` ScriptableObject + 3 assets (T2, T3, T4)
2. Créer `RoadAgent` struct avec `Step()`
3. Créer `AgentRoadGenerator` (spawn + simulation coroutine)
4. Implémenter `RoadGenerator.GenerateT3()` : spawn perpendiculaire aux T2
5. Implémenter les agents T2 radiaux dans `RoadGenerator.GenerateT2()`
6. Ajouter `TERTIARY` et `ALLEY` dans `RoadGraph.EdgeType`
7. Tester et tuner les paramètres (branch prob, noise, steps)

**Critère de validation** : Le `TerrainRenderer` montre des rues formant des blocs
irréguliers (pas une grille parfaite) dans la zone dense.

---

### Phase 4 — BlockExtractor et classification

> **Priorité** : Haute (prérequis pour les maisons)
> **Effort** : Modéré (~1-2 jours)
> **Impact** : Moyen — infrastructure invisible

**Tâches :**

1. Créer `BlockData` struct et `BlockType` enum
2. Créer `BlockExtractor` (`IPipelineStep`) avec flood-fill BFS
3. Implémenter la fusion des petits blocs (< 4 cellules)
4. Implémenter la classification par adjacence POI et urbanité
5. Stocker `BlockId` sur `Cell` et `Blocks` sur `WorldGrid`
6. Visualiser les blocs sur le `DebugRenderer` (couleur par `BlockType`)
7. Marquer les blocs > `MaxBlockSize` pour subdivision (T4)

**Critère de validation** : Le `DebugRenderer` affiche des blocs colorés par type.
Les blocs adjacents à un POI ont le bon type.

---

### Phase 5 — HouseGenerator avec multi-cell

> **Priorité** : Haute (résultat final visible)
> **Effort** : Modéré (~2-3 jours)
> **Impact** : Élevé — la ville prend vie

**Tâches :**

1. Créer `HouseCatalog` ScriptableObject
2. Créer les assets `BuildingData` pour chaque taille (1×2, 2×2, 2×3, 3×3)
3. Ajouter `HasRoadAdjacency()` dans `BuildingAreaHelper`
4. Créer `HouseGenerator` (`IPipelineStep`) avec packing greedy par bloc
5. Adapter `CityRenderer` pour supporter plusieurs meshes par `BuildingData`
6. Insérer dans le pipeline après `BlockExtractor`
7. Yield tous les N placements pour ne pas bloquer le frame

**Critère de validation** : Les blocs sont remplis de maisons de tailles variées.
Toutes les maisons sont adjacentes à une route. Le frame rate reste > 30 FPS
pendant la génération.

---

### Phase 6 — Agents T4 : ruelles (polish)

> **Priorité** : Basse (amélioration visuelle)
> **Effort** : Faible (~0.5 jour)
> **Impact** : Faible — détail supplémentaire

**Tâches :**

1. Implémenter `RoadGenerator.GenerateT4()` ciblant les blocs `NeedsSubdivision`
2. Re-run `BlockExtractor` après T4 (optionnel, ou juste mettre à jour les blocs)
3. Ajouter des couleurs de tier routier dans `BiomeColorConfig`

**Critère de validation** : Les gros blocs sont subdivisés par des passages étroits.

---

### Phase 7 — Polish et optimisations (optionnel)

> **Effort** : Variable

- Couleurs de route par tier dans `TerrainRenderer`
- Seed reproductible propagé via `GenerationPipeline` (`mapSeed + stepIndex`)
- Murailles de ville (anneau de route T2 concentrique à la limite d'urbanité dense)
- Places centrales / carrefours élargis aux intersections majeures
- HousePlacementScoringJob (Burst) pour accélérer le scoring si nécessaire

---

## Annexe A — Diagramme du pipeline

```
                    ┌──────────────┐
                    │ MapGenerator │ Terrain, eau, rivières
                    └──────┬───────┘
                           │
                    ┌──────▼───────────────┐
                    │ UrbanityGenerator    │ Burst Job → Urbanity sur Cell
                    └──────┬───────────────┘
                           │
                    ┌──────▼───────────────┐
                    │ CityGenerator Pass 1 │ TownHall, Church, Well
                    └──────┬───────────────┘   (pas de NEAR_ROAD)
                           │
                    ┌──────▼───────────────┐
                    │ RoadGenerator T1     │ A* → Highways vers NearCities
                    └──────┬───────────────┘
                           │
                    ┌──────▼───────────────┐
                    │ RoadGenerator T2a    │ A* → Center → POIs ancrés
                    └──────┬───────────────┘
                           │
                    ┌──────▼───────────────┐
                    │ CityGenerator Pass 2 │ Market, etc. (NEAR_ROAD ok)
                    └──────┬───────────────┘
                           │
                    ┌──────▼───────────────┐
                    │ RoadGenerator T2b    │ A* + Agents radiaux
                    └──────┬───────────────┘
                           │
                    ┌──────▼───────────────┐
                    │ RoadGenerator T3     │ Agents perpendiculaires
                    └──────┬───────────────┘   → créent les blocs
                           │
                    ┌──────▼───────────────┐
                    │ BlockExtractor       │ Flood-fill + classification
                    └──────┬───────────────┘
                           │
                    ┌──────▼───────────────┐
                    │ RoadGenerator T4     │ Ruelles dans gros blocs
                    └──────┬───────────────┘
                           │
                    ┌──────▼───────────────┐
                    │ HouseGenerator       │ Greedy packing par bloc
                    └──────┬───────────────┘
                           │
                    ┌──────▼───────────────┐
                    │ TerrainRenderer      │ Rebuild mesh final
                    └──────────────────────┘
```

---

## Annexe B — Conventions du projet

- Paramètres : `_camelCase` (underscore prefix)
- Membres privés : `_camelCase`
- Membres publics : `PascalCase`
- Constantes : `UPPER_SNAKE_CASE`
- Singletons : `MonoSingleton<T>` (via `Core.Patterns`)
- Configuration : `ScriptableObject` exposé dans l'Inspector
- Jobs : struct `IJobParallelFor` ou `IJob` avec `[BurstCompile]`
- Dispatching : `GenerationJobManager.DispatchJob<T>()` via coroutine
- Grille : `WorldGrid.Cells[x, y]`, index plat = `y * size + x`

---

## Annexe C — Références

- Parish, Y. I. H., & Müller, P. (2001). *Procedural Modeling of Cities.* SIGGRAPH.
- Chen, G., et al. (2008). *Interactive Procedural Street Modeling.* SIGGRAPH.
- Lechner, T., et al. (2006). *Procedural City Modeling.* Midterm Report, MIT.
- Müller, P., et al. (2006). *Procedural Modeling of Buildings.* SIGGRAPH.
- Greuter, S., et al. (2003). *Real-time Procedural Generation of Pseudo Infinite Cities.* GRAPHITE.
- Kelly, G. & McCabe, H. (2006). *A Survey of Procedural Techniques for City Generation.* ITB Journal.

