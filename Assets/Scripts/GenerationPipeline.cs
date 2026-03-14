using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns;
using UnityEngine;

public class GenerationPipeline : MonoSingleton<GenerationPipeline>
{
    public WorldRevealAnimator revealAnimator;

    public bool IsAnyGenerating { get; private set; }

    public event Action OnPipelineComplete;

    private readonly List<IGenerator> _generators = new();

    private void Start()
    {
        _generators.Add(MapGenerator.Instance);
        _generators.Add(CityGenerator.Instance);
        _generators.Add(RoadGenerator.Instance);
    }

    public void StartGeneration()
    {
        if (IsAnyGenerating) return;

        StartCoroutine(RunPipeline());
    }

    private IEnumerator RunPipeline()
    {
        IsAnyGenerating           = true;
        MapGenerator.IsGenerating = true;

        GameManager.Instance.OnNewGenerationStarted();

        var grid = WorldGrid.Instance;

        var mapGen = MapGenerator.Instance;
        Debug.Log($"[Pipeline] Starting: {mapGen.Name}");
        yield return StartCoroutine(mapGen.Generate(grid));
        Debug.Log($"[Pipeline] Completed: {mapGen.Name}");

        if (revealAnimator && revealAnimator.isActiveAndEnabled && revealAnimator.IsRevealing)
        {
            var revealDone = false;
            void OnReveal() => revealDone = true;
            revealAnimator.OnRevealComplete += OnReveal;

            while (!revealDone)
                yield return null;

            revealAnimator.OnRevealComplete -= OnReveal;
        }

        for (var i = 1; i < _generators.Count; i++)
        {
            var generator = _generators[i];
            Debug.Log($"[Pipeline] Starting: {generator.Name}");
            yield return StartCoroutine(generator.Generate(grid));
            Debug.Log($"[Pipeline] Completed: {generator.Name}");
        }

        MapGenerator.IsGenerating = false;
        IsAnyGenerating           = false;

        OnPipelineComplete?.Invoke();
    }
}