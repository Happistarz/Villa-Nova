using System.Collections.Generic;
using UnityEngine;

public static class RoadBuilder
{
    /// <summary>
    /// Stamps a road onto the grid along the given path with the specified width and road tier
    /// If the path crosses water, it will attempt to place a bridge if the water run is within the max bridge length
    /// If a cell already has a road of a lower tier, it will be upgraded to the new tier
    /// If a cell already has a road of the same or higher tier, it will be left unchanged
    /// </summary>
    public static void StampRoad(List<Vector2Int>   _path, int _width, WorldGrid _grid, int _maxBridgeLength,
                                 RoadGraph.EdgeType _roadTier)
    {
        var bridgeCells = new HashSet<Vector2Int>();
        CollectBridgeCells(_path, _grid, _maxBridgeLength, bridgeCells);

        var bridgeHeights = ComputeBridgeHeights(_path, _grid, bridgeCells);

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
                        if (bridgeHeights.TryGetValue(center, out var bh))
                            cell.Height = bh;
                        _grid.UpdateCell(pos, cell);
                        continue;
                    }

                    if (!CanPlaceRoad(pos, _grid)) continue;
                    if (cell.Is(WorldGrid.CellType.ROAD)) continue;

                    if (cell.RoadTier > 0 && cell.RoadTier < (byte)_roadTier) continue;

                    cell.RoadTier = (byte)_roadTier;
                    cell.Type     = WorldGrid.CellType.ROAD;
                    _grid.UpdateCell(pos, cell);
                }
            }
        }
    }

    /// <summary>
    /// Computes interpolated heights for bridge cells based on the shore endpoints on each side
    /// </summary>
    private static Dictionary<Vector2Int, float> ComputeBridgeHeights(List<Vector2Int>    _path, WorldGrid _grid,
                                                                      HashSet<Vector2Int> _bridgeCells)
    {
        var heights = new Dictionary<Vector2Int, float>();
        var i       = 0;

        while (i < _path.Count)
        {
            if (!_bridgeCells.Contains(_path[i]))
            {
                i++;
                continue;
            }

            var runStart = i;

            var startHeight = 0f;
            for (var b = runStart - 1; b >= 0; b--)
            {
                var p = _path[b];
                if (!_grid.IsInBounds(p) ||
                    _grid.Cells[p.x, p.y].Is(WorldGrid.CellType.WATER, WorldGrid.CellType.RIVER)) continue;

                startHeight = _grid.Cells[p.x, p.y].Height;
                break;
            }

            while (i < _path.Count && _bridgeCells.Contains(_path[i])) i++;
            var runEnd = i - 1;

            var endHeight = startHeight;
            for (var a = runEnd + 1; a < _path.Count; a++)
            {
                var p = _path[a];
                if (!_grid.IsInBounds(p) ||
                    _grid.Cells[p.x, p.y].Is(WorldGrid.CellType.WATER, WorldGrid.CellType.RIVER)) continue;

                endHeight = _grid.Cells[p.x, p.y].Height;
                break;
            }

            var runLength = runEnd - runStart;
            for (var j = runStart; j <= runEnd; j++)
            {
                var t = runLength > 0 ? (float)(j - runStart) / runLength : 0.5f;
                heights[_path[j]] = Mathf.Lerp(startHeight, endHeight, t);
            }
        }

        return heights;
    }

    /// <summary>
    /// Collects all water cells along the path that are within the max bridge length and adds them to the bridgeCells set
    /// </summary>
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
    /// Checks if the current run of water cells is a valid bridge and adds it to the bridgeCells set if so
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