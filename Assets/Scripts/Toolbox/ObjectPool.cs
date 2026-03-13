using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int        initialSize = 10;

        private readonly Queue<GameObject> _pool = new();

        private void Awake()
        {
            for (var i = 0; i < initialSize; i++)
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
        
        private GameObject CreateInstance()
        {
            var obj = Instantiate(prefab);
            return obj;
        }

        public GameObject Get()
        {
            var obj = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            _pool.Enqueue(obj);
        }
    }
}