using UnityEngine;

namespace Changban
{
    // The Animator lives on the art child, so animation never moves the physics body.
    [RequireComponent(typeof(PlayerMotor), typeof(Health))]
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material attackMaterial;
        [SerializeField] private Material northAttackMaterial;
        [SerializeField] private Material southAttackMaterial;
        [SerializeField] private Material northWalkMaterial;
        [SerializeField] private Material southWalkMaterial;
        [SerializeField] private Material comboAttackMaterial;
        [SerializeField] private Material northComboMaterial;
        [SerializeField] private Material southComboMaterial;
        [SerializeField] private Material eastRunMaterial;
        [SerializeField] private Material readyMaterial;

        private PlayerMotor motor;
        private Health health;
        private Transform art;
        private Vector3 restScale;
        private Quaternion restRotation;
        private SpriteRenderer poseTrail;
        private Sprite lastSprite;
        private Material lastMaterial;
        private Color lastColor;
        private Color restColor = Color.white;
        private float hurtFlashUntil;
        private bool lastFlipX;
        private float trailRemaining;
        private string currentState;
        private float stateStartedAt;
        private float actionStartedAt;
        private float actionEndsAt;
        private float actionDuration;
        private int actionPriority;
        private Vector2 actionDirection = Vector2.right;
        private float walkWeight;
        private float scaleX = 1f;
        private float scaleY = 1f;
        private float lean;
        private bool deathPlayed;
        private float lastActivityAt;
        private const float PoseTrailDuration = 0.065f;
        public Animator Target => animator;
        public SpriteRenderer CharacterRenderer => characterRenderer;
        public Material IdleMaterial => idleMaterial;
        public Material AttackMaterial => attackMaterial;
        public Material NorthAttackMaterial => northAttackMaterial;
        public Material SouthAttackMaterial => southAttackMaterial;
        public Material NorthWalkMaterial => northWalkMaterial;
        public Material SouthWalkMaterial => southWalkMaterial;
        public Material ComboAttackMaterial => comboAttackMaterial;
        public Material NorthComboMaterial => northComboMaterial;
        public Material SouthComboMaterial => southComboMaterial;

        public void ConfigureLocomotion(Material run, Material ready)
        {
            eastRunMaterial = run;
            readyMaterial = ready;
        }

