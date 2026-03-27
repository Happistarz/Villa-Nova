using System;

namespace Core
{
    /// <summary>
    /// Thread-safe lazy singleton for plain C# classes.
    /// </summary>
    public class Singleton<T> where T : class, new()
    {
        private static readonly Lazy<T> _INSTANCE = new(() => new T());
        
        public static T Instance => _INSTANCE.Value;
    }
}