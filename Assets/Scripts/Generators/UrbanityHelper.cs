using System.Collections;
using Generators;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class UrbanityHelper
{
    public static bool IsComputed { get; private set; }

    public static void Reset() => IsComputed = false;

    public static IEnumerator Compute(WorldGrid _grid, Vector2Int _cityCenter, UrbanityConfig _config)
    {
        if (IsComputed) yield break;

        if (!_config)
        {
            SetDefaultUrbanity(_grid);
            IsComputed = true;
            yield break;
        }

        var totalCells = _grid.size * _grid.size;
        var results    = new NativeArray<float>(totalCells, Allocator.Persistent);

        var job = new UrbanityJob
        {
            GridSize       = _grid.size,
            CitySettlePos  = new int2(_cityCenter.x, _cityCenter.y),
            MaxRadius      = _config.maxRadius,
            NoiseScale     = _config.noiseScale,
            NoiseAmplitude = _config.noiseAmplitude,
            Results        = results,
            Seed           = GameManager.Instance.RandomEngine.Next()
        };

        yield return GenerationJobManager.Instance.StartCoroutine(
            GenerationJobManager.DispatchJob(job, totalCells, 64,
                                             _completed => ApplyResults(_completed, _grid, totalCells),
                                             results));

        IsComputed = true;
    }

    private static void ApplyResults(UrbanityJob _job, WorldGrid _grid, int _totalCells)
    {
        for (var i = 0; i < _totalCells; i++)
        {
            var x    = i % _grid.size;
            var y    = i / _grid.size;
            var cell = _grid.Cells[x, y];
            cell.UrbanityLevel = _job.Results[i];
            _grid.Cells[x, y]  = cell;
        }
    }

    private static void SetDefaultUrbanity(WorldGrid _grid)
    {
        for (var x = 0; x < _grid.size; x++)
        {
            for (var y = 0; y < _grid.size; y++)
            {
                var cell = _grid.Cells[x, y];
                cell.UrbanityLevel = 1f;
                _grid.Cells[x, y]  = cell;
            }
        }
    }
}