using System.Collections.Generic;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(fileName = "GameEvent", menuName = "Toolbox/EventData", order = 0)]
    public class EventData : ScriptableObject
    {
        private readonly List<GameEventListener> _eventListeners = new();
        
        public void Raise()
        {
            for (var i = _eventListeners.Count - 1; i >= 0; i--)
            {
                _eventListeners[i].OnEventRaised();
            }
        }
        
        public void RegisterListener(GameEventListener listener)
        {
            if (!_eventListeners.Contains(listener))
            {
                _eventListeners.Add(listener);
            }
        }
        
        public void UnregisterListener(GameEventListener listener)
        {
            if (_eventListeners.Contains(listener))
            {
                _eventListeners.Remove(listener);
            }
        }
    }
}