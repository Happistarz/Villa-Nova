using UnityEngine;

namespace Core.Patterns
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        [Header("MonoSingleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        public static bool HasInstance => _instance != null && !_isQuitting;

        public static T Instance
        {
            get
            {
                if (_isQuitting) return _instance;
                
                if (_instance) return _instance;

                _instance = FindFirstObjectByType<T>();

                if (_instance) return _instance;

                var obj = new GameObject(typeof(T).Name);
                _instance = obj.AddComponent<T>();

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (!_instance)
            {
                _instance = (T)this;
                if (!transform.parent || dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);

                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        protected virtual void Initialize()
        {
        }
    }
}