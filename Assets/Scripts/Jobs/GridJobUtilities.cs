using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public static class GridJobUtilities
{
    /// <summary>Flat cell data struct safe for Burst jobs.</summary>
    public struct JobCellData
    {
        public WorldGrid.CellType Type;
        public float Height;
        public bool IsOccupied;
        public bool HasPoi;
    }

    /// <summary>Copies grid cells into a flat NativeArray for job consumption.</summary>
    public static NativeArray<JobCellData> GetFlatGridData(WorldGrid _grid, Allocator _allocator)
    {
        var size = _grid.size;
        var nativeArray = new NativeArray<JobCellData>(size * size, _allocator);

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var cell = _grid.Cells[x, y];
                var index = y * size + x;

                nativeArray[index] = new JobCellData
                {
                    Type       = cell.Type,
                    Height     = cell.Height,
                    IsOccupied = cell.IsOccupied,
                    HasPoi     = cell.POI
                };
            }
        }

        return nativeArray;
    }

    /// <summary>Collects all flat indices within a radius around a center point.</summary>
    public static void GetIndicesInRadius(int2 _center, float _radius, int _gridSize, NativeList<int> _results)
    {
        var r = Mathf.CeilToInt(_radius);
        var r2 = r * r;

        for (var x = -r; x <= r; x++)
        {
            for (var y = -r; y <= r; y++)
            {
                if (x*x + y*y > r2) continue;

                var targetX = _center.x + x;
                var targetY = _center.y + y;

                if (targetX >= 0 && targetX < _gridSize && targetY >= 0 && targetY < _gridSize) 
                    _results.Add(targetY * _gridSize + targetX);
            }
        }
    }
}
