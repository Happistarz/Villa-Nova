using System.Collections;
using System.Collections.Generic;
using Core.Patterns;
using UnityEngine;

public class CityGenerator : MonoSingleton<CityGenerator>
{
    public WorldRevealAnimator revealAnimator;

    public float settlerSearchRadius = 5f;

    public CityRenderer  cityRenderer;
    public DebugRenderer debugRenderer;

    [Header("POI")]
    public POIData[] poiDataList;

    [Header("Houses")]
    public BuildingData houseData;

    [Header("Near Cities")]
    public NearbyCityPool nearbyCityPool;

    [Header("Roads")]
    public RoadSettings roadSettings = RoadSettings.Default;

    public event System.Action OnRoadsGenerated;

    private          WorldGrid        _grid;
    private readonly List<Vector2Int> _placedPOIPositions = new();

    private void Start()
    {
        _grid = WorldGrid.Instance;

        if (revealAnimator)
            revealAnimator.OnRevealComplete += GenerateCity;

        MapGenerator.Instance.OnMapGenerated += OnMapGenerated;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (revealAnimator)
            revealAnimator.OnRevealComplete -= GenerateCity;

        if (MapGenerator.HasInstance)
            MapGenerator.Instance.OnMapGenerated -= OnMapGenerated;
    }

    private void OnMapGenerated()
    {
        if (!revealAnimator || !revealAnimator.isActiveAndEnabled)
            StartCoroutine(GenerateCityCoroutine());
    }

    private void GenerateCity()
    {
        cityRenderer.ClearHouses();

        if (nearbyCityPool)
            nearbyCityPool.ReleaseAll();

        StartCoroutine(GenerateCityCoroutine());
    }

    private IEnumerator GenerateCityCoroutine()
    {
        var bestHomePoint = Vector2Int.zero;
        yield return StartCoroutine(FindSettlePosCoroutine(_result => bestHomePoint = _result));
        GenerateNearCitiesData();

        var cell = _grid.GetCell(bestHomePoint);
        if (cell == null)
        {
            MapGenerator.Instance.NotifyGenerationComplete();
            yield break;
        }

        var tempCell = cell.Value;
        tempCell.Type = WorldGrid.CellType.CITY;
        _grid.UpdateCell(bestHomePoint, tempCell);

        yield return StartCoroutine(PlacePOIsCoroutine(bestHomePoint));

        if (debugRenderer && debugRenderer.renderEnabled.Value)
            debugRenderer.BuildMesh();

        // yield return StartCoroutine(PlaceHousesCoroutine(cell.Value));

        BuildRoads(bestHomePoint);

        cityRenderer.BakeBatches();

        MapGenerator.Instance.NotifyGenerationComplete();
    }

    private void BuildRoads(Vector2Int _cityCenter)
    {
        var graph = RoadGraph.Build(_grid, _cityCenter, _placedPOIPositions, WorldGrid.Instance.NearCities);
        RoadBuilder.BuildFromGraph(_grid, graph, roadSettings);
        OnRoadsGenerated?.Invoke();
    }

    private IEnumerator FindSettlePosCoroutine(System.Action<Vector2Int> _onComplete)
    {
        var bestPoint = Vector2Int.zero;
        var bestScore = float.MinValue;

        for (var x = 0; x < _grid.size; x++)
        {
            for (var y = 0; y < _grid.size; y++)
            {
                var point = new Vector2Int(x, y);
                var score = EvaluateSettlePoint(point);

                if (!(score > bestScore)) continue;

                bestScore = score;
                bestPoint = point;
            }

            if (x % 10 == 0)
                yield return null;
        }

        _onComplete?.Invoke(bestPoint);
    }

