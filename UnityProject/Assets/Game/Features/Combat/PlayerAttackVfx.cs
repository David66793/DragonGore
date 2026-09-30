using System.Collections.Generic;
using UnityEngine;

namespace Changban
{
    // Short particles follow combat timing. Damage and hit detection stay in PlayerAttack.
    public sealed class PlayerAttackVfx : MonoBehaviour
    {
        private const string TextureRoot = "PlayerParticles/";
        private readonly List<Material> ownedMaterials = new List<Material>();
        private SpriteRenderer legacyTrail;
        private ParticleSystem sparks;
        private ParticleSystem dust;
        private ParticleSystem wind;
        private ParticleSystem skillGlints;
        private ParticleSystem shockRing;
        private ParticleSystem finisherWind;
        private ParticleSystem finisherSparks;
        private Health ownerHealth;
        private float nextTravelBurstAt;
        private float nextDodgeBurstAt;

        private void Awake()
        {
            // The old stretched SpriteRenderer looked like a light pillar.
            legacyTrail = transform.Find("Spear Trail")?.GetComponent<SpriteRenderer>();
            if (legacyTrail != null) legacyTrail.enabled = false;

            sparks = CreateSystem("Spear sparks", "star_01", 315, 0.10f,
                new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.2f)));
            dust = CreateSystem("Foot dust", "dirt_01", 292, 0.08f,
                new AnimationCurve(new Keyframe(0, 0.65f), new Keyframe(1, 1.45f)));
            wind = CreateSystem("Skill wind", "smoke_03", 305, 0,
                new AnimationCurve(new Keyframe(0, 0.7f), new Keyframe(1, 1.4f)));
            skillGlints = CreateSystem("Skill glints", "star_01", 318, 0,
                new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.12f)));
            shockRing = CreateSystem("Skill shock ring", "circle_02", 312, 0,
                new AnimationCurve(new Keyframe(0, 0.45f), new Keyframe(1, 2.15f)));
            finisherWind = CreateSystem("Combo Finisher wind", "smoke_03", 314, 0,
                new AnimationCurve(new Keyframe(0, 0.85f), new Keyframe(1, 1.2f)),
                wind.GetComponent<ParticleSystemRenderer>().sharedMaterial);
            var finisherMain = finisherWind.main;
            finisherMain.startSize3D = true;
            finisherSparks = CreateSystem("Combo Finisher sparks", "star_01", 319, 0,
                new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.12f)),
                sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial);
        }

        private void OnEnable()
        {
            ownerHealth = GetComponent<Health>();
            if (ownerHealth != null) ownerHealth.Died += OnOwnerDied;
        }

        private void OnDisable()
        {
            if (ownerHealth != null) ownerHealth.Died -= OnOwnerDied;
            CancelFinisherBurst();
        }

        private void OnOwnerDied(Health _) { CancelFinisherBurst(); }

        // Cancellation is separate from StopTrail: normal recovery keeps its short afterglow.
        public void CancelFinisherBurst()
        {
            finisherWind?.Clear();
            finisherSparks?.Clear();
        }

        private ParticleSystem CreateSystem(string objectName, string textureName, int sortingOrder,
            float gravity, AnimationCurve sizeCurve, Material sharedMaterial = null)
        {
            var holder = new GameObject(objectName);
            holder.transform.SetParent(transform, false);
            var particles = holder.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0;
            main.startLifetime = 0.25f;
            main.startSize = 0.1f;
            main.maxParticles = 128;
            main.gravityModifier = gravity;

            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            var colorOverLife = particles.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = new ParticleSystem.MinMaxGradient(fade);
            var sizeOverLife = particles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = holder.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            if (sharedMaterial != null)
            {
                renderer.sharedMaterial = sharedMaterial;
            }
            else
            {
                Texture2D texture = Resources.Load<Texture2D>(TextureRoot + textureName);
                if (texture == null)
                {
                    Debug.LogWarning("Missing player particle texture: " + TextureRoot + textureName, this);
                    texture = Texture2D.whiteTexture;
                }
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader != null)
                {
                    var material = new Material(shader) { name = "Player particle - " + textureName };
                    material.mainTexture = texture;
                    ownedMaterials.Add(material);
                    renderer.sharedMaterial = material;
                }
            }
            particles.Clear();
            particles.Play();
            return particles;
        }

        private void OnDestroy()
        {
            foreach (Material material in ownedMaterials)
            {
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }

        private static Vector2 SafeDirection(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        }

        private static void Emit(ParticleSystem system, Vector2 worldPosition, Vector2 velocity,
            float size, float lifetime, Color color)
        {
            if (system == null) return;
            var particle = new ParticleSystem.EmitParams
            {
                position = new Vector3(worldPosition.x, worldPosition.y, 0),
                velocity = new Vector3(velocity.x, velocity.y, 0),
                startSize = size,
                startLifetime = lifetime,
                startColor = color
            };
            system.Emit(particle, 1);
        }

        private static Vector2 Spread(Vector2 forward, float degrees)
        {
            Vector3 rotated = Quaternion.Euler(0, 0, Random.Range(-degrees, degrees)) * SafeDirection(forward);
            return new Vector2(rotated.x, rotated.y);
        }

        private static void EmitSparks(ParticleSystem system, Vector2 origin, Vector2 direction,
            int count, Color tint, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Spread(direction, 35f) * Random.Range(speed * 0.55f, speed);
                Emit(system, origin + Random.insideUnitCircle * 0.08f, velocity,
                    Random.Range(size * 0.55f, size), Random.Range(0.11f, 0.20f), tint);
            }
        }

        private void EmitGroundDust(Vector2 position, int count, float strength)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Random.insideUnitCircle * Random.Range(0.25f, strength);
                velocity.y = Mathf.Abs(velocity.y) * 0.4f;
                Emit(dust, position + Random.insideUnitCircle * 0.09f, velocity,
                    Random.Range(0.11f, 0.20f), Random.Range(0.19f, 0.31f),
                    new Color(0.51f, 0.44f, 0.32f, 0.55f));
            }
        }

        public void Windup(int step, Vector2 direction)
        {
            Vector2 feet = (Vector2)transform.position + Vector2.down * 0.07f;
            EmitGroundDust(feet, step == 3 ? 7 : step == 2 ? 5 : 3, step == 3 ? 1.15f : 0.7f);
            if (step == 2)
            {
                Vector2 shoulder = feet + Vector2.up * 0.66f;
                EmitSparks(sparks, shoulder, -SafeDirection(direction), 5,
                    new Color(0.94f, 0.81f, 0.57f, 0.65f), 0.7f, 0.13f);
            }
            else if (step == 3)
            {
                Emit(shockRing, feet + Vector2.up * 0.52f, Vector2.zero, 0.76f, 0.24f,
                    new Color(0.72f, 0.84f, 0.82f, 0.52f));
            }
        }

        public void Strike(Vector2 direction, float reach, float duration, int step)
        {
            if (legacyTrail != null) legacyTrail.enabled = false;
            Vector2 forward = SafeDirection(direction);
            Vector2 body = (Vector2)transform.position + Vector2.up * 0.57f;
            Vector2 spearTip = body + forward * (reach * 0.75f);
            if (step == 2)
            {
                // A short fan of separate flecks reads as an upward sweep, not a solid slash sprite.
                for (int i = 0; i < 15; i++)
                {
                    float arc = Mathf.Lerp(-60f, 65f, i / 14f);
                    Vector2 spread = Quaternion.Euler(0f, 0f, arc) * forward;
                    Vector2 point = body + spread * Random.Range(0.70f, reach * 0.95f);
                    Emit(sparks, point, spread * Random.Range(0.7f, 1.7f),
                        Random.Range(0.12f, 0.23f), Random.Range(0.11f, 0.19f),
                        new Color(0.97f, 0.84f, 0.62f, 0.78f));
                }
                EmitGroundDust(transform.position, 5, 1.1f);
            }
            else if (step == 3)
            {
                if (!isActiveAndEnabled || (ownerHealth != null && ownerHealth.IsDead)) return;
                EmitFinisherBurst(body, forward, reach);
                EmitGroundDust(transform.position, 9, 1.4f);
            }
            else
            {
                EmitSparks(sparks, spearTip, forward, 10,
                    new Color(0.95f, 0.90f, 0.76f, 0.94f), 2.3f, 0.20f);
            }
        }

        private void EmitFinisherBurst(Vector2 body, Vector2 forward, float reach)
        {
            // One burst at J's active frame, independent of whether the spear hits an enemy.
            // Separate thin smoke flecks give the tip a short gust without a solid light bar.
            Vector2 side = new Vector2(-forward.y, forward.x);
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            for (int i = 0; i < 7; i++)
            {
                Vector2 point = body + forward * Mathf.Lerp(reach * 0.35f, reach * 0.95f, i / 6f) +
                                side * Random.Range(-0.08f, 0.08f);
                Vector2 velocity = Spread(forward, 12f) * Random.Range(1.1f, 2.0f);
                finisherWind.Emit(new ParticleSystem.EmitParams
                {
                    position = new Vector3(point.x, point.y, 0),
                    velocity = new Vector3(velocity.x, velocity.y, 0),
                    startSize3D = new Vector3(Random.Range(0.36f, 0.54f), Random.Range(0.12f, 0.19f), 0.1f),
                    rotation = angle,
                    startLifetime = Random.Range(0.30f, 0.38f),
                    startColor = new Color(0.82f, 0.90f, 0.94f, 0.82f)
                }, 1);
            }
            for (int i = 0; i < 14; i++)
            {
                Vector2 point = body + forward * Random.Range(reach * 0.55f, reach * 1.03f) +
                                side * Random.Range(-0.13f, 0.13f);
                Emit(finisherSparks, point, Spread(forward, 18f) * Random.Range(1.4f, 2.6f),
                    Random.Range(0.16f, 0.28f), Random.Range(0.26f, 0.35f),
                    new Color(0.94f, 0.98f, 1f, 0.96f));
            }
        }

        public void StopTrail()
        {
            if (legacyTrail != null) legacyTrail.enabled = false;
        }

        public void Impact(Vector2 targetFeet, Vector2 direction, bool strong)
        {
            Vector2 body = targetFeet + Vector2.up * 0.58f;
            EmitSparks(sparks, body, SafeDirection(direction), strong ? 17 : 12,
                strong ? new Color(0.95f, 0.91f, 0.78f, 0.96f) : new Color(0.85f, 0.89f, 0.83f, 0.9f),
                strong ? 2.8f : 2.3f, strong ? 0.23f : 0.19f);
            EmitGroundDust(targetFeet, strong ? 3 : 2, 0.8f);
            if (strong)
                Emit(shockRing, body, Vector2.zero, 0.96f, 0.23f,
                    new Color(0.82f, 0.93f, 0.85f, 0.62f));
        }

        // L: Breakthrough thrust. A distinct cool wind trail follows the visible dash.
        public void SkillBegin(Vector2 origin, Vector2 direction)
        {
            Vector2 forward = SafeDirection(direction);
            Vector2 body = origin + Vector2.up * 0.51f;
            Emit(shockRing, body, Vector2.zero, 1.02f, 0.29f,
                new Color(0.48f, 0.84f, 0.96f, 0.73f));
            EmitSparks(skillGlints, body, -forward, 11,
                new Color(0.79f, 0.94f, 1f, 0.88f), 1.8f, 0.19f);
            EmitGroundDust(origin, 5, 1.1f);
            nextTravelBurstAt = 0;
        }

        public void SkillTravel(Vector2 position, Vector2 direction)
        {
            if (Time.time < nextTravelBurstAt) return;
            nextTravelBurstAt = Time.time + 0.035f;
            Vector2 backward = -SafeDirection(direction);
            Vector2 body = position + Vector2.up * 0.48f;
            for (int i = 0; i < 6; i++)
            {
                Emit(wind, body + Random.insideUnitCircle * 0.17f,
                    Spread(backward, 28f) * Random.Range(0.8f, 1.6f),
                    Random.Range(0.47f, 0.69f), Random.Range(0.26f, 0.38f),
                    new Color(0.62f, 0.87f, 0.95f, 0.78f));
            }
            EmitSparks(skillGlints, body, backward, 6,
                new Color(0.81f, 0.95f, 1f, 0.92f), 1.5f, 0.26f);
        }

        public void SkillImpact(Vector2 targetFeet, Vector2 direction)
        {
            Vector2 body = targetFeet + Vector2.up * 0.58f;
            Emit(shockRing, body, Vector2.zero, 1.48f, 0.33f,
                new Color(0.61f, 0.90f, 0.99f, 0.86f));
            Emit(shockRing, body, Vector2.zero, 0.93f, 0.27f,
                new Color(0.36f, 0.73f, 0.91f, 0.68f));
            EmitSparks(skillGlints, body, SafeDirection(direction), 22,
                new Color(0.85f, 0.97f, 1f, 0.98f), 3.4f, 0.24f);
            EmitGroundDust(targetFeet, 8, 1.4f);
        }

        public void DodgeBegin(Vector2 origin, Vector2 direction)
        {
            EmitGroundDust(origin, 8, 1.3f);
            Emit(wind, origin + Vector2.up * 0.45f, -SafeDirection(direction) * 0.35f,
                0.48f, 0.22f, new Color(0.76f, 0.77f, 0.73f, 0.48f));
            nextDodgeBurstAt = 0f;
        }

        public void DodgeTravel(Vector2 position, Vector2 direction)
        {
            if (Time.time < nextDodgeBurstAt) return;
            nextDodgeBurstAt = Time.time + 0.055f;
            EmitGroundDust(position, 3, 0.65f);
            Emit(wind, position + Vector2.up * 0.42f,
                -SafeDirection(direction) * Random.Range(0.4f, 0.9f),
                Random.Range(0.27f, 0.42f), 0.18f,
                new Color(0.70f, 0.72f, 0.69f, 0.38f));
        }

        public void HurtImpact(Vector2 feet, Vector2 fromEnemy)
        {
            Vector2 body = feet + Vector2.up * 0.56f;
            EmitSparks(sparks, body, SafeDirection(fromEnemy), 18,
                new Color(1f, 0.71f, 0.62f, 0.95f), 2.4f, 0.26f);
            Emit(shockRing, body, Vector2.zero, 0.86f, 0.25f,
                new Color(0.91f, 0.36f, 0.28f, 0.60f));
            EmitGroundDust(feet, 5, 0.9f);
        }
    }
}
