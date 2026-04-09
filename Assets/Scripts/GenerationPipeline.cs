using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns;

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
        
        GameManager.Instance.SetSeed(-1);
        GameManager.Instance.InitSeed();

        StartCoroutine(RunPipeline());
    }

    /// <summary>
    /// Runs the generation pipeline sequentially, waiting for each generator to complete before starting the next
    /// </summary>
    private IEnumerator RunPipeline()
    {
        IsAnyGenerating           = true;
        MapGenerator.IsGenerating = true;

        GameManager.Instance.OnNewGenerationStarted();

        var grid = WorldGrid.Instance;

        yield return StartCoroutine(MapGenerator.Instance.Generate(grid));

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
            yield return StartCoroutine(generator.Generate(grid));
        }

        MapGenerator.IsGenerating = false;
        IsAnyGenerating           = false;

        OnPipelineComplete?.Invoke();
    }
}