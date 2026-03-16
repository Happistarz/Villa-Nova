using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class CityGenerationJobRunner
{
    public static IEnumerator FindBestSettlePoint(WorldGrid _grid, float _radius, Action<Vector2Int> _onComplete)
    {
        var totalCells = _grid.size * _grid.size;
        var gridData   = GridJobUtilities.GetFlatGridData(_grid, Allocator.Persistent);
        var scores     = new NativeArray<float>(totalCells, Allocator.Persistent);

        var job = new SettleScoreJob
        {
            GridCells    = gridData,
            Size         = _grid.size,
            SearchRadius = _radius,
            Results      = scores
        };

        yield return GenerationJobManager.Instance.StartCoroutine(
            GenerationJobManager.DispatchJob(job, totalCells, 64,
                _completed => ProcessSettleResults(_completed, _grid, totalCells, _onComplete),
                gridData, scores));
    }

    private static void ProcessSettleResults(SettleScoreJob _job, WorldGrid _grid, int _totalCells,
                                             Action<Vector2Int> _onComplete)
    {
        var bestScore = float.MinValue;
        var bestIndex = 0;

        for (var i = 0; i < _totalCells; i++)
        {
            if (!(_job.Results[i] > bestScore)) continue;
            bestScore = _job.Results[i];
            bestIndex = i;
        }

        _onComplete?.Invoke(new Vector2Int(bestIndex % _grid.size, bestIndex / _grid.size));
    }

    public static IEnumerator ComputePaths(WorldGrid _grid, List<PathRequest> _requests,
                                           RoadSettings _settings,
                                           Action<List<List<Vector2Int>>> _onComplete)
    {
        if (_requests.Count == 0)
        {
            _onComplete?.Invoke(new List<List<Vector2Int>>());
            yield break;
        }

        var gridData    = GridJobUtilities.GetFlatGridData(_grid, Allocator.Persistent);
        var requests    = new NativeArray<PathRequest>(_requests.Count, Allocator.Persistent);
        var allPaths    = new NativeArray<int2>(_requests.Count * PathfindingProcessJob.MAX_PATH_LENGTH, Allocator.Persistent);
        var pathLengths = new NativeArray<int>(_requests.Count, Allocator.Persistent);

        for (var i = 0; i < _requests.Count; i++)
            requests[i] = _requests[i];

        var job = new PathfindingProcessJob
        {
            GridCells            = gridData,
            GridSize             = _grid.size,
            Requests             = requests,
            WaterPenalty         = _settings.waterPenalty,
            ElevationMultiplier  = _settings.elevationMultiplier,
            OccupiedPenalty      = _settings.occupiedPenalty,
            TerrainNoiseStrength = _settings.noiseStrength,
            TerrainNoiseScale    = _settings.noiseScale,
            AllPaths             = allPaths,
            PathLengths          = pathLengths
        };

        yield return GenerationJobManager.Instance.StartCoroutine(
            GenerationJobManager.DispatchJob(job, _requests.Count, 1,
                _completed =>
                {
                    _onComplete?.Invoke(ExtractPaths(_completed, _requests.Count));
                },
                gridData, requests, allPaths, pathLengths));
    }

    public static IEnumerator FindBestPoiLocation(WorldGrid _grid, POIData _poiData,
                                                  List<Vector2Int> _existingPois,
                                                  Vector2Int _cityCenter,
                                                  Action<List<(Vector2Int, float)>> _onComplete)
    {
        var totalCells   = _grid.size * _grid.size;
        var gridData     = GridJobUtilities.GetFlatGridData(_grid, Allocator.Persistent);
        var scores       = new NativeArray<float>(totalCells, Allocator.Persistent);
        var rules        = new NativeList<JobPoiRule>(Allocator.Persistent);
        var existingPois = new NativeArray<int2>(_existingPois.Count, Allocator.Persistent);

        foreach (var r in _poiData.Rules)
        {
            rules.Add(new JobPoiRule
            {
                RuleTypeInt = (int)r.rule,
                Value       = r.value,
                Weight      = r.scoreWeight
            });
        }

        for (var i = 0; i < _existingPois.Count; i++)
            existingPois[i] = new int2(_existingPois[i].x, _existingPois[i].y);

        var job = new PoiScoreJob
        {
            GridCells    = gridData,
            Rules        = rules.AsArray(),
            ExistingPois = existingPois,
            CityCenter   = new int2(_cityCenter.x, _cityCenter.y),
            GridSize     = _grid.size,
            Results      = scores
        };

        yield return GenerationJobManager.Instance.StartCoroutine(
            GenerationJobManager.DispatchJob(job, totalCells, 64,
                _completed =>
                {
                    _onComplete?.Invoke(ExtractPoiCandidates(_completed, _grid.size, totalCells));
                },
                gridData, scores, rules, existingPois));
    }

    private static List<List<Vector2Int>> ExtractPaths(PathfindingProcessJob _job, int _count)
    {
        var result = new List<List<Vector2Int>>(_count);

        for (var i = 0; i < _count; i++)
        {
            var length = _job.PathLengths[i];
            if (length <= 0)
            {
                result.Add(null);
                continue;
            }

            var path = new List<Vector2Int>(length);
            for (var k = 0; k < length; k++)
            {
                var point = _job.AllPaths[i * PathfindingProcessJob.MAX_PATH_LENGTH + k];
                path.Add(new Vector2Int(point.x, point.y));
            }

            result.Add(path);
        }

        return result;
    }

    private static List<(Vector2Int, float)> ExtractPoiCandidates(PoiScoreJob _job, int _gridSize,
                                                                   int _totalCells)
    {
        var candidates = new List<(Vector2Int, float)>();

        for (var i = 0; i < _totalCells; i++)
        {
            var score = _job.Results[i];
            if (score <= float.MinValue + 1f) continue;

            candidates.Add((new Vector2Int(i % _gridSize, i / _gridSize), score));
        }

        candidates.Sort((_a, _b) => _b.Item2.CompareTo(_a.Item2));
        return candidates;
    }
}
