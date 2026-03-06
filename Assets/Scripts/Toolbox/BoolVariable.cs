using UnityEngine;

namespace Core.Variables
{
    [CreateAssetMenu(fileName = "NewBoolVariable", menuName = "Toolbox/Variables/Bool", order = 2)]
    public class BoolVariable : ScriptableObject
    {
        [SerializeField] private bool value;
        [SerializeField] private bool defaultValue;

        public bool Value
        {
            get => value;
            set => this.value = value;
        }

        public void SetValue(bool newValue) => Value = newValue;
        public void SetValue(BoolVariable variable) => Value = variable.Value;
        
        public void Toggle() => Value = !Value;
        
        public void ResetToDefault() => Value = defaultValue;
    }
}

