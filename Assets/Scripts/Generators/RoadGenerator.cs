using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoadGenerator : MonoSingleton<RoadGenerator>, IGenerator
{
    public string Name => "Roads";

    public bool IsGenerating { get; private set; }

    public event Action OnGenerationComplete;

    [Header("Roads")]
    public RoadSettings roadSettings = RoadSettings.Default;

    public IEnumerator Generate(WorldGrid _grid)
    {
        IsGenerating = true;

        var cityCenter   = CityGenerator.Instance.CityCenter;
        var poiPositions = CityGenerator.Instance.PlacedPOIPositions;
        var nearCities   = WorldGrid.Instance.NearCities;

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

        if (foundPaths != null && foundPaths.Count == requests.Count)
        {
            for (var i = 0; i < foundPaths.Count; i++)
            {
                var path = foundPaths[i];
                if (path == null || path.Count == 0) continue;

                var smoothed = MathHelper.SmoothPath(path);
                var meta     = edgeMeta[i];
                RoadBuilder.StampRoad(smoothed, meta.settings.roadWidth, _grid, meta.settings.maxBridgeLength);

                if (i % 5 == 0) yield return null;
            }
        }

        IsGenerating = false;
        OnGenerationComplete?.Invoke();
    }
}