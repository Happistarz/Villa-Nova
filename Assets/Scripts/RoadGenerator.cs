using System;
using System.Collections;
using Core.Patterns;
using UnityEngine;

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

        var graph = RoadGraph.Build(_grid, cityCenter, poiPositions, nearCities);
        RoadBuilder.BuildFromGraph(_grid, graph, roadSettings);

        IsGenerating = false;
        OnGenerationComplete?.Invoke();
        yield break;
    }
}