using System;
using System.Collections;
using System.Collections.Generic;
using Core.Extensions;
using Core.Patterns;
using Generators;
using UnityEngine;

public class CityGenerator : MonoSingleton<CityGenerator>, IGenerator
{
    public string Name => "City";

    public HouseGenerator houseGenerator;
    
    [Header("Settings")]
    public float settlerSearchRadius = 5f;

    public Transform cityCenterMarker;

    [Header("Urbanity")]
    public UrbanityConfig urbanityConfig;

    [Header("Renderers")]
    public CityRenderer cityRenderer;
    public DebugRenderer debugRenderer;

    [Header("POI")]
    public POIData[] poiDataList;

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
        _placedPOIPositions.Clear();
        UrbanityHelper.Reset();
        DistanceFieldHelper.Reset();

        if (cityRenderer)
            cityRenderer.Clear();

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

        CityCenter                = bestHomePoint;
        cityCenterMarker.position = bestHomePoint.ToVector3();

        var tempCell = cell.Value;
        tempCell.Type = WorldGrid.CellType.CITY;
        _grid.UpdateCell(bestHomePoint, tempCell);

        yield return StartCoroutine(PlacePOIsCoroutine(bestHomePoint));

        yield return StartCoroutine(UrbanityHelper.Compute(_grid, bestHomePoint, urbanityConfig));

        if (debugRenderer)
            debugRenderer.BuildMesh();

        IsGenerating = false;
        OnGenerationComplete?.Invoke();
    }

    public IEnumerator PlaceHouses(WorldGrid _generationGrid)
    {
        _grid = _generationGrid;

        yield return StartCoroutine(DistanceFieldHelper.Compute(_grid, CityCenter));

        yield return StartCoroutine(houseGenerator.PlaceHousesCoroutine());

        if (cityRenderer)
            cityRenderer.BakeBatches();
    }

    private IEnumerator PlacePOIsCoroutine(Vector2Int _cityCenter)
    {
        _placedPOIPositions.Clear();

        foreach (var poiData in poiDataList)
        {
            if (!poiData) continue;

            var buildingData = poiData.buildingData;

            var poiSpawnCount = GameManager.Instance.RandomEngine.Range(poiData.spawnRange.x, poiData.spawnRange.y + 1);
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

                var heightStep = MapGenerator.Instance.heightStep;

                if (buildingData && buildingData.buildingArea is { Count: > 0 })
                {
                    BuildingAreaHelper.FlattenArea(buildingData, bestPos, bestRotation, _grid, heightStep);
                    BuildingAreaHelper.MarkAreaAsPoi(buildingData, bestPos, bestRotation, _grid, poiData);

                    if (cityRenderer)
                        cityRenderer.AddBuilding(
                            BuildingAreaHelper.GetAreaCenter(buildingData, bestPos, bestRotation, _grid),
                            bestRotation, buildingData);
                }
                else
                {
                    var poiCell = _grid.Cells[bestPos.x, bestPos.y];
                    poiCell.POI        = poiData;
                    poiCell.IsOccupied = true;
                    _grid.UpdateCell(bestPos, poiCell);
                }

                _placedPOIPositions.Add(bestPos);
            }
        }
    }

    private void GenerateNearCitiesData()
    {
        const int MAX_NEAR_CITIES = 4;
        var       rng             = GameManager.Instance.RandomEngine;
        var       cityCount       = rng.Range(0, MAX_NEAR_CITIES + 1);
        var       nearCitiesData  = new List<WorldGrid.NearCityData>();

        for (var i = 0; i < cityCount; i++)
        {
            var edge       = rng.Range(0, 4);
            var randomCell = rng.Range(0, _grid.size);
            var pos        = FindSafeCityPosition(edge, randomCell, nearCitiesData);

            var distanceToCenter    = Vector2Int.Distance(pos, new Vector2Int(_grid.size / 2, _grid.size / 2));
            var randomExtraDistance = rng.Range(0f, 20f);
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
                0 => new Vector2Int(-EDGE_OFFSET,             _randomCell),              // Left edge
                1 => new Vector2Int(_grid.size + EDGE_OFFSET, _randomCell),              // Right edge
                2 => new Vector2Int(_randomCell,              -EDGE_OFFSET),             // Top edge
                3 => new Vector2Int(_randomCell,              _grid.size + EDGE_OFFSET), // Bottom edge
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

            _randomCell = GameManager.Instance.RandomEngine.Range(0, _grid.size);
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