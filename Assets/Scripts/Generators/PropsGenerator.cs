using System;
using System.Collections;
using Core.Patterns;
using Unity.Collections;
using UnityEngine;

public class PropsGenerator : MonoSingleton<PropsGenerator>, IGenerator
{
    public string Name         => "Props";
    public bool   IsGenerating { get; private set; }

    public event Action OnGenerationComplete;

    [Header("Config")]
    public GameConfig gameConfig;

    [Header("Settings")]
    [Range(0f, 1f)] public float spawnThreshold = 0.50f;

    [Range(0f, 0.3f)] public float spawnFadeWidth = 0.15f;

    [Range(1, 10)] public int waterAttractionRadius = 4;

    [Range(0f, 0.25f)] public float waterAttractionBonus = 0.08f;

    [Header("Renderer")]
    public InstancedMeshRenderer propsRenderer;

    private void Start()
    {
        GameManager.Instance.NewGenerationStarted += OnNewGenerationStarted;
    }

    private void OnNewGenerationStarted()
    {
        if (propsRenderer) propsRenderer.Clear();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (GameManager.HasInstance)
            GameManager.Instance.NewGenerationStarted -= OnNewGenerationStarted;
    }

    public IEnumerator Generate(WorldGrid _grid)
    {
        IsGenerating = true;

        var trees = gameConfig ? gameConfig.GetTreesData() : null;

        if (trees == null || trees.Length == 0)
        {
            IsGenerating = false;
            OnGenerationComplete?.Invoke();
            yield break;
        }

        var cellData = GridJobUtilities.GetFlatGridData(_grid, Allocator.TempJob);
        var result   = new NativeArray<float>(_grid.size * _grid.size, Allocator.TempJob);

        var job = new PropsGenerationJob
        {
            GridSize              = _grid.size,
            WaterAttractionRadius = waterAttractionRadius,
            WaterAttractionBonus  = waterAttractionBonus,
            Cells                 = cellData,
            Result                = result
        };

        yield return StartCoroutine(GenerationJobManager.DispatchJob(job, _grid.size * _grid.size, 64,
                                                                     OnCompleted,
                                                                     cellData, result));

        IsGenerating = false;
        OnGenerationComplete?.Invoke();
    }

    private void OnCompleted(PropsGenerationJob _completed)
    {
        var rng     = GameManager.Instance.RandomEngine;
        var fadeMin = spawnThreshold - spawnFadeWidth;
        var fadeMax = spawnThreshold + spawnFadeWidth;

        for (var i = 0; i < _completed.Result.Length; i++)
        {
            var score = _completed.Result[i];
            if (score <= fadeMin) continue;

            var probability = Mathf.SmoothStep(fadeMin, fadeMax, score);
            if (rng.NextDouble() > probability) continue;

            var x   = i % WorldGrid.Instance.size;
            var y   = i / WorldGrid.Instance.size;
            var pos = WorldGrid.Instance.CellToWorld(new Vector2Int(x, y));

            var data     = gameConfig.GetTreesData()[rng.Next(0, gameConfig.GetTreesData().Length)];
            var rotation = rng.Next(0, 4);

            propsRenderer?.AddInstance(pos, rotation, data);
        }

        propsRenderer?.BakeBatches();
        propsRenderer?.SetColor(GameManager.Instance.ActiveColorConfig.treeColor);
    }
}