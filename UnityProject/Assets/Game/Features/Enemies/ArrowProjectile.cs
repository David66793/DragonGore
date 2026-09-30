using UnityEngine;

namespace Changban
{
    public sealed class ArrowProjectile : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private int damage;
        private PlayerMotor target;
        private float lifetime;
        private float nextTrailBurstAt;
        private SpriteRenderer graphic;
        private ParticleSystem trailParticles;

        private void Awake()
        {
            graphic = GetComponent<SpriteRenderer>();
            if (graphic != null)
            {
                // The starting prefab is a tiny rectangular placeholder. Replace it only at runtime.
                graphic.sprite = EnemyAttackVfx.ArrowSprite;
                graphic.sharedMaterial = EnemyAttackVfx.ArrowMaterial;
                graphic.color = Color.white;
                transform.localScale = Vector3.one;
                graphic.sortingOrder = 80;
            }
            trailParticles = EnemyAttackVfx.CreateArrowTrail(transform);
        }

        public void Initialize(Vector2 travel, float unitsPerSecond, int amount, PlayerMotor player)
        {
            direction = travel.normalized;
            speed = unitsPerSecond;
            damage = amount;
            target = player;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            nextTrailBurstAt = Time.time;
        }

        private void Update()
        {
            if (LevelDirector.Instance == null || !LevelDirector.Instance.IsPlaying) return;
            Vector2 previous = transform.position;
            Vector2 next = previous + direction * (speed * Time.deltaTime);
            transform.position = next;
            int sort = -Mathf.RoundToInt(next.y * 100f) + 30;
            if (graphic != null) graphic.sortingOrder = sort;
            if (trailParticles != null)
            {
                trailParticles.GetComponent<ParticleSystemRenderer>().sortingOrder = sort - 1;
                if (Time.time >= nextTrailBurstAt)
                {
                    nextTrailBurstAt = Time.time + 0.038f;
                    Vector2 back = -direction;
                    Vector2 side = new Vector2(-direction.y, direction.x);
                    for (int i = 0; i < 2; i++)
                    {
                        var particle = new ParticleSystem.EmitParams
                        {
                            position = next + back * Random.Range(0.19f, 0.38f) +
                                side * Random.Range(-0.055f, 0.055f),
                            velocity = back * Random.Range(0.15f, 0.42f),
                            startColor = new Color(0.80f, 0.66f, 0.46f, 0.53f),
                            startSize = Random.Range(0.09f, 0.15f),
                            startLifetime = Random.Range(0.10f, 0.17f)
                        };
                        trailParticles.Emit(particle, 1);
                    }
                }
            }
            lifetime += Time.deltaTime;
            if (target != null && !target.Health.IsDead)
            {
                Vector2 targetCenter = (Vector2)target.transform.position + Vector2.up * 0.52f;
                Vector2 segment = next - previous;
                float fraction = segment.sqrMagnitude > 0.0001f
                    ? Mathf.Clamp01(Vector2.Dot(targetCenter - previous, segment) / segment.sqrMagnitude) : 0f;
                Vector2 closest = previous + segment * fraction;
                if (Vector2.Distance(closest, targetCenter) < 0.33f)
                {
                    if (target.ReceiveHit(damage, closest)) EnemyAttackVfx.SpawnImpact(targetCenter, direction, true);
                    Destroy(gameObject);
                }
            }
            if (lifetime > 4f) Destroy(gameObject);
        }
    }
}
