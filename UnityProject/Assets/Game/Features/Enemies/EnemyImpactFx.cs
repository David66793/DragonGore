using UnityEngine;

namespace Changban
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyImpactFx : MonoBehaviour
    {
        private SpriteRenderer graphic;
        private Color initialColor;
        private Vector3 initialScale;
        private float age;

        private void Awake()
        {
            graphic = GetComponent<SpriteRenderer>();
            initialColor = graphic.color;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float progress = Mathf.Clamp01(age / 0.23f);
            transform.localScale = initialScale * (1f + progress * 0.40f);
            graphic.color = new Color(initialColor.r, initialColor.g, initialColor.b, initialColor.a * (1f - progress));
            if (progress >= 1f) Destroy(gameObject);
        }
    }
}
