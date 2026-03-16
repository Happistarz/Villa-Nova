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
    [ReadOnly] public NativeArray<JobPoiRule>                   Rules;
    [ReadOnly] public NativeArray<int2>                         ExistingPois;
    [ReadOnly] public int2                                      CityCenter;
    [ReadOnly] public int                                       GridSize;

    public NativeArray<float> Results;

    public void Execute(int _index)
    {
        var x    = _index % GridSize;
        var y    = _index / GridSize;
        var cell = GridCells[_index];
        var pos  = new float2(x, y);

        if (cell.IsOccupied || cell.HasPoi
                            || cell.Type == WorldGrid.CellType.WATER
                            || cell.Type == WorldGrid.CellType.RIVER)
        {
            Results[_index] = float.MinValue;
            return;
        }

        var score = 0f;

        for (var i = 0; i < Rules.Length; i++)
        {
            var rule      = Rules[i];
            var ruleType  = (POIData.POIRule)rule.RuleTypeInt;
            var ruleScore = 0f;
            var valid     = true;

            switch (ruleType)
            {
                case POIData.POIRule.NEAR_CITY:
                    ruleScore = GetProximityScore(x, y, WorldGrid.CellType.CITY, rule.Value);
                    if (ruleScore < 0) valid = false;
                    break;

                case POIData.POIRule.NEAR_WATER:
                    ruleScore = math.max(
                        GetProximityScore(x, y, WorldGrid.CellType.WATER, rule.Value),
                        GetProximityScore(x, y, WorldGrid.CellType.RIVER, rule.Value));
                    if (ruleScore < 0) valid = false;
                    break;

                case POIData.POIRule.NEAR_ROAD:
                    ruleScore = math.max(
                        GetProximityScore(x, y, WorldGrid.CellType.ROAD,   rule.Value),
                        GetProximityScore(x, y, WorldGrid.CellType.BRIDGE, rule.Value));
                    if (ruleScore < 0) valid = false;
                    break;

                case POIData.POIRule.POI_DISTANCE:
                    if (!IsMinDistanceFromPois(pos, rule.Value))
                        valid = false;
                    else
                        ruleScore = 1f;
                    break;
            }

            if (!valid)
            {
                Results[_index] = float.MinValue;
                return;
            }

            score += ruleScore * rule.Weight;
        }

        score -= math.distance(pos, new float2(CityCenter.x, CityCenter.y)) * 0.1f;
        Results[_index] = score;
    }

    private float GetProximityScore(int _cx, int _cy, WorldGrid.CellType _type, float _radius)
    {
        var r          = (int)math.ceil(_radius);
        var radiusSq   = _radius * _radius;
        var bestDistSq = float.MaxValue;
        var found      = false;

        for (var dx = -r; dx <= r; dx++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                var distSq = dx * dx + dy * dy;
                if (distSq > radiusSq) continue;

                var tx = _cx + dx;
                var ty = _cy + dy;
                if (tx < 0 || tx >= GridSize || ty < 0 || ty >= GridSize) continue;

                if (GridCells[ty * GridSize + tx].Type != _type) continue;
                if (!(distSq < bestDistSq)) continue;

                bestDistSq = distSq;
                found      = true;
            }
        }

        if (!found) return -1f;
        return 1f - math.sqrt(bestDistSq) / _radius;
    }

    private bool IsMinDistanceFromPois(float2 _pos, float _minDistance)
    {
        var minDistSq = _minDistance * _minDistance;

        for (var i = 0; i < ExistingPois.Length; i++)
        {
            var p = ExistingPois[i];
            if (math.distancesq(_pos, new float2(p.x, p.y)) < minDistSq)
                return false;
        }

        return true;
    }
}
