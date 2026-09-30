using UnityEngine;

namespace Changban
{
    // Enemy warning, strike and impact cues are short lived particles.
    public sealed class EnemyAttackVfx : MonoBehaviour
    {
        private static Sprite arrowSprite;
        private static Material arrowMaterial;
        private static Material starParticleMaterial;
        private static Material dirtParticleMaterial;
        private static Material ringParticleMaterial;

        private ParticleSystem warningParticles;
        private ParticleSystem strikeParticles;
        private ParticleSystem strikeDust;
        private EnemyBrain brain;
        private EnemyBrain.Role role;
        private Vector2 direction = Vector2.right;
        private float warningDuration;
        private bool contactEmitted;
        private bool warning;
        private bool dead;

        public Vector2 AimDirection => direction;
        public float WarningProgress => warning && brain != null ? brain.StateProgress : 0f;
        public float StrikeProgress => IsStriking ? brain.StateProgress : 1f;
        public bool IsStriking => !dead && brain != null && brain.CurrentState == EnemyBrain.State.Attack;

        private void Awake()
        {
            brain = GetComponent<EnemyBrain>();
            warningParticles = CreateParticleChild("Enemy Warning Particles", "circle_02", 12);
            // The warning remains readable until the AI clears it at Attack entry.
            // Strike and impact particles keep their own short fade-out.
            var warningFade = warningParticles.colorOverLifetime;
            warningFade.enabled = false;
            strikeParticles = CreateParticleChild("Enemy Strike Particles", "star_01", 24);
            strikeDust = CreateParticleChild("Enemy Strike Dust", "dirt_01", 23);
        }

        public void BeginWarning(EnemyBrain.Role attackRole, Vector2 aim, float duration)
        {
            if (dead) return;
            role = attackRole;
            SetAim(aim);
            warning = true;
            warningDuration = Mathf.Max(0.01f, duration);
            if (warningParticles != null) warningParticles.Clear();
            EmitWarningBurst();
        }

        public void SetAim(Vector2 aim)
        {
            if (aim.sqrMagnitude > 0.0001f) direction = aim.normalized;
        }

        public void BeginAttack(EnemyBrain.Role attackRole, Vector2 aim)
        {
            if (dead) return;
            role = attackRole;
            SetAim(aim);
            warning = false;
            if (warningParticles != null) warningParticles.Clear();
            contactEmitted = false;
        }

        public void EmitContact()
        {
            if (dead || contactEmitted || !IsStriking) return;
            contactEmitted = true;
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.61f +
                direction * (role == EnemyBrain.Role.Archer ? 0.42f : 0.62f);
            if (role == EnemyBrain.Role.Archer)
            {
                // Release feedback belongs at the bow. The bright star burst is
                // reserved for an arrow whose damage was actually accepted.
                EmitBurst(strikeDust, origin, -direction, 4,
                    new Color(0.60f, 0.54f, 0.43f, 0.42f), 0.16f);
                return;
            }
            Color spark = role == EnemyBrain.Role.Elite
                ? new Color(1f, 0.68f, 0.43f, 0.95f)
                : new Color(1f, 0.86f, 0.63f, 0.91f);
            EmitBurst(strikeParticles, origin, direction, role == EnemyBrain.Role.Elite ? 28 : 22, spark,
                role == EnemyBrain.Role.Elite ? 0.39f : 0.32f);
            EmitBurst(strikeDust, transform.position, -direction, 7,
                new Color(0.60f, 0.49f, 0.35f, 0.68f), 0.32f);
        }

        public void BeginRecovery()
        {
            warning = false;
            if (warningParticles != null) warningParticles.Clear();
        }

        public void Die()
        {
            dead = true;
            warning = false;
            if (warningParticles != null) warningParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (strikeParticles != null) strikeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (strikeDust != null) strikeDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (dead) return;
            int sort = -Mathf.RoundToInt(transform.position.y * 100f) + 24;
            if (warningParticles != null) warningParticles.GetComponent<ParticleSystemRenderer>().sortingOrder = sort - 12;
            if (strikeParticles != null) strikeParticles.GetComponent<ParticleSystemRenderer>().sortingOrder = sort;
            if (strikeDust != null) strikeDust.GetComponent<ParticleSystemRenderer>().sortingOrder = sort - 1;
        }

        private void EmitWarningBurst()
        {
            if (warningParticles == null) return;
            float reach = role == EnemyBrain.Role.Archer ? 4.15f : role == EnemyBrain.Role.Elite ? 1.60f : 1.28f;
            Vector2 weapon = (Vector2)transform.position + Vector2.up * 0.57f + direction * 0.22f;
            Vector2 target = (Vector2)transform.position + direction * reach + Vector2.up * 0.07f;
            Color tint = role == EnemyBrain.Role.Elite
                ? new Color(0.98f, 0.46f, 0.28f, 0.76f)
                : role == EnemyBrain.Role.Archer
                    ? new Color(1f, 0.76f, 0.45f, 0.72f)
                    : new Color(0.98f, 0.60f, 0.36f, 0.74f);
            // Emit one sustained cue for this windup. Repeated short-lived bursts
            // made one telegraph look like several separate attack decisions.
            for (int i = 0; i < 4; i++)
            {
                var particle = new ParticleSystem.EmitParams
                {
                    position = weapon + Random.insideUnitCircle * 0.18f,
                    velocity = Vector3.zero,
                    startColor = tint,
                    startSize = Random.Range(0.16f, 0.27f),
                    startLifetime = warningDuration + 0.06f
                };
                warningParticles.Emit(particle, 1);
            }
            for (int i = 0; i < 3; i++)
            {
                var marker = new ParticleSystem.EmitParams
                {
                    position = target + Random.insideUnitCircle * 0.15f,
                    velocity = Vector3.zero,
                    startColor = tint,
                    startSize = Random.Range(0.20f, 0.33f),
                    startLifetime = warningDuration + 0.06f
                };
                warningParticles.Emit(marker, 1);
            }
            var targetRing = new ParticleSystem.EmitParams
            {
                position = target,
                velocity = Vector3.zero,
                startColor = new Color(tint.r, tint.g, tint.b, 0.56f),
                startSize = 0.52f,
                startLifetime = warningDuration + 0.06f
            };
            warningParticles.Emit(targetRing, 1);
        }

