using UnityEngine;
using UnityEngine.Events;

namespace Core.Events
{
    /// <summary>
    /// Subscribes to an EventData channel and invokes a UnityEvent response.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        public EventData @event;
        
        public UnityEvent response;
        
        private void OnEnable()
        {
            if (@event)
            {
                @event.RegisterListener(this);
            }
        }
        
        private void OnDisable()
        {
            if (@event)
            {
                @event.UnregisterListener(this);
            }
        }
        
        public void OnEventRaised()
        {
            response.Invoke();
        }
    }
}