using System;

namespace Core
{
    public class Singleton<T> where T : class, new()
    {
        private static readonly Lazy<T> _INSTANCE = new(() => new T());
        
        public static T Instance => _INSTANCE.Value;
    }
}