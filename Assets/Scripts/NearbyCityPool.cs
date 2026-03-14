using System.Collections.Generic;
using UnityEngine;

public class NearbyCityPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public NearbyCityDisplay prefab;

    public int preloadCount = 4;

    private          PrefabPool<NearbyCityDisplay> _pool;
    private readonly List<NearbyCityDisplay>       _active = new();

    private void Awake()
    {
        _pool = new PrefabPool<NearbyCityDisplay>(gameObject, prefab, preloadCount);
    }

    public NearbyCityDisplay Get()
    {
        var instance = _pool.Get();
        _active.Add(instance);
        return instance;
    }

    public void Release(NearbyCityDisplay _instance)
    {
        _active.Remove(_instance);
        _pool.Release(_instance);
    }

    public void ReleaseAll()
    {
        for (var i = _active.Count - 1; i >= 0; i--)
            _active[i].Hide(_pool);

        _active.Clear();
    }
}