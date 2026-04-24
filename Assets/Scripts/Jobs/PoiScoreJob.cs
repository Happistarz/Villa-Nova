using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct JobPoiRule
{
    public int   RuleTypeInt;
    public float Value;
    public float Weight;
}

[BurstCompile]
public struct PoiScoreJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<GridJobUtilities.JobCellData> GridCells;

    [ReadOnly] public NativeArray<JobPoiRule> Rules;
    [ReadOnly] public NativeArray<int2>       ExistingPois;
    [ReadOnly] public int2                    CityCenter;
    [ReadOnly] public int                     GridSize;
    [ReadOnly] public int                     BuildingSize;
    [ReadOnly] public float                   FlatTolerance;

    public NativeArray<float> Results;

    public void Execute(int _index)
    {
        var x    = _index % GridSize;
        var y    = _index / GridSize;
        var cell = GridCells[_index];
        var pos  = new float2(x, y);

        var heightPenalty = 0f;

        if (cell.IsOccupied || cell.HasPoi
                            || cell.Type == WorldGrid.CellType.WATER
                            || cell.Type == WorldGrid.CellType.RIVER
                            || (BuildingSize > 1 && !CanFitInArea(x, y, cell.Height, out heightPenalty)))
        {
            Results[_index] = float.MinValue;
            return;
        }

        var score = -heightPenalty;

        foreach (var rule in Rules)
        {
            var ruleType  = (POIData.POIRule)rule.RuleTypeInt;
            var ruleScore = 0f;
            var valid     = true;

            // Rules behaviors
            switch (ruleType)
            {
                case POIData.POIRule.NEAR_CITY:
                    ruleScore = GetProximityScore(x, y, WorldGrid.CellType.CITY, rule.Value);
                    if (ruleScore < 0) ruleScore = -2f;
                    break;

                case POIData.POIRule.NEAR_WATER:
                    ruleScore = math.max(
                        GetProximityScore(x, y, WorldGrid.CellType.WATER, rule.Value),
                        GetProximityScore(x, y, WorldGrid.CellType.RIVER, rule.Value));
                    if (ruleScore < 0) ruleScore = -2f;
                    break;

                case POIData.POIRule.NEAR_ROAD:
                    ruleScore = math.max(
                        GetProximityScore(x, y, WorldGrid.CellType.ROAD,   rule.Value),
                        GetProximityScore(x, y, WorldGrid.CellType.BRIDGE, rule.Value));
                    if (ruleScore < 0) ruleScore = 0f;
                    break;

                case POIData.POIRule.POI_DISTANCE:
                    if (!IsMinDistanceFromPOIs(pos, rule.Value))
                        valid = false;
                    else
                        ruleScore = 1f;
                    break;
                
                case POIData.POIRule.HIGH_ELEVATION:
                    ruleScore = cell.Height / rule.Value;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!valid)
            {
                Results[_index] = float.MinValue;
                return;
            }

            score += ruleScore * rule.Weight;
        }

        score           -= math.distance(pos, new float2(CityCenter.x, CityCenter.y)) * 0.1f;
        Results[_index] =  score;
    }

    /// <summary>
    /// Calculates a score based on how close the cell is to the specified type
    /// </summary>
    private float GetProximityScore(int _cx, int _cy, WorldGrid.CellType _type, float _radius)
    {
        var radius         = (int)math.ceil(_radius);
        var radiusSquare   = _radius * _radius;
        var baseDistSquare = float.MaxValue;
        var found          = false;

        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                var distSquare = dx * dx + dy * dy;
                if (distSquare > radiusSquare) continue;

                var tx = _cx + dx;
                var ty = _cy + dy;
                if (tx < 0 || tx >= GridSize || ty < 0 || ty >= GridSize) continue;

                if (GridCells[ty * GridSize + tx].Type != _type) continue;
                if (!(distSquare < baseDistSquare)) continue;

                baseDistSquare = distSquare;
                found          = true;
            }
        }

        if (!found) return -1f;
        return 1f - math.sqrt(baseDistSquare) / _radius;
    }

    /// <summary>
    /// Checks if the position is at least a certain distance away from all existing POIs to avoid overcrowding
    /// </summary>
    private bool IsMinDistanceFromPOIs(float2 _pos, float _minDistance)
    {
        var minDistSquare = _minDistance * _minDistance;

        foreach (var p in ExistingPois)
        {
            if (math.distancesq(_pos, new float2(p.x, p.y)) < minDistSquare)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a building of the specified size can fit in the area without overlapping occupied cells
    /// </summary>
    private bool CanFitInArea(int _cx, int _cy, float _originHeight, out float _heightPenalty)
    {
        var half = BuildingSize / 2;
        _heightPenalty = 0f;

        for (var dx = -half; dx < BuildingSize - half; dx++)
        {
            for (var dy = -half; dy < BuildingSize - half; dy++)
            {
                var nx = _cx + dx;
                var ny = _cy + dy;

                if (nx < 0 || nx >= GridSize || ny < 0 || ny >= GridSize)
                    return false;

                var neighbor = GridCells[ny * GridSize + nx];

                if (neighbor.IsOccupied || neighbor.HasPoi)
                    return false;

                if (neighbor.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER)
                    return false;

                var diff = math.abs(neighbor.Height - _originHeight);

                if (diff > 1.5f)
                    return false;

                if (FlatTolerance >= 0 && diff > FlatTolerance)
                    _heightPenalty += (diff - FlatTolerance) * 5f;
            }
        }

        return true;
    }
}