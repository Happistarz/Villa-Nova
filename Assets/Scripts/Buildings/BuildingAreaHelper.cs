using UnityEngine;

public static class BuildingAreaHelper
{
    /// <summary>Checks if the cell is a valid starting point for placing a building</summary>
    public static bool IsCellValidForBuilding(WorldGrid.Cell _cell)
    {
        return _cell.Is(WorldGrid.CellType.PLAIN, WorldGrid.CellType.CITY) && !_cell.IsOccupied;
    }
    
    /// <summary>
    /// Returns true if the building fits at the position and rotation
    /// Checks that all cells are empty and height difference is within tolerance
    /// </summary>
    public static bool CanPlace(BuildingData _data, Vector2Int _position, int _rotation, WorldGrid _grid)
    {
        var originCell = _grid.GetCell(_position);
        if (originCell == null) return false;

        var originHeight = originCell.Value.Height;

        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition  = _position + rotatedOffset;

            var cell = _grid.GetCell(cellPosition);
            if (cell == null) return false;

            if (!IsCellValidForBuilding(cell.Value))
                return false;

            if (_data.flatTolerance <= 0) continue;

            if (Mathf.Abs(cell.Value.Height - originHeight) > _data.flatTolerance)
                return false;
        }

        return true;
    }

    /// <summary>Marks all cells under the building footprint as occupied by a POI</summary>
    public static void MarkAreaAsPoi(BuildingData _data, Vector2Int _position, int _rotation,
                                     WorldGrid _grid, POIData _poiData)
    {
        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition  = _position + rotatedOffset;
            if (!_grid.IsInBounds(cellPosition)) continue;

            var cell        = _grid.Cells[cellPosition.x, cellPosition.y];
            cell.POI        = _poiData;
            cell.IsOccupied = true;
            _grid.Cells[cellPosition.x, cellPosition.y] = cell;
        }
    }

    /// <summary>Marks all cells under the building footprint as HOUSE and occupied</summary>
    public static void MarkAreaAsHouse(BuildingData _data, Vector2Int _position, int _rotation, WorldGrid _grid)
    {
        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition  = _position + rotatedOffset;
            if (!_grid.IsInBounds(cellPosition)) continue;

            var cell        = _grid.Cells[cellPosition.x, cellPosition.y];
            cell.Type       = WorldGrid.CellType.HOUSE;
            cell.IsOccupied = true;
            _grid.Cells[cellPosition.x, cellPosition.y] = cell;
        }
    }

    /// <summary>Levels the terrain under the building to a quantized height</summary>
    public static void FlattenArea(BuildingData _data, Vector2Int _position, int _rotation,
                                   WorldGrid _grid, float _heightStep)
    {
        var originCell = _grid.GetCell(_position);
        if (originCell == null) return;

        var targetHeight = _heightStep > 0
            ? MathHelper.Quantize(originCell.Value.Height, _heightStep)
            : originCell.Value.Height;

        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition  = _position + rotatedOffset;
            if (!_grid.IsInBounds(cellPosition)) continue;

            var cell    = _grid.Cells[cellPosition.x, cellPosition.y];
            cell.Height = targetHeight;
            _grid.Cells[cellPosition.x, cellPosition.y] = cell;
        }
    }

    /// <summary>
    /// Tries all 4 rotations and returns the one with the lowest height variance, returns -1 if no rotation fits
    /// </summary>
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
    /// Computes the height variance of the cells under the building footprint at the given position and rotation
    /// </summary>
    private static float ComputeHeightVariance(BuildingData _data, Vector2Int _position, int _rotation, WorldGrid _grid)
    {
        var minH = float.MaxValue;
        var maxH = float.MinValue;

        foreach (var offset in _data.buildingArea)
        {
            var rotatedOffset = RotateOffset(offset, _rotation);
            var cellPosition  = _position + rotatedOffset;
            var cell          = _grid.GetCell(cellPosition);
            if (cell == null) continue;

            var h = cell.Value.Height;
            if (h < minH) minH = h;
            if (h > maxH) maxH = h;
        }

        return maxH - minH;
    }

    /// <summary>Rotates an offset by 0, 90, 180 or 270 degrees</summary>
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

    /// <summary>
    /// Returns the world-space center of the building footprint at the given grid position and rotation.
    /// Averages all rotated cell positions to find the geometric center.
    /// </summary>
    public static Vector3 GetAreaCenter(BuildingData _data, Vector2Int _position, int _rotation, WorldGrid _grid)
    {
        if (_data.buildingArea == null || _data.buildingArea.Count == 0)
            return _grid.CellToWorld(_position);

        var sum   = Vector3.zero;
        var count = 0;

        foreach (var offset in _data.buildingArea)
        {
            var rotated  = RotateOffset(offset, _rotation);
            var cellPos  = _position + rotated;
            sum += _grid.CellToWorld(cellPos);
            count++;
        }

        return sum / count;
    }
}