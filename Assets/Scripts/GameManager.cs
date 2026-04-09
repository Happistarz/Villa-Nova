using System;
using Core.Patterns;
using UnityEngine;

public sealed class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private GameConfig config;

    public GameConfig Config => config;

    public GameConfig.BiomeColorConfig ActiveColorConfig { get; set; } = GameConfig.BiomeColorConfig.Default;

    public event Action NewGenerationStarted;

    [Header("Seed")]
    [SerializeField] private int seed = -1;

    public int CurrentSeed { get; private set; }

    public System.Random RandomEngine { get; private set; }

    public void InitSeed()
    {
        CurrentSeed = seed == -1
            ? Environment.TickCount
            : seed;

        UnityEngine.Random.InitState(CurrentSeed);
        RandomEngine = new System.Random(CurrentSeed);
    }

    /// <summary>Sets a specific seed for the next generation.</summary>
    public void SetSeed(int _seed)
    {
        seed = _seed;
    }

    public void OnNewGenerationStarted()
    {
        NewGenerationStarted?.Invoke();
    }
}