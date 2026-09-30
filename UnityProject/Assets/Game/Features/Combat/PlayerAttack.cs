using UnityEngine;

namespace Changban
{
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        public GameBalance balance;
        private PlayerMotor motor;
        private PlayerAttackVfx effects;
        private HitRegistry<Health> hits;
        private float timer;
        private int phase; // 0 idle, 1 windup, 2 active, 3 recovery
        private int combo;
        private bool queued;
        private float comboExpiresAt;
        private Vector2 attackDirection;
        private PlayerVisualAnimator visual;
        private HitRegistry<Health> skillHits;
        public bool IsAttacking => phase != 0;
        public int CurrentPhase => phase;
        public int CurrentComboStep => combo;
        public event System.Action<int> AttackStarted;
        public bool CanRequestAttack => isActiveAndEnabled && LevelDirector.Instance != null &&
            LevelDirector.Instance.IsPlaying && balance != null && motor != null && !motor.Health.IsDead &&
            !motor.IsUsingSkill && !motor.IsDodging && (phase == 0 || (combo < 3 && !queued));

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            visual = GetComponent<PlayerVisualAnimator>();
            effects = GetComponent<PlayerAttackVfx>();
            if (effects == null) effects = gameObject.AddComponent<PlayerAttackVfx>();
        }

        private void Update()
        {
            if (LevelDirector.Instance == null || !LevelDirector.Instance.IsPlaying) return;
            if (GameInput.Attack) RequestAttack();
            if (phase == 0)
            {
                if (combo > 0 && Time.time > comboExpiresAt) combo = 0;
                return;
            }
            timer -= Time.deltaTime;
            if (timer > 0) { if (phase == 2) ApplyHits(); return; }
            if (phase == 1)
            {
                phase = 2;
                timer = balance.attackActive;
                motor.StepIntoAttack(attackDirection, combo == 3 ? 1.7f : combo == 2 ? 0.85f : 1f);
                effects?.Strike(attackDirection, ReachForStep(), balance.attackActive, combo);
                ApplyHits();
            }
            else if (phase == 2)
            {
                phase = 3;
                timer = balance.attackRecovery;
                effects?.StopTrail();
            }
            else if (queued && combo < 3)
            {
                queued = false;
                BeginStrike(combo + 1);
            }
            else
            {
                phase = 0;
                queued = false;
                comboExpiresAt = Time.time + 0.38f;
            }
        }

        // Kept separate from input polling so UI controls and tests can request the same strike.
        public void RequestAttack()
        {
            if (!CanRequestAttack) return;
            if (phase == 0)
                BeginStrike(combo > 0 && combo < 3 && Time.time <= comboExpiresAt ? combo + 1 : 1);
            else if (combo < 3) queued = true;
        }

        private void BeginStrike(int step)
        {
            combo = step;
            attackDirection = FindAimDirection(motor.Facing);
            hits = new HitRegistry<Health>();
            phase = 1;
            timer = balance.attackWindup;
            visual?.PlayAttack(balance.attackWindup + balance.attackActive + balance.attackRecovery,
                attackDirection, combo);
            effects?.Windup(combo, attackDirection);
            AttackStarted?.Invoke(combo);
        }

        private float ReachForStep()
        {
            return balance.attackReach * (combo == 3 ? 1.20f : combo == 2 ? 1.07f : 1f);
        }

        private Vector2 FindAimDirection(Vector2 facing)
        {
            Vector2 chosen = facing;
            float bestScore = float.NegativeInfinity;
            foreach (Collider2D candidate in Physics2D.OverlapCircleAll(transform.position, balance.attackReach + 0.3f))
            {
                EnemyBrain enemy = candidate.GetComponentInParent<EnemyBrain>();
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead) continue;
                Vector2 offset = (Vector2)enemy.transform.position - (Vector2)transform.position;
                float distance = offset.magnitude;
                if (distance < 0.05f || distance > balance.attackReach + 0.25f) continue;
                Vector2 direction = offset / distance;
                float alignment = Vector2.Dot(direction, facing);
                if (alignment < balance.attackAimDot) continue;
                float score = alignment * 2f - distance * 0.35f;
                if (score <= bestScore) continue;
                bestScore = score;
                chosen = direction;
            }
            return chosen;
        }

        private void ApplyHits()
        {
            float reach = ReachForStep();
            Vector2 center = (Vector2)transform.position + attackDirection * (reach * 0.55f);
            float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
            Collider2D[] near = Physics2D.OverlapBoxAll(center,
                new Vector2(reach, balance.attackWidth * (combo == 2 ? 1.23f : 1f)), angle);
            foreach (Collider2D candidate in near)
            {
                EnemyBrain enemy = candidate.GetComponentInParent<EnemyBrain>();
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead) continue;
                Vector2 offset = enemy.transform.position - transform.position;
                if (offset.sqrMagnitude < 0.001f || Vector2.Dot(offset.normalized, attackDirection) < balance.attackAimDot) continue;
                if (hits.TryMark(enemy.Health))
                {
                    effects?.Impact(enemy.transform.position, attackDirection, combo == 3);
                    enemy.ReceiveHit(balance.attackDamage + (combo == 3 ? 1 : 0), transform.position, balance.knockback);
                }
            }
        }

        public void SkillStrike()
        {
            SkillStrike(transform.position, transform.position);
        }

        public void SkillStrike(Vector2 origin, Vector2 destination)
        {
            BeginSkillStrike();
            Vector2 travel = destination - origin;
            Vector2 direction = travel.sqrMagnitude > 0.001f ? travel.normalized : motor.Facing;
            SkillStrikeSegment(origin, destination, direction);
            EndSkillStrike();
        }

        public void BeginSkillStrike()
        {
            CancelForDodge();
            skillHits = new HitRegistry<Health>();
        }

        public void CancelForDodge()
        {
            phase = 0;
            combo = 0;
            queued = false;
            comboExpiresAt = 0f;
            effects?.StopTrail();
            effects?.CancelFinisherBurst();
        }

        public void EndSkillStrike() { skillHits = null; }

        public void SkillStrikeSegment(Vector2 origin, Vector2 destination, Vector2 direction)
        {
            if (skillHits == null) skillHits = new HitRegistry<Health>();
            float length = (destination - origin).magnitude + 1.35f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector2 center = origin + direction * (length * 0.5f);
            foreach (Collider2D candidate in Physics2D.OverlapBoxAll(center,
                new Vector2(length, balance.attackWidth * 1.25f), angle))
            {
                EnemyBrain enemy = candidate.GetComponentInParent<EnemyBrain>();
                if (enemy != null && enemy.Health != null && !enemy.Health.IsDead &&
                    Vector2.Dot((Vector2)enemy.transform.position - origin, direction) >= 0f && skillHits.TryMark(enemy.Health))
                {
                    effects?.SkillImpact(enemy.transform.position, direction);
                    enemy.ReceiveHit(balance.attackDamage + 1, origin, balance.knockback * 2f);
                }
            }
        }
    }
}
