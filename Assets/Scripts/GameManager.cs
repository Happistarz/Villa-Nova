using System;
using Core.Patterns;
using UnityEngine;

public sealed class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private GameConfig config;

    public GameConfig Config => config;

    public GameConfig.BiomeColorConfig ActiveColorConfig { get; set; } = GameConfig.BiomeColorConfig.Default;

    public event Action NewGenerationStarted;

    public void OnNewGenerationStarted()
    {
        NewGenerationStarted?.Invoke();
    }
}