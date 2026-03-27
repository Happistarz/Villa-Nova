using System;
using System.Collections;
using System.Collections.Generic;
using Core.Extensions;
using Core.Patterns;
using UnityEngine;
using Random = UnityEngine.Random;

public class CityGenerator : MonoSingleton<CityGenerator>, IGenerator
{
    public string Name => "City";

    [Header("Settings")]
    public float settlerSearchRadius = 5f;

    public Transform cityCenterMarker;

    [Header("Renderers")]
    public CityRenderer cityRenderer;

    public DebugRenderer debugRenderer;

    [Header("POI")]
    public POIData[] poiDataList;

    [Header("Houses")]
    public BuildingData houseData;

    [Header("Near Cities")]
    public NearbyCityPool nearbyCityPool;

    public bool IsGenerating { get; private set; }

    public event Action OnGenerationComplete;

    public Vector2Int                CityCenter         { get; private set; }
    public IReadOnlyList<Vector2Int> PlacedPOIPositions => _placedPOIPositions;

    private          WorldGrid        _grid;
    private readonly List<Vector2Int> _placedPOIPositions = new();

    private void Start()
    {
        _grid = WorldGrid.Instance;

        GameManager.Instance.NewGenerationStarted += NewGenerationStarted;
    }

    private void NewGenerationStarted()
    {
        cityRenderer.ClearCity();
        _placedPOIPositions.Clear();

        if (nearbyCityPool)
            nearbyCityPool.ReleaseAll();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (GameManager.HasInstance)
            GameManager.Instance.NewGenerationStarted -= NewGenerationStarted;
    }

    public IEnumerator Generate(WorldGrid _generationGrid)
    {
        _grid        = _generationGrid;
        IsGenerating = true;

        // Find best city center point for the settler
        var bestHomePoint = Vector2Int.zero;
        yield return StartCoroutine(
            CityGenerationJobRunner
                .FindBestSettlePoint(_grid, settlerSearchRadius, _point => bestHomePoint = _point));

        GenerateNearCitiesData();

        // End generation if no valid city center point found
        var cell = _grid.GetCell(bestHomePoint);
        if (cell == null)
        {
            IsGenerating = false;
            OnGenerationComplete?.Invoke();
            yield break;
        }

        CityCenter                = bestHomePoint;
        cityCenterMarker.position = bestHomePoint.ToVector3();

        // Mark the cell as city center
        var tempCell = cell.Value;
        tempCell.Type = WorldGrid.CellType.CITY;
        _grid.UpdateCell(bestHomePoint, tempCell);

        yield return StartCoroutine(PlacePOIsCoroutine(bestHomePoint));

        // Rebuild meshes with POIs before placing houses
        if (debugRenderer && debugRenderer.renderEnabled.Value)
            debugRenderer.BuildMesh();

        cityRenderer.BakeBatches();

        IsGenerating = false;
        OnGenerationComplete?.Invoke();
    }

    private IEnumerator PlaceHousesCoroutine(WorldGrid.Cell _cityCell)
    {
        var count = 0;

        const int RADIUS = 32;
        for (var x = -RADIUS; x <= RADIUS; x++)
        {
            for (var y = -RADIUS; y <= RADIUS; y++)
            {
                if (!_cityCell.POI) continue; // TEMP

                var point = new Vector2Int(_cityCell.Position.x + x, _cityCell.Position.y + y);
                var cell  = _grid.GetCell(point);

                if (cell != null && !cell.Value.Is(WorldGrid.CellType.PLAIN)) continue;

                if (houseData && houseData.buildingArea is { Count: > 0 })
                {
                    int rotation;
                    if (houseData.randomizeRotation)
                    {
                        rotation = Random.Range(0, 4);
                        if (!BuildingAreaHelper.CanPlace(houseData, point, rotation, _grid))
                            continue;
                    }
                    else
                    {
                        rotation = BuildingAreaHelper.FindBestRotation(houseData, point, _grid);
                        if (rotation < 0) continue;
                    }

                    var heightStep = MapGenerator.Instance.heightStep;
                    BuildingAreaHelper.FlattenArea(houseData, point, rotation, _grid, heightStep);
                    BuildingAreaHelper.MarkCellAsOccupied(houseData, point, rotation, _grid);
                }

                var worldPos = _grid.CellToWorld(point);
                cityRenderer.AddHouse(worldPos);

                count++;

                if (count % 20 == 0)
                    yield return null;
            }
        }
    }

