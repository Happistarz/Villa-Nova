using UnityEngine;

public static class RoadCostCalculator
{
    public delegate float CostFunc(Vector2Int _from, Vector2Int _to, WorldGrid _grid);
    
    public static float BaseCost(Vector2Int _from, Vector2Int _to, WorldGrid _grid) => 1f;

    public static float WaterPenalty(Vector2Int _from, Vector2Int _to, WorldGrid _grid, float _penalty)
    {
        if (!_grid.IsInBounds(_to)) return 0f;

        var cell = _grid.Cells[_to.x, _to.y];
        return cell.Type is WorldGrid.CellType.WATER or WorldGrid.CellType.RIVER ? _penalty : 0f;
    }

    public static float ElevationCost(Vector2Int _from, Vector2Int _to, WorldGrid _grid, float _multiplier)
    {
        if (!_grid.IsInBounds(_from) || !_grid.IsInBounds(_to)) return 0f;

        var fromH = _grid.Cells[_from.x, _from.y].Height;
        var toH   = _grid.Cells[_to.x, _to.y].Height;
        return Mathf.Abs(toH - fromH) * _multiplier;
    }

    public static float RoadBonus(Vector2Int _from, Vector2Int _to, WorldGrid _grid, float _bonus)
    {
        if (!_grid.IsInBounds(_to)) return 0f;

        var cell = _grid.Cells[_to.x, _to.y];
        return cell.Type is WorldGrid.CellType.ROAD or WorldGrid.CellType.BRIDGE ? -_bonus : 0f;
    }

    public static float OccupiedPenalty(Vector2Int _from, Vector2Int _to, WorldGrid _grid, float _penalty)
    {
        if (!_grid.IsInBounds(_to)) return 0f;

        var cell = _grid.Cells[_to.x, _to.y];
        if (cell.IsOccupied || cell.POI) return _penalty;
        return 0f;
    }

    public static float TerrainNoiseCost(Vector2Int _to, float _strength, float _scale,
                                          float _offsetX, float _offsetY)
    {
        var nx = _to.x * _scale + _offsetX;
        var ny = _to.y * _scale + _offsetY;
        return Mathf.PerlinNoise(nx, ny) * _strength;
    }

    public static CostFunc BuildCostFunction(RoadSettings _settings)
    {
        return (_from, _to, _grid) =>
        {
            var cost = BaseCost(_from, _to, _grid);
            cost += WaterPenalty(_from, _to, _grid, _settings.waterPenalty);
            cost += ElevationCost(_from, _to, _grid, _settings.elevationMultiplier);
            cost += RoadBonus(_from, _to, _grid, _settings.roadBonus);
            cost += RoadProximityBonus(_from, _to, _grid, _settings.roadProximityBonus, _settings.roadProximityRadius);
            cost += OccupiedPenalty(_from, _to, _grid, _settings.occupiedPenalty);
            cost += TerrainNoiseCost(_to, _settings.noiseStrength, _settings.noiseScale,
                                     _settings.noiseOffsetX, _settings.noiseOffsetY);
            return Mathf.Max(0.1f, cost);
        };
    }
    
    public static float RoadProximityBonus(Vector2Int _from, Vector2Int _to, WorldGrid _grid, float _bonus, int _radius)
    {
        if (!_grid.IsInBounds(_to)) return 0f;

        for (var dx = -_radius; dx <= _radius; dx++)
        {
            for (var dy = -_radius; dy <= _radius; dy++)
            {
                var nx = _to.x + dx;
                var ny = _to.y + dy;

                if (!_grid.IsInBounds(new Vector2Int(nx, ny))) continue;

                var cell = _grid.Cells[nx, ny];
                if (cell.Type is WorldGrid.CellType.ROAD or WorldGrid.CellType.BRIDGE)
                    return -_bonus;
            }
        }

        return 0f;
    }
}