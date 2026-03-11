using System.Collections.Generic;
using UnityEngine;

public static class BuildingAreaHelper
{
    public static bool CanPlace(BuildingData _data, Vector2Int _position, int _rotation, WorldGrid _grid)
    {
        var originCell = _grid.GetCell(_position);
        if (originCell == null) return false;

        var originHeight = originCell.Value.Height;

        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition = _position + rotatedOffset;

            if (!_grid.IsCellEmpty(cellPosition))
                return false;

            if (_data.flatTolerance <= 0) continue;

            var cell = _grid.GetCell(cellPosition);
            if (cell == null) return false;

            if (Mathf.Abs(cell.Value.Height - originHeight) > _data.flatTolerance)
                return false;
        }

        return true;
    }

    public static List<Vector2Int> GetOccupiedCells(BuildingData _data, Vector2Int _position, int _rotation)
    {
        var occupiedCells = new List<Vector2Int>();

        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition = _position + rotatedOffset;
            occupiedCells.Add(cellPosition);
        }

        return occupiedCells;
    }

    public static void MarkCellAsOccupied(BuildingData _data, Vector2Int _position, int _rotation, WorldGrid _grid)
    {
        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition = _position + rotatedOffset;
            _grid.SetCellOccupied(cellPosition);
        }
    }

    public static int FindBestRotation(BuildingData _data, Vector2Int _position, WorldGrid _grid)
    {
        var bestRotation = -1;
        var bestVariance = float.MaxValue;

        for (var rot = 0; rot < 4; rot++)
        {
            if (!CanPlace(_data, _position, rot, _grid))
                continue;

            var variance = ComputeHeightVariance(_data, _position, rot, _grid);
            if (!(variance < bestVariance)) continue;

            bestVariance = variance;
            bestRotation = rot;
        }

        return bestRotation;
    }

    private static float ComputeHeightVariance(BuildingData _data, Vector2Int _position, int _rotation, WorldGrid _grid)
    {
        var minH = float.MaxValue;
        var maxH = float.MinValue;

        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition = _position + rotatedOffset;
            var cell = _grid.GetCell(cellPosition);
            if (cell == null) continue;

            var h = cell.Value.Height;
            if (h < minH) minH = h;
            if (h > maxH) maxH = h;
        }

        return maxH - minH;
    }

    public static Vector2Int RotateOffset(Vector2Int _offset, int _rotation)
    {
        return (_rotation % 4) switch
        {
            0 => _offset,
            1 => new Vector2Int(-_offset.y, _offset.x),
            2 => new Vector2Int(-_offset.x, -_offset.y),
            3 => new Vector2Int(_offset.y, -_offset.x),
            _ => _offset
        };
    }
}