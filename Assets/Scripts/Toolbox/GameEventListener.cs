using UnityEngine;
using UnityEngine.Events;

namespace Core.Events
{
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