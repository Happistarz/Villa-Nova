using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns;
using Generators;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoadGenerator : MonoSingleton<RoadGenerator>, IGenerator
{
    public string Name => "Roads";

    public bool IsGenerating { get; private set; }

    public event Action OnGenerationComplete;

    [Header("Pathfinding")]
    public RoadSettings roadSettings = RoadSettings.Default;

    [Header("Agents")]
    public RoadAgentConfig[] agentConfigs;

    [Header("Urbanity")]
    public UrbanityConfig urbanityConfig;

    public IEnumerator Generate(WorldGrid _grid)
    {
        IsGenerating = true;

        var cityCenter   = CityGenerator.Instance.CityCenter;
        var poiPositions = CityGenerator.Instance.PlacedPOIPositions;
        var nearCities   = WorldGrid.Instance.NearCities;

        yield return StartCoroutine(ComputeUrbanity(_grid, cityCenter));

        var graph    = RoadGraph.Build(_grid, cityCenter, poiPositions, nearCities);
        var requests = new List<PathRequest>();
        var edgeMeta = new List<(RoadGraph.Edge edge, RoadSettings settings)>();

        foreach (var edge in graph.Edges)
        {
            var fromNode = graph.Nodes[edge.FromIndex];
            var toNode   = graph.Nodes[edge.ToIndex];

            var request = new PathRequest
            {
                Start        = new int2(fromNode.Position.x, fromNode.Position.y),
                End          = new int2(toNode.Position.x,   toNode.Position.y),
                NoiseOffsetX = Random.Range(0f, 1000f),
                NoiseOffsetY = Random.Range(0f, 1000f)
            };

            var edgeSettings = roadSettings;
            if (edge.Type == RoadGraph.EdgeType.SECONDARY)
                edgeSettings.roadWidth = Mathf.Max(1, edgeSettings.roadWidth - 1);

            requests.Add(request);
            edgeMeta.Add((edge, edgeSettings));
        }

        List<List<Vector2Int>> foundPaths = null;

        yield return StartCoroutine(
            CityGenerationJobRunner.ComputePaths(_grid, requests, roadSettings,
                                                 _results => foundPaths = _results));

        var spawnCells = new List<Vector2Int>();

        if (foundPaths != null && foundPaths.Count == requests.Count)
        {
            for (var i = 0; i < foundPaths.Count; i++)
            {
                var path = foundPaths[i];
                if (path == null || path.Count == 0) continue;

                var smoothed = MathHelper.SmoothPath(path);
                var meta     = edgeMeta[i];
                RoadBuilder.StampRoad(smoothed, meta.settings.roadWidth, _grid,
                    meta.settings.maxBridgeLength, meta.edge.Type);

                spawnCells.AddRange(smoothed);

                if (i % 5 == 0) yield return null;
            }
        }

        if (agentConfigs != null && spawnCells.Count > 0)
        {
            foreach (var config in agentConfigs)
            {
                if (!config) continue;

                var agentSeed = Random.Range(int.MinValue, int.MaxValue);
                yield return StartCoroutine(
                    RoadAgentRunner.Run(_grid, config, spawnCells, agentSeed));
            }
        }

        IsGenerating = false;
        OnGenerationComplete?.Invoke();
    }

    private IEnumerator ComputeUrbanity(WorldGrid _grid, Vector2Int _cityCenter)
    {
        if (!urbanityConfig)
        {
            SetDefaultUrbanity(_grid);
            yield break;
        }

        var totalCells = _grid.size * _grid.size;
        var results    = new NativeArray<float>(totalCells, Allocator.Persistent);
        var noiseSeed  = Random.Range(0f, 1000f);

        var job = new UrbanityJob
        {
            GridSize       = _grid.size,
            CitySettlePos  = new int2(_cityCenter.x, _cityCenter.y),
            MaxRadius      = urbanityConfig.maxRadius,
            NoiseScale     = urbanityConfig.noiseScale,
            NoiseAmplitude = urbanityConfig.noiseAmplitude,
            Seed           = noiseSeed,
            Results        = results
        };

        yield return GenerationJobManager.Instance.StartCoroutine(
            GenerationJobManager.DispatchJob(job, totalCells, 64,
                _completed => ApplyUrbanityResults(_completed, _grid, totalCells),
                results));
    }

    private static void ApplyUrbanityResults(UrbanityJob _job, WorldGrid _grid, int _totalCells)
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