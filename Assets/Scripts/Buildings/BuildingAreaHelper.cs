using UnityEngine;

public static class BuildingAreaHelper
{
    /// <summary>
    /// Checks if a building can be placed at the given position and rotation on the grid.
    /// Verify if all cells in the building area are empty and if the height difference is within the flat tolerance.
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="_position"></param>
    /// <param name="_rotation"></param>
    /// <param name="_grid"></param>
    /// <returns></returns>
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

            // Check the height difference against the origin cell
            if (Mathf.Abs(cell.Value.Height - originHeight) > _data.flatTolerance)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Marks the cells occupied by the building as occupied on the grid.
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="_position"></param>
    /// <param name="_rotation"></param>
    /// <param name="_grid"></param>
    public static void MarkCellAsOccupied(BuildingData _data, Vector2Int _position, int _rotation, WorldGrid _grid)
    {
        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition = _position + rotatedOffset;
            _grid.SetCellOccupied(cellPosition);
        }
    }

    public static void MarkAreaAsPoi(BuildingData _data, Vector2Int _position, int _rotation,
                                          WorldGrid _grid, POIData _poiData)
    {
        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition  = _position + rotatedOffset;
            if (!_grid.IsInBounds(cellPosition)) continue;

            var cell       = _grid.Cells[cellPosition.x, cellPosition.y];
            cell.POI        = _poiData;
            cell.IsOccupied = true;
            _grid.Cells[cellPosition.x, cellPosition.y] = cell;
        }
    }

    /// <summary>
    /// Flattens the terrain under the building area to ensure a level foundation.
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="_position"></param>
    /// <param name="_rotation"></param>
    /// <param name="_grid"></param>
    /// <param name="_heightStep"></param>
    public static void FlattenArea(BuildingData _data, Vector2Int _position, int _rotation,
                                        WorldGrid _grid, float _heightStep)
    {
        var originCell = _grid.GetCell(_position);
        if (originCell == null) return;

        // Determine the target height based on the origin cell and height step
        var targetHeight = _heightStep > 0
            ? MathHelper.Quantize(originCell.Value.Height, _heightStep)
            : originCell.Value.Height;

        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition  = _position + rotatedOffset;
            if (!_grid.IsInBounds(cellPosition)) continue;

            // Update the cell height
            var cell   = _grid.Cells[cellPosition.x, cellPosition.y];
            cell.Height = targetHeight;
            _grid.Cells[cellPosition.x, cellPosition.y] = cell;
        }
    }

    /// <summary>
    /// Finds the best rotation for placing the building at the given position on the grid.
    /// Evaluates all four rotations:
    /// - Checks if the building can be placed at the position with the current rotation.
    /// - Computes the height variance of the cells under the building area for that rotation.
    /// - Selects the rotation with the lowest height variance, indicating the flattest placement.
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="_position"></param>
    /// <param name="_grid"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Computes the height variance of the cells under the building area for a given position and rotation.
    /// - Iterates through all cells covered by the building area based on the position and rotation.
    /// - Retrieves the height of each cell and tracks the minimum and maximum heights.
    /// - Returns the difference between the maximum and minimum heights as the variance.
    /// </summary>
    /// <param name="_data"></param>
    /// <param name="_position"></param>
    /// <param name="_rotation"></param>
    /// <param name="_grid"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Rotates a given offset by the specified rotation (0, 90, 180, or 270 degrees).
    /// </summary>
    /// <param name="_offset"></param>
    /// <param name="_rotation"></param>
    /// <returns></returns>
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