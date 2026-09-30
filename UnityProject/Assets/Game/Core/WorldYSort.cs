using UnityEngine;

namespace Changban
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WorldYSort : MonoBehaviour
    {
        public int sortingOffset;
        private SpriteRenderer graphic;
        private void Awake() { graphic = GetComponent<SpriteRenderer>(); }
        private void LateUpdate() { graphic.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100f) + sortingOffset; }
    }
}
