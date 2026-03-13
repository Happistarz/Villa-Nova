using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PrefabPool
{
    private readonly List<GameObject> _availableObjects;
    private readonly List<GameObject> _usedObjects;

    public GameObject Prefab { get; }

    public PrefabPool(GameObject prefab, int min = 0, int max = 50)
    {
        _availableObjects = new List<GameObject>(min);
        _usedObjects      = new List<GameObject>(max);
        Prefab            = prefab;
    }

    public GameObject Get()
    {
        if (_availableObjects.Count == 0)
        {
            var newInstance = Object.Instantiate(Prefab);
            _availableObjects.Add(newInstance);
        }

        var instance = _availableObjects.Last();
        _availableObjects.RemoveAt(_availableObjects.Count - 1);
        _usedObjects.Add(instance);

        instance.SetActive(true);
        return instance;
    }
    
    public T Get<T>() where T : MonoBehaviour
    {
        var gameObject = Get();
        return gameObject.GetComponent<T>();
    }

    public void Release(GameObject gameObject)
    {
        _availableObjects.Add(gameObject);
        _usedObjects.Remove(gameObject);

        gameObject.SetActive(false);
    }
}

public class PrefabPool<T> where T : MonoBehaviour
{
    private readonly List<T> _availableObjects;
    private readonly List<T> _usedObjects;

    public T Prefab { get; }
    private readonly GameObject _instanceRoot;

    public PrefabPool(GameObject instanceRoot, T prefab, int minInstanceCount = 0)
    {
        _availableObjects = new List<T>();
        _usedObjects      = new List<T>();

        Prefab       = prefab;
        _instanceRoot = instanceRoot;

        for (var i = 0; i < minInstanceCount; i++)
            CreateInstance();
    }

    public T Get()
    {
        if (_availableObjects.Count == 0)
            CreateInstance();

        var instance = _availableObjects.Last();
        _availableObjects.RemoveAt(_availableObjects.Count - 1);
        _usedObjects.Add(instance);

        instance.gameObject.SetActive(true);
        return instance;
    }

    private void CreateInstance()
    {
        var newInstance = Object.Instantiate(Prefab, _instanceRoot.transform);
        newInstance.gameObject.SetActive(false);
        _availableObjects.Add(newInstance);
    }

    public void Release(T instance)
    {
        _availableObjects.Add(instance);
        _usedObjects.Remove(instance);

        instance.gameObject.SetActive(false);
    }
}