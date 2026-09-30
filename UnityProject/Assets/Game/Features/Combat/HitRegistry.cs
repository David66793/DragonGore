using System.Collections.Generic;

namespace Changban
{
    // One registry belongs to one strike. A new strike gets a new registry.
    public sealed class HitRegistry<T> where T : class
    {
        private readonly HashSet<T> targets = new HashSet<T>();
        public bool TryMark(T target) { return target != null && targets.Add(target); }
    }
}
