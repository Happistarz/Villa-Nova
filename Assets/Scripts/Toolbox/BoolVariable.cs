using System;
using UnityEngine;

namespace Core.Variables
{
    /// <summary>
    /// ScriptableObject wrapper for a bool value with change notification.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoolVariable", menuName = "Toolbox/Variables/Bool", order = 2)]
    public class BoolVariable : ScriptableObject
    {
        [SerializeField] private bool value;
        [SerializeField] private bool defaultValue;

        public event Action<bool> OnChanged;

        public bool Value
        {
            get => value;
            set
            {
                if (this.value == value) return;
                this.value = value;
                OnChanged?.Invoke(this.value);
            }
        }

        public void SetValue(bool _newValue) => Value = _newValue;
        public void SetValue(BoolVariable _variable) => Value = _variable.Value;

        public void Toggle() => Value = !Value;

        public void ResetToDefault() => Value = defaultValue;
    }
}
