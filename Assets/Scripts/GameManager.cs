using Core.Patterns;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private GameConfig config;

    public GameConfig Config => config;
}