    private float EvaluateSettlePoint(Vector2Int _point)
    {
        var score = 0f;
        var cell  = _grid.GetCell(_point);

        var nearbyCells = _grid.GetTilesInRadius(_point, settlerSearchRadius);
        foreach (var nearbyCell in nearbyCells)
        {
            switch (nearbyCell.Type)
            {
                case WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER:
                    score += 1f;
                    break;
                case WorldGrid.CellType.PLAIN:
                    score += 0.5f;
                    break;
            }
        }

        var center           = new Vector2Int(_grid.size / 2, _grid.size / 2);
        var distanceToCenter = Vector2Int.Distance(_point, center);
        score -= distanceToCenter * 0.3f;

        if (cell?.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
        {
            score -= 999f;
        }

        return score;
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
        var allPlacedPOIs = _placedPOIPositions;

        foreach (var poiData in poiDataList)
        {
            if (!poiData) continue;

            var buildingData = poiData.BuildingData;

            var poiSpawnCount = Random.Range(poiData.SpawnRange.x, poiData.SpawnRange.y + 1);
            for (var i = 0; i < poiSpawnCount; i++)
            {
                var bestPos      = Vector2Int.zero;
                var bestScore    = float.MinValue;
                var bestRotation = 0;
                var found        = false;

                for (var x = 0; x < _grid.size; x++)
                {
                    for (var y = 0; y < _grid.size; y++)
                    {
                        var pos = new Vector2Int(x, y);

                        if (!POIRulesValidator.IsValid(poiData, pos, _grid, allPlacedPOIs))
                            continue;

                        var rotation = 0;
                        if (buildingData && buildingData.buildingArea is { Count: > 0 })
                        {
                            rotation = BuildingAreaHelper.FindBestRotation(buildingData, pos, _grid);
                            if (rotation < 0) continue;
                        }

                        var score = POIRulesValidator.Score(poiData, pos, _grid, allPlacedPOIs, _cityCenter);
                        if (!(score > bestScore)) continue;

                        bestScore    = score;
                        bestPos      = pos;
                        bestRotation = rotation;
                        found        = true;
                    }

                    if (x % 10 == 0)
                        yield return null;
                }

                if (!found) continue;

                if (buildingData && buildingData.buildingArea is { Count: > 0 })
                    BuildingAreaHelper.MarkCellAsOccupied(buildingData, bestPos, bestRotation, _grid);

                var cell = _grid.Cells[bestPos.x, bestPos.y];
                cell.POI = poiData;
                _grid.UpdateCell(bestPos, cell);
                allPlacedPOIs.Add(bestPos);

                Debug.Log($"[POI] Placed {poiData.Type} ({i + 1}/{poiSpawnCount}) at {bestPos} with score {bestScore}");
            }
        }
    }

    private void GenerateNearCitiesData()
    {
        const int MAX_NEAR_CITIES = 4;
        var       cityCount       = Random.Range(0, MAX_NEAR_CITIES + 1);
        var       nearCitiesData  = new List<WorldGrid.NearCityData>();

        const int EDGE_OFFSET = 10;
        for (var i = 0; i < cityCount; i++)
        {
            var edge = Random.Range(0, 4);
            var pos = edge switch
            {
                0 => new Vector2Int(-EDGE_OFFSET,             Random.Range(0, _grid.size)), // left
                1 => new Vector2Int(_grid.size + EDGE_OFFSET, Random.Range(0, _grid.size)), // right
                2 => new Vector2Int(Random.Range(0, _grid.size), -EDGE_OFFSET), // bottom
                3 => new Vector2Int(Random.Range(0, _grid.size), _grid.size + EDGE_OFFSET), // top
                _ => Vector2Int.zero
            };

            var distanceToCenter = Vector2Int.Distance(pos, new Vector2Int(_grid.size / 2, _grid.size / 2));
            var randomExtraDistance = Random.Range(0f, 20f);
            nearCitiesData.Add(new WorldGrid.NearCityData
            {
                CityPos  = pos,
                Distance = distanceToCenter / Constants.CELL_TO_METER + randomExtraDistance
            });
        }

        nearCitiesData.Sort((_a, _b) => _a.Distance.CompareTo(_b.Distance));
        WorldGrid.Instance.NearCities = nearCitiesData;

        var names = GameManager.Instance.Config
            ? GameManager.Instance.Config.GetRandomCityNames(nearCitiesData.Count)
            : null;

        for (var i = 0; i < nearCitiesData.Count; i++)
        {
            nearCitiesData[i].Name = names != null ? names[i] : $"City {i + 1}";
        }

        if (!nearbyCityPool) return;

        nearbyCityPool.ReleaseAll();

        foreach (var nearCity in nearCitiesData)
        {
            var display = nearbyCityPool.Get();
            display.SetCityInfo(nearCity.Name, nearCity.Distance);

            var worldPos = _grid.CellToWorld(nearCity.CityPos);
            display.transform.position = worldPos;
        }
    }
}