using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PrefabPool
{
    private readonly List<GameObject> _availableObjects;
    private readonly List<GameObject> _usedObjects;

    public GameObject Prefab { get; }

    public PrefabPool(GameObject _prefab, int _min = 0, int _max = 50)
    {
        _availableObjects = new List<GameObject>(_min);
        _usedObjects      = new List<GameObject>(_max);
        Prefab            = _prefab;
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

    public void Release(GameObject _gameObject)
    {
        _availableObjects.Add(_gameObject);
        _usedObjects.Remove(_gameObject);

        _gameObject.SetActive(false);
    }
}

public class PrefabPool<T> where T : MonoBehaviour
{
    private readonly List<T> _availableObjects;
    private readonly List<T> _usedObjects;

    public T Prefab { get; }
    private readonly GameObject _instanceRoot;

    public PrefabPool(GameObject _instanceRoot, T _prefab, int _minInstanceCount = 0)
    {
        _availableObjects = new List<T>();
        _usedObjects      = new List<T>();

        Prefab             = _prefab;
        this._instanceRoot = _instanceRoot;

        for (var i = 0; i < _minInstanceCount; i++)
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

    public void Release(T _instance)
    {
        _availableObjects.Add(_instance);
        _usedObjects.Remove(_instance);

        _instance.gameObject.SetActive(false);
    }
}