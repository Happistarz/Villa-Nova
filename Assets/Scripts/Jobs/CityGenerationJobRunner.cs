using System;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class CityGenerationJobRunner
{
    public static IEnumerator FindBestSettlePoint(WorldGrid _grid, float _radius, Action<Vector2Int> _onComplete)
    {
        var totalCells = _grid.size * _grid.size;

        var gridData = GridJobUtilities.GetFlatGridData(_grid, Allocator.TempJob);
        var results = new NativeArray<float>(totalCells, Allocator.TempJob);

        var job = new SettleScoreJob
        {
            GridCells = gridData,
            Size = _grid.size,
            SearchRadius = _radius,
            Results = results
        };

        var handle = job.Schedule(totalCells, 64);

        while (!handle.IsCompleted)
        {
            yield return null;
        }

        handle.Complete();

        var bestScore = float.MinValue;
        var bestIndex = 0;
        
        for (var i = 0; i < totalCells; i++)
        {
            if (!(results[i] > bestScore)) continue;
            
            bestScore = results[i];
            bestIndex = i;
        }

        var bestPoint = new Vector2Int(bestIndex % _grid.size, bestIndex / _grid.size);
        
        gridData.Dispose();
        results.Dispose();

        _onComplete?.Invoke(bestPoint);
    }
}

