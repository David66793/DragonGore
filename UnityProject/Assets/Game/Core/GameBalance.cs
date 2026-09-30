using UnityEngine;

namespace Changban
{
    [CreateAssetMenu(menuName = "Changban/Game Balance")]
    public sealed class GameBalance : ScriptableObject
    {
        [Header("Player")]
        public float moveSpeed = 4.2f;
        public int playerHealth = 6;
        public float dodgeSpeed = 9f;
        public float dodgeDuration = 0.24f;
        public float dodgeCooldown = 0.8f;
        public float dodgeInvulnerability = 0.34f;
        public float skillDistance = 2.5f;
        public float skillCooldown = 5f;
        [Min(0.1f)] public float playerIdleDelay = 0.9f;

        [Header("Arena (world units)")]
        public Vector2 playableMin = new Vector2(-14.5f, -3.6f);
        public Vector2 playableMax = new Vector2(23f, 3.6f);
        public Vector2 visibleGroundMin = new Vector2(-28f, -9f);
        public Vector2 visibleGroundMax = new Vector2(37f, 9f);

        [Header("Combat")]
        public int attackDamage = 2;
        public float attackReach = 1.45f;
        public float attackArcDot = 0.25f;
        public float attackWidth = 0.72f;
        public float attackAimDot = 0.55f;
        public float attackStep = 0.16f;
        public float attackWindup = 0.17f;
        public float attackActive = 0.12f;
        public float attackRecovery = 0.20f;
        public float knockback = 0.28f;

        [Header("Enemies")]
        public int meleeHealth = 4;
        public int archerHealth = 3;
        public int eliteHealth = 12;
        public float enemySpeed = 2.0f;
        public float enemyAggroRange = 8f;
        public int enemyDamage = 1;
        public float enemyWindup = 0.6f;
        public float enemyRecovery = 0.18f;
        [Min(0.02f)] public float enemyAttackDuration = 0.16f;
        [Min(0f)] public float enemyContactTime = 0.07f;
        [Min(0f)] public float enemyCooldown = 0.60f;
        [Range(-1f, 1f)] public float enemyAttackAimDot = 0.25f;
        public float arrowSpeed = 6f;
    }
}
