using UnityEngine;

namespace Changban
{
    // Small pooled-looking one-shot at the target body height; no sound or gameplay collision.
    public sealed class CombatHitSpark : MonoBehaviour
    {
        private SpriteRenderer graphic;
        private Color startColor;
        private Vector3 startScale;
        private float duration;
        private float startedAt;
        private bool bright;

        public static void Spawn(Vector2 position, Vector2 direction, Sprite sprite, Material material,
            Color color, float life, bool hit)
        {
            if (sprite == null) return;
            var obj = new GameObject(hit ? "Impact flash" : "Strike dust");
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            obj.transform.localScale = hit ? new Vector3(0.28f, 0.07f, 1) : new Vector3(0.52f, 0.18f, 1);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingOrder = 310;
            var effect = obj.AddComponent<CombatHitSpark>();
            effect.graphic = renderer;
            effect.startColor = color;
            effect.startScale = obj.transform.localScale;
            effect.duration = life;
            effect.startedAt = Time.time;
            effect.bright = hit;
        }

        private void Update()
        {
            float progress = (Time.time - startedAt) / duration;
            if (progress >= 1f) { Destroy(gameObject); return; }
            Color color = startColor;
            color.a *= 1f - progress;
            graphic.color = color;
            transform.localScale = startScale * (bright ? 1f + progress * 1.8f : 1f + progress * 0.45f);
        }
    }
}