    private IEnumerator PlacePOIsCoroutine(Vector2Int _cityCenter)
    {
        _placedPOIPositions.Clear();

        // foreach registered POI type, apply placement algorithm
        foreach (var poiData in poiDataList)
        {
            if (!poiData) continue;

            var buildingData = poiData.BuildingData;

            // Spawn a random number of these POI types within the defined range [0, n]
            var poiSpawnCount = Random.Range(poiData.SpawnRange.x, poiData.SpawnRange.y + 1);
            for (var i = 0; i < poiSpawnCount; i++)
            {
                List<(Vector2Int pos, float score)> candidates = null;

                // Find the best candidate positions for this POI type, sorted by score
                yield return StartCoroutine(CityGenerationJobRunner.FindBestPoiLocation(
                                                _grid, poiData, _placedPOIPositions, _cityCenter,
                                                _result => candidates = _result));

                if (candidates == null || candidates.Count == 0)
                    continue;

                var found        = false;
                var bestPos      = Vector2Int.zero;
                var bestRotation = 0;

                // Limit the number of candidates we check to avoid long generation times in case of many POIs or large maps
                var checksCount = 0;
                foreach (var (pos, _) in candidates)
                {
                    if (checksCount++ > 100) break;

                    var rotation = 0;

                    // Ensure the building has a valid area to be placed on
                    if (buildingData && buildingData.buildingArea is { Count: > 0 })
                    {
                        rotation = BuildingAreaHelper.FindBestRotation(buildingData, pos, _grid);
                        if (rotation < 0)
                            continue;
                    }

                    bestPos      = pos;
                    bestRotation = rotation;
                    found        = true;
                    break;
                }

                if (!found) continue;

                var heightStep = MapGenerator.Instance.heightStep;

                // If the POI has an associated building with a defined area, flatten the terrain and mark the area as occupied
                if (buildingData && buildingData.buildingArea is { Count: > 0 })
                {
                    BuildingAreaHelper.FlattenArea(buildingData, bestPos, bestRotation, _grid, heightStep);
                    BuildingAreaHelper.MarkAreaAsPoi(buildingData, bestPos, bestRotation, _grid, poiData);
                }
                else
                {
                    // If no building area, just mark the single cell as occupied by this POI
                    var cell = _grid.Cells[bestPos.x, bestPos.y];
                    cell.POI        = poiData;
                    cell.IsOccupied = true;
                    _grid.UpdateCell(bestPos, cell);
                }

                _placedPOIPositions.Add(bestPos);
            }
        }
    }

    private void GenerateNearCitiesData()
    {
        const int MAX_NEAR_CITIES = 4;
        var       cityCount       = Random.Range(0, MAX_NEAR_CITIES + 1);
        var       nearCitiesData  = new List<WorldGrid.NearCityData>();

        // Fill near cities data with random positions around the map edges, ensuring they are not too close to each other
        for (var i = 0; i < cityCount; i++)
        {
            var edge       = Random.Range(0, 4);
            var randomCell = Random.Range(0, _grid.size);
            var pos        = FindSafeCityPosition(edge, randomCell, nearCitiesData);

            var distanceToCenter    = Vector2Int.Distance(pos, new Vector2Int(_grid.size / 2, _grid.size / 2));
            var randomExtraDistance = Random.Range(0f, 20f);
            nearCitiesData.Add(new WorldGrid.NearCityData
            {
                CityPos  = pos,
                Distance = distanceToCenter / Constants.CELL_TO_METER + randomExtraDistance
            });
        }

        nearCitiesData.Sort((_a, _b) => _a.Distance.CompareTo(_b.Distance));

        // Get random names for the near cities from the config
        var names = GameManager.Instance.Config
            ? GameManager.Instance.Config.GetRandomCityNames(nearCitiesData.Count)
            : null;

        // Default to "City 1", "City 2", etc. if no names available
        for (var i = 0; i < nearCitiesData.Count; i++)
            nearCitiesData[i].Name = names != null ? names[i] : $"City {i + 1}";

        WorldGrid.Instance.NearCities = nearCitiesData;

        DisplayNearCities(nearCitiesData);
    }

    private Vector2Int FindSafeCityPosition(int _edge, int _randomCell, List<WorldGrid.NearCityData> _existing)
    {
        const int EDGE_OFFSET = 10;
        var       pos         = Vector2Int.zero;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            // Generate a position along the specified edge with an offset to avoid being too close to the actual edge
            pos = _edge switch
            {
                0 => new Vector2Int(-EDGE_OFFSET,             _randomCell),
                1 => new Vector2Int(_grid.size + EDGE_OFFSET, _randomCell),
                2 => new Vector2Int(_randomCell,              -EDGE_OFFSET),
                3 => new Vector2Int(_randomCell,              _grid.size + EDGE_OFFSET),
                _ => Vector2Int.zero
            };

            // Check if this position is too close to any existing near city
            var tooClose = false;
            foreach (var existingCity in _existing)
            {
                if (!(Vector2Int.Distance(pos, existingCity.CityPos) <
                      Constants.NEAR_CITY_MIN_DISTANCE * Constants.CELL_TO_METER))
                    continue;

                tooClose = true;
                break;
            }

            if (!tooClose) break;

            _randomCell = Random.Range(0, _grid.size);
        }

        return pos;
    }

    private void DisplayNearCities(List<WorldGrid.NearCityData> _nearCitiesData)
    {
        if (!nearbyCityPool) return;

        nearbyCityPool.ReleaseAll();

        for (var i = 0; i < _nearCitiesData.Count; i++)
        {
            var nearCity = _nearCitiesData[i];
            var display  = nearbyCityPool.Get();
            var worldPos = _grid.CellToWorld(nearCity.CityPos);
            display.DisplayInfos(nearCity.Name, nearCity.Distance, worldPos, i);
        }
    }
}