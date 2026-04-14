using System.Collections;
using Unity.Collections;
using UnityEngine;

public static class DistanceFieldHelper
{
    public static bool IsComputed { get; private set; }

    public static void Reset() => IsComputed = false;

    public static IEnumerator Compute(WorldGrid _grid, Vector2Int _cityCenter)
    {
        if (IsComputed) yield break;

        var size       = _grid.size;
        var totalCells = size * size;

        var waterDist  = new NativeArray<float>(totalCells, Allocator.Persistent);
        var roadDist   = new NativeArray<float>(totalCells, Allocator.Persistent);
        var centerDist = new NativeArray<float>(totalCells, Allocator.Persistent);

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var idx  = y * size + x;
                var cell = _grid.Cells[x, y];

                waterDist[idx] = cell.Is(WorldGrid.CellType.WATER, WorldGrid.CellType.RIVER)
                    ? 0f
                    : float.MaxValue;

                roadDist[idx] = cell.Is(WorldGrid.CellType.ROAD, WorldGrid.CellType.BRIDGE)
                    ? 0f
                    : float.MaxValue;

                centerDist[idx] = x == _cityCenter.x && y == _cityCenter.y
                    ? 0f
                    : float.MaxValue;
            }
        }
        
        yield return DispatchDistanceField(waterDist, size);
        yield return DispatchDistanceField(roadDist, size);
        yield return DispatchDistanceField(centerDist, size);

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var idx  = y * size + x;
                var cell = _grid.Cells[x, y];

                cell.DistanceToWater      = waterDist[idx];
                cell.DistanceToRoad       = roadDist[idx];
                cell.DistanceToCityCenter = centerDist[idx];

                _grid.Cells[x, y] = cell;
            }
        }

        waterDist.Dispose();
        roadDist.Dispose();
        centerDist.Dispose();

        IsComputed = true;
    }

    private static IEnumerator DispatchDistanceField(NativeArray<float> _distances, int _gridSize)
    {
        var job = new DistanceFieldJob
        {
            GridSize  = _gridSize,
            Distances = _distances
        };

        yield return GenerationJobManager.Instance.StartCoroutine(
            GenerationJobManager.DispatchJob(job, _ => { }));
    }
}

