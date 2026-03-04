using UnityEngine;

namespace Core.Patterns
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        [Header("MonoSingleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        public static bool HasInstance => _instance || FindFirstObjectByType<T>();

        public static T Instance
        {
            get
            {
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

        protected virtual void Initialize()
        {
        }
    }
}