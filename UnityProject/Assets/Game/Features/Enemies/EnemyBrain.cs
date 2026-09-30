using UnityEngine;

namespace Changban
{
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        public enum Role { Melee, Archer, Elite }
        public enum State { Approach, Warning, Attack, Recover, Dead }
        public Role role;
        public GameBalance balance;
        public ArrowProjectile arrowPrefab;
        public Health Health { get; private set; }
        public State CurrentState { get; private set; }
        public float StateProgress => stateDuration > 0f ? Mathf.Clamp01(1f - timer / stateDuration) : 0f;
        public float AttackContactProgress => stateDuration > 0f ? contactAt / stateDuration : 0.5f;
        private Rigidbody2D body;
        private PlayerMotor player;
        private SpriteRenderer telegraph;
        private SpriteRenderer graphic;
        private EnemyAttackVfx attackVfx;
        private float timer;
        private float stateDuration;
        private float cooldownRemaining;
        private float contactAt;
        private bool contactResolved;
        private Vector2 attackDirection = Vector2.right;

        private void Awake()
        {
            Health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            graphic = GetComponent<SpriteRenderer>();
            telegraph = transform.Find("Warning")?.GetComponent<SpriteRenderer>();
            if (telegraph != null) telegraph.enabled = false;
            attackVfx = GetComponent<EnemyAttackVfx>();
            if (attackVfx == null) attackVfx = gameObject.AddComponent<EnemyAttackVfx>();
            Health.Died += OnDeath;
        }

        private void Start()
        {
            Health.SetMaximum(role == Role.Elite ? balance.eliteHealth : role == Role.Archer ? balance.archerHealth : balance.meleeHealth);
            player = LevelDirector.Instance != null ? LevelDirector.Instance.player : null;
            CurrentState = State.Approach;
        }

        private void OnDestroy() { if (Health != null) Health.Died -= OnDeath; }

        private void Update()
        {
            if (Health.IsDead || player == null || LevelDirector.Instance == null || !LevelDirector.Instance.IsPlaying) return;
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (CurrentState == State.Approach)
            {
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
                attackVfx.SetAim(AimAtPlayer());
                float attackRange = role == Role.Archer ? 4.5f : 1.15f;
                if (distance <= attackRange && cooldownRemaining <= 0f)
                {
                    attackDirection = AimAtPlayer();
                    EnterState(State.Warning, balance.enemyWindup * (role == Role.Elite ? 0.75f : 1f));
                    attackVfx.BeginWarning(role, attackDirection, stateDuration);
                }
            }
            else
            {
                timer -= Time.deltaTime;
                // One authoritative contact event owns the damage/projectile and
                // the strike burst. Hit immunity is not the deduplication mechanism.
                if (CurrentState == State.Attack && !contactResolved && stateDuration - timer >= contactAt)
                {
                    contactResolved = true;
                    attackVfx.EmitContact();
                    PerformAttack(attackDirection);
                }
                if (timer > 0) return;
                if (CurrentState == State.Warning)
                {
                    EnterState(State.Attack, Mathf.Max(0.04f, balance.enemyAttackDuration));
                    contactAt = Mathf.Clamp(balance.enemyContactTime, 0.01f, stateDuration - 0.01f);
                    contactResolved = false;
                    attackVfx.BeginAttack(role, attackDirection);
                }
                else if (CurrentState == State.Attack)
                {
                    EnterState(State.Recover, balance.enemyRecovery);
                    attackVfx.BeginRecovery();
                }
                else if (CurrentState == State.Recover)
                {
                    EnterState(State.Approach, 0f);
                    cooldownRemaining = Mathf.Max(0f, balance.enemyCooldown);
                }
            }
        }

        private void EnterState(State state, float duration)
        {
            CurrentState = state;
            stateDuration = state == State.Approach || state == State.Dead ? 0f : Mathf.Max(0.01f, duration);
            timer = stateDuration;
        }

        private void FixedUpdate()
        {
            if (Health.IsDead || player == null || LevelDirector.Instance == null || !LevelDirector.Instance.IsPlaying || CurrentState != State.Approach) return;
            Vector2 offset = player.transform.position - transform.position;
            float hold = role == Role.Archer ? 4f : 0.9f;
            if (offset.magnitude > hold && offset.magnitude < balance.enemyAggroRange * 2f)
                body.MovePosition(body.position + offset.normalized * (balance.enemySpeed * (role == Role.Elite ? 1.25f : 1f) * Time.fixedDeltaTime));
        }

        private Vector2 AimAtPlayer()
        {
            Vector2 toward = (Vector2)player.transform.position - body.position;
            return toward.sqrMagnitude > 0.001f ? toward.normalized : Vector2.right;
        }

        private void PerformAttack(Vector2 aim)
        {
            if (role == Role.Archer)
            {
                if (arrowPrefab != null)
                {
                    Vector2 muzzle = body.position + Vector2.up * 0.60f + aim * 0.38f;
                    ArrowProjectile arrow = Instantiate(arrowPrefab, muzzle, Quaternion.identity);
                    arrow.Initialize(aim, balance.arrowSpeed, balance.enemyDamage, player);
                }
            }
            else
            {
                Vector2 offset = (Vector2)player.transform.position - body.position;
                bool inDirection = offset.sqrMagnitude < 0.001f ||
                    Vector2.Dot(aim, offset.normalized) >= balance.enemyAttackAimDot;
                if (inDirection && offset.magnitude < (role == Role.Elite ? 1.65f : 1.3f) &&
                    player.ReceiveHit(role == Role.Elite ? balance.enemyDamage + 1 : balance.enemyDamage, body.position))
                    EnemyAttackVfx.SpawnImpact((Vector2)player.transform.position + Vector2.up * 0.52f, aim, false);
            }
        }

        public void ReceiveHit(int damage, Vector2 from, float knockback)
        {
            if (Health.IsDead) return;
            body.MovePosition(body.position + (body.position - from).normalized * knockback);
            Health.TakeDamage(damage);
            EnemyAttackVfx.SpawnImpact(body.position + Vector2.up * 0.60f, body.position - from, false);
        }

        private void OnDeath(Health _) 
        {
            EnterState(State.Dead, 0f);
            contactResolved = true;
            attackVfx?.Die();
            if (telegraph != null) telegraph.enabled = false;
            if (graphic != null) graphic.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            LevelDirector.Instance?.EnemyDefeated(this);
            Destroy(gameObject, 0.5f);
        }
    }
}
