using System.Collections;
using System.Collections.Generic;
using Core.Extensions;
using Unity.Mathematics;
using UnityEngine;

public class HouseGenerator : MonoBehaviour
{
    public CityGenerator cityGenerator;

    [Header("Config")]
    public HousePlacementConfig config;

    [Header("Cluster")]
    [Range(1, 5)] public int placementPasses = 3;

    private WorldGrid _grid;
    private int       _noiseSeed;

    private readonly List<Vector2Int> _placedPositions = new();

    private void Start()
    {
        _grid = WorldGrid.Instance;
    }

    public IEnumerator PlaceHousesCoroutine()
    {
        var buildingList = GameManager.Instance.Config.GetHousesData();
        if (buildingList == null || buildingList.Length == 0)
            yield break;

        var rng      = GameManager.Instance.RandomEngine;
        var gridSize = _grid.size;
        _noiseSeed = rng.Next();

        _placedPositions.Clear();

        InitHouseDistanceField(gridSize);

        for (var pass = 0; pass < placementPasses; pass++)
        {
            var processed    = 0;
            var placedInPass = 0;

            for (var x = 0; x < gridSize; x++)
            {
                for (var y = 0; y < gridSize; y++)
                {
                    var cell = _grid.Cells[x, y];

                    if (!BuildingAreaHelper.IsCellValidForBuilding(cell)) continue;

                    if (cell.UrbanityLevel < config.edgeFadeStart && config.edgeFadeStrength > 0f)
                    {
                        var fadeFactor = 1f - cell.UrbanityLevel / config.edgeFadeStart;
                        var skipChance = fadeFactor * config.edgeFadeStrength;
                        if (rng.Range(0f, 1f) < skipChance) continue;
                    }

                    var pos = new Vector2Int(x, y);

                    var score = CalculateHouseScore(pos);
                    if (score < config.placementThreshold) continue;

                    if (rng.Range(0f, 1f) > score) continue;

                    var buildingData = buildingList.RandomItem(rng);
                    if (PlaceHouseAt(pos, buildingData))
                        placedInPass++;

                    if (++processed % 10 == 0)
                        yield return null;
                }
            }

            if (placedInPass == 0) break;

            RecalculateHouseDistanceField(gridSize);
        }
    }

    private void InitHouseDistanceField(int _size)
    {
        for (var x = 0; x < _size; x++)
        {
            for (var y = 0; y < _size; y++)
            {
                var cell = _grid.Cells[x, y];
                cell.DistanceToHouse = float.MaxValue;
                _grid.Cells[x, y]    = cell;
            }
        }
    }

    /// <summary>
    /// Updates the DistanceToHouse field for all cells within clusterMaxDistance of any placed house
    /// </summary>
    private void RecalculateHouseDistanceField(int _size)
    {
        var maxDist    = config.clusterMaxDistance;
        var maxDistInt = Mathf.CeilToInt(maxDist);

        foreach (var housePos in _placedPositions)
        {
            var minX = Mathf.Max(0, housePos.x - maxDistInt);
            var maxX = Mathf.Min(_size         - 1, housePos.x + maxDistInt);
            var minY = Mathf.Max(0, housePos.y - maxDistInt);
            var maxY = Mathf.Min(_size         - 1, housePos.y + maxDistInt);

            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var dist = Mathf.Sqrt((x - housePos.x) * (x - housePos.x) +
                                          (y - housePos.y) * (y - housePos.y));

                    if (dist >= _grid.Cells[x, y].DistanceToHouse) continue;

                    var cell = _grid.Cells[x, y];
                    cell.DistanceToHouse = dist;
                    _grid.Cells[x, y]    = cell;
                }
            }
        }
    }

    /// <summary>Returns true if the house was successfully placed.</summary>
    private bool PlaceHouseAt(Vector2Int _pos, BuildingData _buildingData)
    {
        var rotation = BuildingAreaHelper.FindBestRotation(_buildingData, _pos, _grid);
        if (rotation < 0) return false;

        BuildingAreaHelper.FlattenArea(_buildingData, _pos, rotation, _grid, MapGenerator.Instance.heightStep);
        BuildingAreaHelper.MarkAreaAsHouse(_buildingData, _pos, rotation, _grid);

        _placedPositions.Add(_pos);

        var buildingPosition = BuildingAreaHelper.GetAreaCenter(_buildingData, _pos, rotation, _grid);

        if (cityGenerator.cityRenderer)
            cityGenerator.cityRenderer.AddBuilding(
                buildingPosition,
                rotation, _buildingData);

        return true;
    }

    private float CalculateHouseScore(Vector2Int _pos)
    {
        var cell = _grid.Cells[_pos.x, _pos.y];
        var cfg  = config;

        var urbanityScore   = cell.UrbanityLevel;
        var waterScore      = ProximityScore(cell.DistanceToWater,      cfg.waterMaxDistance);
        var roadScore       = ProximityScore(cell.DistanceToRoad,       cfg.roadMaxDistance);
        var cityCenterScore = ProximityScore(cell.DistanceToCityCenter, cfg.centerMaxDistance);
        var clusterScore    = ProximityScore(cell.DistanceToHouse,      cfg.clusterMaxDistance);

        var roadDepthScore = RoadDepthScore(cell.DistanceToRoad, cfg.roadDepthIdeal, cfg.roadDepthMax);

        var totalWeight = cfg.urbanityWeight     + cfg.waterWeight        + cfg.roadWeight
                          + cfg.cityCenterWeight + cfg.houseClusterWeight + cfg.roadDepthWeight;

        var score = 0f;
        if (totalWeight > 0f)
        {
            score = (urbanityScore     * cfg.urbanityWeight
                     + waterScore      * cfg.waterWeight
                     + roadScore       * cfg.roadWeight
                     + cityCenterScore * cfg.cityCenterWeight
                     + clusterScore    * cfg.houseClusterWeight
                     + roadDepthScore  * cfg.roadDepthWeight) / totalWeight;
        }

        var nx       = _pos.x * cfg.noiseScale + _noiseSeed;
        var ny       = _pos.y * cfg.noiseScale + _noiseSeed;
        var noiseVal = Mathf.PerlinNoise(nx, ny);
        noiseVal =  math.remap(0f, 1f, -1f, 1f, noiseVal);
        score    += noiseVal * cfg.noiseAmplitude;

        return Mathf.Clamp01(score);
    }

    /// <summary>
    /// Bell-curve score that rewards cells that are a few cells away from a road, not directly adjacent
    /// </summary>
    private static float RoadDepthScore(float _distance, float _ideal, float _max)
    {
        if (_distance >= _max) return 0f;
        if (_distance <= 0f) return 0f;

        return _distance <= _ideal
            ? _distance / _ideal
            : 1f - (_distance - _ideal) / (_max - _ideal);
    }

    /// <summary>Converts a raw distance into a 0 to 1 proximity score</summary>
    private static float ProximityScore(float _distance, float _maxDistance)
    {
        return 1f - Mathf.InverseLerp(0f, _maxDistance, _distance);
    }
}