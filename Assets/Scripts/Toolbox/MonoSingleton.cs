using UnityEngine;

namespace Core.Patterns
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T    _Instance;
        private static bool _IsQuitting;

        [Header("MonoSingleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        public static bool HasInstance => _Instance && !_IsQuitting;

        public static T Instance
        {
            get
            {
                if (_IsQuitting || _Instance) return _Instance;

                _Instance = FindFirstObjectByType<T>();

                if (_Instance) return _Instance;

                var obj = new GameObject(typeof(T).Name);
                _Instance = obj.AddComponent<T>();

                return _Instance;
            }
        }

        protected virtual void Awake()
        {
            if (!_Instance)
            {
                _Instance = (T)this;
                if (!transform.parent && dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);

                Initialize();
            }
            else if (_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _IsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_Instance == this)
                _Instance = null;
        }

        protected void Initialize()
        {
        }
    }
}