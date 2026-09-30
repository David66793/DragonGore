using UnityEngine;

namespace Changban
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public GameBalance balance;
        public Vector2 Facing { get; private set; } = Vector2.right;
        public Vector2 MoveInput => movement;
        public Health Health { get; private set; }
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private bool contactGhost;
        private readonly System.Collections.Generic.List<Collider2D> ignoredEnemies =
            new System.Collections.Generic.List<Collider2D>();
        private Vector2 movement;
        private float dodgeRemaining;
        private Vector2 dodgeDirection = Vector2.right;
        private float nextDodge;
        private float invulnerableUntil;
        private float nextSkill;
        private const float SkillDuration = 0.27f;
        private float skillElapsed;
        private bool skillActive;
        private Vector2 skillOrigin;
        private Vector2 skillDestination;
        private Vector2 skillDirection;
        private PlayerAttack skillAttack;
        private PlayerAttackVfx skillEffects;
        private SpriteRenderer directionIndicator;
        private PlayerVisualAnimator visual;
        private Vector2 pendingAttackStep;
        private Camera viewCamera;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            Health = GetComponent<Health>();
            visual = GetComponent<PlayerVisualAnimator>();
            skillAttack = GetComponent<PlayerAttack>();
            skillEffects = GetComponent<PlayerAttackVfx>();
            viewCamera = Camera.main;
            directionIndicator = transform.Find("Direction Indicator")?.GetComponent<SpriteRenderer>();
        }

        private void Start() { Health.SetMaximum(balance.playerHealth); }

        private void Update()
        {
            if (LevelDirector.Instance == null || !LevelDirector.Instance.IsPlaying) { movement = Vector2.zero; return; }
            movement = GameInput.Move;
            if (movement.sqrMagnitude > 0.01f) Facing = movement.normalized;
            if (GameInput.Dodge) TryDodge();
            if (GameInput.Skill) TryUseSkill();
            if (directionIndicator != null)
            {
                directionIndicator.transform.localPosition = (Vector3)(Facing * 0.65f);
                directionIndicator.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg - 90f);
            }
        }

        private void FixedUpdate()
        {
            if (LevelDirector.Instance == null || !LevelDirector.Instance.IsPlaying) return;
            if (contactGhost) RefreshEnemyCollisionIgnores();
            if (skillActive)
            {
                float previous = Mathf.Clamp01(skillElapsed / SkillDuration);
                skillElapsed = Mathf.Min(SkillDuration, skillElapsed + Time.fixedDeltaTime);
                float current = Mathf.Clamp01(skillElapsed / SkillDuration);
                float eased = current * current * (3f - 2f * current);
                Vector2 from = Vector2.Lerp(skillOrigin, skillDestination,
                    previous * previous * (3f - 2f * previous));
                Vector2 skillStepDestination = ClampToPlayArea(Vector2.Lerp(skillOrigin, skillDestination, eased));
                body.MovePosition(skillStepDestination);
                skillAttack?.SkillStrikeSegment(from, skillStepDestination, skillDirection);
                skillEffects?.SkillTravel(skillStepDestination, skillDirection);
                if (skillElapsed >= SkillDuration)
                {
                    skillActive = false;
                    skillAttack?.EndSkillStrike();
                }
                return;
            }
            Vector2 direction = dodgeRemaining > 0 ? dodgeDirection : movement;
            float speed = dodgeRemaining > 0 ? balance.dodgeSpeed : balance.moveSpeed;
            Vector2 destination = body.position + direction * (speed * Time.fixedDeltaTime) + pendingAttackStep;
            pendingAttackStep = Vector2.zero;
            Vector2 clampedDestination = ClampToPlayArea(destination);
            body.MovePosition(clampedDestination);
            if (dodgeRemaining > 0f) skillEffects?.DodgeTravel(clampedDestination, dodgeDirection);
            dodgeRemaining = Mathf.Max(0, dodgeRemaining - Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (contactGhost && dodgeRemaining <= 0f && !skillActive && !OverlapsIgnoredEnemy())
                SetContactGhost(false);
        }

        private void OnDisable() { SetContactGhost(false); }

        private void SetContactGhost(bool active)
        {
            contactGhost = active;
            if (active) { RefreshEnemyCollisionIgnores(); return; }
            if (bodyCollider != null)
                foreach (Collider2D other in ignoredEnemies)
                    if (other != null) Physics2D.IgnoreCollision(bodyCollider, other, false);
            ignoredEnemies.Clear();
        }

        private void RefreshEnemyCollisionIgnores()
        {
            if (bodyCollider == null) return;
            foreach (EnemyBrain enemy in FindObjectsByType<EnemyBrain>())
            {
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead) continue;
                foreach (Collider2D other in enemy.GetComponentsInChildren<Collider2D>())
                {
                    if (other == null || !other.enabled || other == bodyCollider) continue;
                    if (!ignoredEnemies.Contains(other)) ignoredEnemies.Add(other);
                    Physics2D.IgnoreCollision(bodyCollider, other, true);
                }
            }
        }

        private bool OverlapsIgnoredEnemy()
        {
            if (bodyCollider == null) return false;
            foreach (Collider2D other in ignoredEnemies)
            {
                if (other != null && other.enabled && bodyCollider.Distance(other).isOverlapped) return true;
            }
            return false;
        }

        public bool ReceiveHit(int damage, Vector2 from)
        {
            if (Time.time < invulnerableUntil || Health.IsDead) return false;
            bool accepted = Health.TakeDamage(damage);
            if (accepted)
                skillEffects?.HurtImpact(body.position, ((Vector2)transform.position - from).normalized);
            if (accepted && !Health.IsDead)
            {
                Vector2 push = ((Vector2)transform.position - from).normalized * 0.35f;
                body.MovePosition(ClampToPlayArea(body.position + push));
                invulnerableUntil = Time.time + 0.45f;
                visual?.PlayHurt();
            }
            return accepted;
        }

        public float DodgeReadyIn => Mathf.Max(0, nextDodge - Time.time);
        public float SkillReadyIn => Mathf.Max(0, nextSkill - Time.time);
        public bool IsDodging => dodgeRemaining > 0f;
        public bool IsUsingSkill => skillActive;
        public event System.Action Dodged;
        public event System.Action SkillUsed;
        public bool CanDodge => isActiveAndEnabled && LevelDirector.Instance != null &&
            LevelDirector.Instance.IsPlaying && balance != null && !Health.IsDead && !skillActive &&
            dodgeRemaining <= 0f && Time.time >= nextDodge;
        public bool CanUseSkill => isActiveAndEnabled && LevelDirector.Instance != null &&
            LevelDirector.Instance.IsPlaying && balance != null && !Health.IsDead && !skillActive && Time.time >= nextSkill;

        public bool TryDodge()
        {
            if (!CanDodge) return false;
            dodgeDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : Facing;
            dodgeRemaining = balance.dodgeDuration;
            nextDodge = Time.time + balance.dodgeCooldown;
            invulnerableUntil = Time.time + balance.dodgeInvulnerability;
            skillAttack?.CancelForDodge();
            pendingAttackStep = Vector2.zero;
            SetContactGhost(true);
            visual?.PlayDodge(balance.dodgeDuration);
            skillEffects?.DodgeBegin(body.position, dodgeDirection);
            Dodged?.Invoke();
            return true;
        }

        public bool TryUseSkill()
        {
            if (!CanUseSkill) return false;
            nextSkill = Time.time + balance.skillCooldown;
            dodgeRemaining = 0f;
            skillOrigin = body.position;
            skillDirection = Facing;
            skillDestination = ClampToPlayArea(skillOrigin + skillDirection * balance.skillDistance);
            skillElapsed = 0f;
            skillActive = true;
            SetContactGhost(true);
            pendingAttackStep = Vector2.zero;
            invulnerableUntil = Time.time + SkillDuration + 0.03f;
            visual?.PlaySkill(SkillDuration + 0.08f);
            skillAttack?.BeginSkillStrike();
            skillEffects?.SkillBegin(skillOrigin, skillDirection);
            SkillUsed?.Invoke();
            return true;
        }

        public void StepIntoAttack(Vector2 direction, float distanceScale = 1f)
        {
            pendingAttackStep = direction.normalized * (balance.attackStep * distanceScale);
        }

        public Vector2 ClampToPlayArea(Vector2 position)
        {
            position = new Vector2(Mathf.Clamp(position.x, balance.playableMin.x, balance.playableMax.x),
                Mathf.Clamp(position.y, balance.playableMin.y, balance.playableMax.y));
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null || !viewCamera.orthographic) return position;
            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            Vector2 center = viewCamera.transform.position;
            const float spriteMargin = 1.15f;
            position.x = Mathf.Clamp(position.x, center.x - Mathf.Max(0f, halfWidth - spriteMargin),
                center.x + Mathf.Max(0f, halfWidth - spriteMargin));
            position.y = Mathf.Clamp(position.y, center.y - Mathf.Max(0f, halfHeight - spriteMargin),
                center.y + Mathf.Max(0f, halfHeight - spriteMargin));
            return position;
        }
    }
}