        public static void SpawnImpact(Vector2 point, Vector2 travel, bool ranged)
        {
            var obj = new GameObject(ranged ? "Arrow Impact Particles" : "Weapon Impact Particles");
            obj.transform.position = point;
            ParticleSystem particles = ConfigureParticles(obj, "star_01",
                -Mathf.RoundToInt(point.y * 100f) + 45);
            EmitBurst(particles, point, travel, ranged ? 14 : 18,
                ranged ? new Color(1f, 0.83f, 0.55f, 0.95f) : new Color(1f, 0.91f, 0.73f, 0.95f),
                ranged ? 0.25f : 0.30f);
            Destroy(obj, 0.55f);
        }

        private ParticleSystem CreateParticleChild(string name, string textureName, int sortOffset)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            return ConfigureParticles(child, textureName,
                -Mathf.RoundToInt(transform.position.y * 100f) + sortOffset);
        }

        public static ParticleSystem CreateArrowTrail(Transform arrow)
        {
            var child = new GameObject("Arrow Particles");
            child.transform.SetParent(arrow, false);
            return ConfigureParticles(child, "dirt_01", 79);
        }

        private static ParticleSystem ConfigureParticles(GameObject holder, string textureName, int sortingOrder)
        {
            var system = holder.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = 0.20f;
            main.startSize = 0.15f;
            main.maxParticles = 96;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            var colorOverLife = system.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = new ParticleSystem.MinMaxGradient(fade);
            var renderer = holder.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = textureName == "dirt_01" ? DirtParticleMaterial :
                textureName == "circle_02" ? RingParticleMaterial : StarParticleMaterial;
            renderer.sortingOrder = sortingOrder;
            system.Play();
            return system;
        }

        private static void EmitBurst(ParticleSystem system, Vector2 point, Vector2 heading,
            int count, Color color, float size)
        {
            if (system == null) return;
            Vector2 forward = heading.sqrMagnitude > 0.001f ? heading.normalized : Vector2.right;
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Quaternion.Euler(0, 0, Random.Range(-55f, 55f)) * forward;
                var particle = new ParticleSystem.EmitParams
                {
                    position = point + Random.insideUnitCircle * 0.10f,
                    velocity = velocity * Random.Range(1.0f, 2.6f),
                    startColor = color,
                    startSize = Random.Range(size * 0.65f, size),
                    startLifetime = Random.Range(0.12f, 0.25f)
                };
                system.Emit(particle, 1);
            }
        }

        private static Material StarParticleMaterial => starParticleMaterial != null ? starParticleMaterial :
            starParticleMaterial = MakeParticleMaterial(Resources.Load<Texture2D>("PlayerParticles/star_01"));
        private static Material DirtParticleMaterial => dirtParticleMaterial != null ? dirtParticleMaterial :
            dirtParticleMaterial = MakeParticleMaterial(Resources.Load<Texture2D>("PlayerParticles/dirt_01"));
        private static Material RingParticleMaterial => ringParticleMaterial != null ? ringParticleMaterial :
            ringParticleMaterial = MakeParticleMaterial(Resources.Load<Texture2D>("PlayerParticles/circle_02"));

        private static Material MakeParticleMaterial(Texture texture)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return null;
            var material = new Material(shader) { hideFlags = HideFlags.DontSave };
            material.mainTexture = texture != null ? texture : Texture2D.whiteTexture;
            return material;
        }

        public static Sprite ArrowSprite => arrowSprite != null ? arrowSprite : arrowSprite = MakeArrowSprite();
        public static Material ArrowMaterial => arrowMaterial != null ? arrowMaterial : arrowMaterial = MakeMaterial(ArrowSprite.texture);

        private static Material MakeMaterial(Texture texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var material = new Material(shader) { hideFlags = HideFlags.DontSave };
            material.mainTexture = texture;
            return material;
        }

        private static Sprite MakeArrowSprite()
        {
            const int width = 128;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "Enemy Arrow Runtime", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float mid = Mathf.Abs(y + 0.5f - height * 0.5f);
                bool shaft = x >= 10 && x < 102 && mid <= 1.75f;
                bool head = x >= 91 && mid <= (width - x) * 0.33f;
                bool feather = x < 24 && x >= 5 && mid <= 2f + (24 - x) * 0.28f;
                if (shaft || head || feather) pixels[y * width + x] = new Color32(245, 228, 184, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 256f);
        }
    }
}