        public void Configure(Animator target, SpriteRenderer renderer, Material idle, Material attack,
            Material northAttack, Material southAttack, Material northWalk, Material southWalk,
            Material comboAttack, Material northCombo, Material southCombo)
        {
            animator = target;
            characterRenderer = renderer;
            idleMaterial = idle;
            attackMaterial = attack;
            northAttackMaterial = northAttack;
            southAttackMaterial = southAttack;
            northWalkMaterial = northWalk;
            southWalkMaterial = southWalk;
            comboAttackMaterial = comboAttack;
            northComboMaterial = northCombo;
            southComboMaterial = southCombo;
        }

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            health = GetComponent<Health>();
            lastActivityAt = Time.time;
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (characterRenderer == null && animator != null)
                characterRenderer = animator.GetComponent<SpriteRenderer>();
            if (animator != null)
            {
                art = animator.transform;
                restScale = art.localScale;
                restRotation = art.localRotation;
            }
            if (characterRenderer != null)
            {
                lastSprite = characterRenderer.sprite;
                lastMaterial = characterRenderer.sharedMaterial;
                lastColor = characterRenderer.color;
                restColor = characterRenderer.color;
                lastFlipX = characterRenderer.flipX;
            }
            health.Died += OnDeath;
        }

        private void OnDisable()
        {
            if (art != null)
            {
                art.localScale = restScale;
                art.localRotation = restRotation;
            }
            if (poseTrail != null) poseTrail.enabled = false;
            if (characterRenderer != null) characterRenderer.color = restColor;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDeath;
        }

        private void Update()
        {
            if (animator == null || health.IsDead) return;
            if (LevelDirector.Instance != null && LevelDirector.Instance.CurrentPhase == LevelDirector.Phase.Paused) return;
            if (characterRenderer != null)
            {
                // Lock the chosen strike direction. Vertical poses already show their facing.
                bool vertical = currentState != null &&
                    (currentState == "WalkNorth" || currentState == "WalkSouth" ||
                     currentState.EndsWith("North") || currentState.EndsWith("South"));
                Vector2 facing = IsDirectionalAction(currentState) && Time.time < actionEndsAt
                    ? actionDirection : motor.Facing;
                if (vertical) characterRenderer.flipX = false;
                else if (Mathf.Abs(facing.x) > 0.22f) characterRenderer.flipX = facing.x < 0f;
            }
            if (Time.time < actionEndsAt)
            {
                lastActivityAt = Time.time;
                return;
            }
            actionPriority = 0;
            bool moving = LevelDirector.Instance != null && LevelDirector.Instance.IsPlaying &&
                          motor.MoveInput.sqrMagnitude > 0.01f;
            string locomotion = "Idle";
            if (moving)
            {
                lastActivityAt = Time.time;
                locomotion = Mathf.Abs(motor.Facing.y) > Mathf.Abs(motor.Facing.x) * 1.10f
                    ? motor.Facing.y > 0f ? "WalkNorth" : "WalkSouth"
                    : "Walk";
            }
            else if (Time.time - lastActivityAt < (motor.balance != null ? motor.balance.playerIdleDelay : 0.9f))
            {
                locomotion = Mathf.Abs(motor.Facing.y) > Mathf.Abs(motor.Facing.x) * 1.10f
                    ? motor.Facing.y > 0f ? "ReadyNorth" : "ReadySouth"
                    : "Ready";
            }
            PlayState(locomotion);
        }

        private void LateUpdate()
        {
            if (art == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            UpdatePoseTrail(dt);

            float walkTarget = currentState != null && currentState.StartsWith("Walk") && !health.IsDead
                ? Mathf.Clamp01(motor.MoveInput.magnitude) : 0f;
            walkWeight = Mathf.MoveTowards(walkWeight, walkTarget, dt * 7f);
            float targetX = 1f;
            float targetY = 1f;
            float targetLean = 0f;
            if (currentState == "Idle" && !health.IsDead)
                targetY += 0.004f * Mathf.Sin(Time.time * 2.3f);
            if (walkWeight > 0f)
            {
                // The clip plants a foot at 0 and 0.24 seconds. The sprite pivots near the
                // boots, so this small stretch lifts the torso without moving the contact.
                float period = 0.48f;
                float gait = (Time.time - stateStartedAt) * (Mathf.PI * 2f / period);
                float rise = Mathf.Abs(Mathf.Sin(gait));
                // Leg motion comes from the frames. Keep the body grounded instead of rocking it.
                targetY += 0.004f * rise * walkWeight;
            }

            float progress = actionDuration > 0f
                ? Mathf.Clamp01((Time.time - actionStartedAt) / actionDuration) : 1f;
            float side = Mathf.Abs(actionDirection.x) > 0.30f ? Mathf.Sign(actionDirection.x) : 0f;
            switch (currentState)
            {
                case "Attack":
                case "AttackNorth":
                case "AttackSouth":
                case "Attack2":
                case "Attack2North":
                case "Attack2South":
                case "Attack3":
                case "Attack3North":
                case "Attack3South":
                {
                    // Pull the shoulders back during the windup, drive toward the target on
                    // the extend frame, then settle through recovery.
                    float thrust = AttackEnvelope(progress);
                    float strength = currentState.StartsWith("Attack3") ? 1.55f :
                        currentState.StartsWith("Attack2") ? 1.18f : 1f;
                    targetLean += thrust * 2.5f * side * strength;
                    targetY += thrust > 0f ? thrust * 0.027f * strength : thrust * 0.018f;
                    targetX += Mathf.Max(0f, thrust) * 0.014f * strength;
                    break;
                }
                case "Skill":
                case "SkillNorth":
                case "SkillSouth":
                {
                    float drive = SkillEnvelope(progress);
                    targetLean += drive * 4.2f * side;
                    targetY += drive > 0f ? drive * 0.055f : drive * 0.020f;
                    targetX += Mathf.Max(0f, drive) * 0.025f;
                    break;
                }
                case "Dodge":
                {
                    float crouch = Mathf.Sin(progress * Mathf.PI);
                    targetY -= crouch * 0.055f;
                    targetX += crouch * 0.038f;
                    targetLean += crouch * 2.1f * side;
                    break;
                }
                case "Hurt":
                {
                    float recoil = Mathf.Sin(progress * Mathf.PI);
                    targetY -= recoil * 0.035f;
                    targetLean -= recoil * 3f * side;
                    break;
                }
            }

            // The art child eases between poses. Collider, world sorting and hit tests stay
            // anchored to the PlayerMotor root.
            float blend = 1f - Mathf.Exp(-25f * dt);
            scaleX = Mathf.Lerp(scaleX, targetX, blend);
            scaleY = Mathf.Lerp(scaleY, targetY, blend);
            lean = Mathf.Lerp(lean, targetLean, blend);
            art.localScale = Vector3.Scale(restScale, new Vector3(scaleX, scaleY, 1f));
            art.localRotation = restRotation * Quaternion.Euler(0f, 0f, lean);
            if (characterRenderer != null)
            {
                float remaining = Mathf.Clamp01((hurtFlashUntil - Time.time) / 0.25f);
                float flash = remaining > 0f ? Mathf.Sin((1f - remaining) * Mathf.PI * 3f) * 0.16f : 0f;
                characterRenderer.color = Color.Lerp(restColor,
                    new Color(1f, 0.58f + flash, 0.55f + flash, restColor.a), remaining * 0.75f);
            }
        }

        public void PlayAttack(float duration) { PlayAttack(duration, motor != null ? motor.Facing : Vector2.right, 1); }
        public void PlayAttack(float duration, Vector2 direction) { PlayAttack(duration, direction, 1); }
        public void PlayAttack(float duration, Vector2 direction, int comboStep)
        {
            string prefix = comboStep == 3 ? "Attack3" : comboStep == 2 ? "Attack2" : "Attack";
            string state = Mathf.Abs(direction.y) > Mathf.Abs(direction.x) * 1.10f
                ? prefix + (direction.y > 0 ? "North" : "South")
                : prefix;
            PlayAction(state, duration, direction, 1);
        }
        public void PlayDodge(float duration)
        {
            PlayAction("Dodge", duration, motor != null ? motor.Facing : Vector2.right, 2);
        }
        public void PlaySkill(float duration)
        {
            Vector2 direction = motor != null ? motor.Facing : Vector2.right;
            string state = Mathf.Abs(direction.y) > Mathf.Abs(direction.x) * 1.10f
                ? direction.y > 0f ? "SkillNorth" : "SkillSouth"
                : "Skill";
            // Older generated controllers may not have the new vertical skill states until
            // the Editor setup runs. Their directional attack frames are a safe visual fallback.
            if (state != "Skill" && animator != null && !animator.HasState(0, Animator.StringToHash(state)))
                state = direction.y > 0f ? "AttackNorth" : "AttackSouth";
            PlayAction(state, duration, direction, 3);
        }
        public void PlayHurt()
        {
            hurtFlashUntil = Time.time + 0.25f;
            PlayAction("Hurt", 0.25f, motor != null ? motor.Facing : Vector2.right, 4);
        }

        private void PlayAction(string state, float duration, Vector2 direction, int priority)
        {
            if (animator == null || health.IsDead || deathPlayed) return;
            if (Time.time < actionEndsAt && priority < actionPriority) return;
            actionPriority = priority;
            lastActivityAt = Time.time;
            actionStartedAt = Time.time;
            actionDuration = Mathf.Max(0.01f, duration);
            actionEndsAt = actionStartedAt + actionDuration;
            if (direction.sqrMagnitude > 0.001f) actionDirection = direction.normalized;
            PlayState(state, true);
        }

        private void OnDeath(Health _)
        {
            if (deathPlayed) return;
            deathPlayed = true;
            actionPriority = 100;
            actionEndsAt = float.PositiveInfinity;
            walkWeight = 0f;
            if (poseTrail != null) poseTrail.enabled = false;
            PlayState("Death", true);
        }

        private void PlayState(string state, bool restart = false)
        {
            if (animator == null || (!restart && currentState == state)) return;
            currentState = state;
            stateStartedAt = Time.time;
            Material material = state == "Attack" || state == "Skill" ? attackMaterial :
                state == "AttackNorth" || state == "SkillNorth"
                    ? northAttackMaterial :
                state == "AttackSouth" || state == "SkillSouth"
                    ? southAttackMaterial :
                state == "Attack2North" || state == "Attack3North" ? northComboMaterial :
                state == "Attack2South" || state == "Attack3South" ? southComboMaterial :
                state == "WalkNorth" ? northWalkMaterial :
                state == "WalkSouth" ? southWalkMaterial :
                state == "Walk" ? eastRunMaterial :
                state == "ReadyNorth" ? northAttackMaterial :
                state == "ReadySouth" ? southAttackMaterial :
                state == "Ready" ? readyMaterial :
                state == "Attack2" || state == "Attack3" ? comboAttackMaterial : idleMaterial;
            if (characterRenderer != null && material != null && characterRenderer.sharedMaterial != material)
                characterRenderer.sharedMaterial = material;
            if (characterRenderer != null && (state == "WalkNorth" || state == "WalkSouth" ||
                state.EndsWith("North") || state.EndsWith("South")))
                characterRenderer.flipX = false;
            else if (characterRenderer != null)
            {
                Vector2 facing = IsDirectionalAction(state) ? actionDirection : motor.Facing;
                if (Mathf.Abs(facing.x) > 0.22f) characterRenderer.flipX = facing.x < 0f;
            }
            animator.Play(state, 0, 0f);
        }

        private static bool IsDirectionalAction(string state)
        {
            return state != null && (state.StartsWith("Attack") ||
                   state == "Skill" || state == "SkillNorth" || state == "SkillSouth" || state == "Dodge");
        }

        private void UpdatePoseTrail(float dt)
        {
            if (characterRenderer == null || health.IsDead) return;
            Sprite sprite = characterRenderer.sprite;
            bool running = currentState != null && currentState.StartsWith("Walk");
            if (running && poseTrail != null) poseTrail.enabled = false;
            if (!running && sprite != null && lastSprite != null &&
                (sprite != lastSprite || characterRenderer.flipX != lastFlipX))
            {
                EnsurePoseTrail();
                poseTrail.sprite = lastSprite;
                poseTrail.sharedMaterial = lastMaterial;
                poseTrail.flipX = lastFlipX;
                trailRemaining = PoseTrailDuration;
                poseTrail.enabled = true;
            }
            lastSprite = sprite;
            lastMaterial = characterRenderer.sharedMaterial;
            lastColor = characterRenderer.color;
            lastFlipX = characterRenderer.flipX;
            if (poseTrail == null || !poseTrail.enabled) return;
            poseTrail.sortingLayerID = characterRenderer.sortingLayerID;
            poseTrail.sortingOrder = characterRenderer.sortingOrder - 1;
            trailRemaining = Mathf.Max(0f, trailRemaining - dt);
            Color tint = lastColor;
            tint.a *= 0.18f * (trailRemaining / PoseTrailDuration);
            poseTrail.color = tint;
            if (trailRemaining <= 0f) poseTrail.enabled = false;
        }

        private void EnsurePoseTrail()
        {
            if (poseTrail != null) return;
            var holder = new GameObject("Pose Transition");
            holder.transform.SetParent(art, false);
            poseTrail = holder.AddComponent<SpriteRenderer>();
            poseTrail.enabled = false;
        }

        private static float AttackEnvelope(float t)
        {
            if (t < 0.24f) return Ease(0f, -0.65f, t / 0.24f);
            if (t < 0.37f) return Ease(-0.65f, 1f, (t - 0.24f) / 0.13f);
            if (t < 0.58f) return Ease(1f, 0.50f, (t - 0.37f) / 0.21f);
            return Ease(0.50f, 0f, (t - 0.58f) / 0.42f);
        }

        private static float SkillEnvelope(float t)
        {
            if (t < 0.18f) return Ease(0f, -0.45f, t / 0.18f);
            if (t < 0.34f) return Ease(-0.45f, 1f, (t - 0.18f) / 0.16f);
            if (t < 0.64f) return Ease(1f, 0.50f, (t - 0.34f) / 0.30f);
            return Ease(0.50f, 0f, (t - 0.64f) / 0.36f);
        }

        private static float Ease(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            t = t * t * (3f - 2f * t);
            return Mathf.LerpUnclamped(from, to, t);
        }
    }
}
