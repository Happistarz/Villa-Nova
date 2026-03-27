using UnityEngine;

namespace Core.Variables
{
    /// <summary>
    /// ScriptableObject wrapper for a float value. Resets to default on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "FloatVar", menuName = "Toolbox/Variables/Float", order = 0)]
    public class FloatVariable : ScriptableObject
    {
        [TextArea] public string description;
        public float value;
        
        public float defaultValue;
        
        public void OnEnable()
        {
            value = defaultValue;
        }
    }
}