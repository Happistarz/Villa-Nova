using System.Collections.Generic;
using UnityEngine;

public static class RoadBuilder
{

    public static void StampRoad(List<Vector2Int>   _path, int _width, WorldGrid _grid, int _maxBridgeLength,
                                 RoadGraph.EdgeType _roadTier)
    {
        var stampedCount = 0;
        var bridgeCells  = new HashSet<Vector2Int>();
        CollectBridgeCells(_path, _grid, _maxBridgeLength, bridgeCells);

        var half = _width / 2;

        foreach (var center in _path)
        {
            for (var dx = -half; dx <= half; dx++)
            {
                for (var dy = -half; dy <= half; dy++)
                {
                    var pos = new Vector2Int(center.x + dx, center.y + dy);
                    if (!_grid.IsInBounds(pos)) continue;

                    var cell = _grid.Cells[pos.x, pos.y];

                    if (cell.Is(WorldGrid.CellType.WATER, WorldGrid.CellType.RIVER)
                        && bridgeCells.Contains(center))
                    {
                        if (cell.Is(WorldGrid.CellType.BRIDGE)) continue;

                        cell.Type = WorldGrid.CellType.BRIDGE;
                        _grid.UpdateCell(pos, cell);
                        stampedCount++;
                        continue;
                    }

                    if (!CanPlaceRoad(pos, _grid)) continue;
                    if (cell.Is(WorldGrid.CellType.ROAD)) continue;
                    
                    if (cell.RoadTier > 0 && cell.RoadTier < (byte)_roadTier) continue;

                    cell.RoadTier = (byte)_roadTier;
                    cell.Type = WorldGrid.CellType.ROAD;
                    _grid.UpdateCell(pos, cell);
                    stampedCount++;
                }
            }
        }
    }

    private static void CollectBridgeCells(List<Vector2Int> _path,            WorldGrid           _grid,
                                           int              _maxBridgeLength, HashSet<Vector2Int> _bridgeCells)
    {
        var waterRun = new List<Vector2Int>();

        foreach (var pos in _path)
        {
            if (!_grid.IsInBounds(pos))
            {
                FlushWaterRun(waterRun, _maxBridgeLength, _bridgeCells);
                continue;
            }

            var cell = _grid.Cells[pos.x, pos.y];

            if (cell.Is(WorldGrid.CellType.WATER, WorldGrid.CellType.RIVER))
                waterRun.Add(pos);
            else
                FlushWaterRun(waterRun, _maxBridgeLength, _bridgeCells);
        }

        FlushWaterRun(waterRun, _maxBridgeLength, _bridgeCells);
    }

    /// <summary>
    /// Checks if the current run of water cells is a valid bridge (not too long) and adds it to the bridgeCells set if so.
    /// </summary>
    private static void FlushWaterRun(List<Vector2Int>    _waterRun, int _maxLength,
                                      HashSet<Vector2Int> _bridgeCells)
    {
        if (_waterRun.Count > 0 && _waterRun.Count <= _maxLength)
        {
            foreach (var pos in _waterRun)
                _bridgeCells.Add(pos);
        }

        _waterRun.Clear();
    }

    public static bool CanPlaceRoad(Vector2Int _pos, WorldGrid _grid)
    {
        if (!_grid.IsInBounds(_pos)) return false;

        var cell = _grid.Cells[_pos.x, _pos.y];

        if (!cell.Is(WorldGrid.CellType.PLAIN)) return false;
        if (cell.IsOccupied) return false;
        return !cell.POI;
    }

    public static Vector2Int ClampToGrid(Vector2Int _pos, WorldGrid _grid)
    {
        return new Vector2Int(
            Mathf.Clamp(_pos.x, 0, _grid.size - 1),
            Mathf.Clamp(_pos.y, 0, _grid.size - 1)
        );
    }
}