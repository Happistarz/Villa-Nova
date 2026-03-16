using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns;
using UnityEngine;
using Random = UnityEngine.Random;

public class CityGenerator : MonoSingleton<CityGenerator>, IGenerator
{
    public string Name => "City";

    public float settlerSearchRadius = 5f;

    public CityRenderer  cityRenderer;
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
        _grid                                     =  WorldGrid.Instance;
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

        var bestHomePoint = Vector2Int.zero;
        yield return StartCoroutine(
            CityGenerationJobRunner
                .FindBestSettlePoint(_grid, settlerSearchRadius, _point => bestHomePoint = _point));
        GenerateNearCitiesData();

        var cell = _grid.GetCell(bestHomePoint);
        if (cell == null)
        {
            IsGenerating = false;
            OnGenerationComplete?.Invoke();
            yield break;
        }

        CityCenter = bestHomePoint;

        var tempCell = cell.Value;
        tempCell.Type = WorldGrid.CellType.CITY;
        _grid.UpdateCell(bestHomePoint, tempCell);

        yield return StartCoroutine(PlacePOIsCoroutine(bestHomePoint));

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

        foreach (var poiData in poiDataList)
        {
            if (!poiData) continue;

            var buildingData  = poiData.BuildingData;
            var poiSpawnCount = Random.Range(poiData.SpawnRange.x, poiData.SpawnRange.y + 1);

            for (var i = 0; i < poiSpawnCount; i++)
            {
                List<(Vector2Int pos, float score)> candidates = null;

                yield return StartCoroutine(CityGenerationJobRunner.FindBestPoiLocation(
                    _grid, poiData, _placedPOIPositions, _cityCenter,
                    _result => candidates = _result));

                if (candidates == null || candidates.Count == 0)
                    continue;

                var found        = false;
                var bestPos      = Vector2Int.zero;
                var bestRotation = 0;

                var checksCount = 0;
                foreach (var (pos, _) in candidates)
                {
                    if (checksCount++ > 100) break;

                    var rotation = 0;

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

                if (buildingData && buildingData.buildingArea is { Count: > 0 })
                    BuildingAreaHelper.MarkCellAsOccupied(buildingData, bestPos, bestRotation, _grid);

                var cell = _grid.Cells[bestPos.x, bestPos.y];
                cell.POI = poiData;
                _grid.UpdateCell(bestPos, cell);
                _placedPOIPositions.Add(bestPos);
            }
        }
    }

    private void GenerateNearCitiesData()
    {
        const int MAX_NEAR_CITIES = 4;
        var       cityCount       = Random.Range(0, MAX_NEAR_CITIES + 1);
        var       nearCitiesData  = new List<WorldGrid.NearCityData>();

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

        var names = GameManager.Instance.Config
            ? GameManager.Instance.Config.GetRandomCityNames(nearCitiesData.Count)
            : null;

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
            pos = _edge switch
            {
                0 => new Vector2Int(-EDGE_OFFSET,             _randomCell),
                1 => new Vector2Int(_grid.size + EDGE_OFFSET, _randomCell),
                2 => new Vector2Int(_randomCell,              -EDGE_OFFSET),
                3 => new Vector2Int(_randomCell,              _grid.size + EDGE_OFFSET),
                _ => Vector2Int.zero
            };

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