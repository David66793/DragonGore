using System;
using UnityEngine;

namespace Changban
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int maximum = 4;
        public int Current { get; private set; }
        public int Maximum => maximum;
        public bool IsDead => Current <= 0;
        public event Action<Health> Died;
        public event Action<Health> Changed;

        private void Awake() { Current = maximum; }

        public void SetMaximum(int value)
        {
            maximum = Mathf.Max(1, value);
            Current = maximum;
            Changed?.Invoke(this);
        }

        public bool TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return false;
            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(this);
            if (IsDead) Died?.Invoke(this);
            return true;
        }
    }
}
