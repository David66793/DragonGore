using UnityEngine;

namespace Changban
{
    // Keeps pose changes and small visual motion independent from enemy AI and physics.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyVisual : MonoBehaviour
    {
        [SerializeField] private Sprite idle;
        // Kept as "action" so existing prefab data remains the warning pose.
        [SerializeField] private Sprite action;
        [SerializeField] private Sprite strike;
        [SerializeField] private Sprite recover;
        [SerializeField] private Material idleMaterial;
        [SerializeField] private Material actionMaterial;
        [SerializeField] private Material strikeMaterial;
        [SerializeField] private Material recoverMaterial;
        private EnemyBrain brain;
        private EnemyAttackVfx attackVfx;
        private SpriteRenderer graphic;
        private Vector3 restPosition;
        private bool dead;
        public Sprite Idle => idle;
        public Sprite Action => action;
        public Sprite Warning => action;
        public Sprite Strike => strike;
        public Sprite Recover => recover;
        public Material IdleMaterial => idleMaterial;
        public Material ActionMaterial => actionMaterial;
        public Material WarningMaterial => actionMaterial;
        public Material StrikeMaterial => strikeMaterial;
        public Material RecoverMaterial => recoverMaterial;

        public void Configure(Sprite idleSprite, Sprite actionSprite, Material idleMat, Material actionMat)
        {
            idle = idleSprite;
            action = actionSprite;
            idleMaterial = idleMat;
            actionMaterial = actionMat;
        }

        public void Configure(Sprite idleSprite, Sprite warningSprite, Sprite strikeSprite, Sprite recoverSprite,
            Material idleMat, Material warningMat, Material strikeMat, Material recoverMat)
        {
            idle = idleSprite;
            action = warningSprite;
            strike = strikeSprite;
            recover = recoverSprite;
            idleMaterial = idleMat;
            actionMaterial = warningMat;
            strikeMaterial = strikeMat;
            recoverMaterial = recoverMat;
        }

        private void Awake()
        {
            brain = GetComponentInParent<EnemyBrain>();
            attackVfx = GetComponentInParent<EnemyAttackVfx>();
            graphic = GetComponent<SpriteRenderer>();
            restPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (brain == null || graphic == null) return;
            if (attackVfx == null) attackVfx = GetComponentInParent<EnemyAttackVfx>();
            if (brain.Health != null && brain.Health.IsDead)
            {
                if (!dead)
                {
                    dead = true;
                    graphic.color = new Color(0.37f, 0.37f, 0.37f, 0.68f);
                }
                return;
            }
            bool warning = brain.CurrentState == EnemyBrain.State.Warning;
            bool striking = brain.CurrentState == EnemyBrain.State.Attack;
            bool recovering = brain.CurrentState == EnemyBrain.State.Recover;
            Sprite desiredSprite = idle;
            Material desired = idleMaterial;
            if (warning && action != null && actionMaterial != null)
            {
                desiredSprite = action;
                desired = actionMaterial;
            }
            else if (striking)
            {
                if (strike != null && strikeMaterial != null)
                {
                    desiredSprite = strike;
                    desired = strikeMaterial;
                }
                else if (action != null && actionMaterial != null)
                {
                    desiredSprite = action;
                    desired = actionMaterial;
                }
            }
            else if (recovering && recover != null && recoverMaterial != null)
            {
                desiredSprite = recover;
                desired = recoverMaterial;
            }
            if (graphic.sprite != desiredSprite) graphic.sprite = desiredSprite;
            if (desired != null && graphic.sharedMaterial != desired) graphic.sharedMaterial = desired;
            PlayerMotor player = LevelDirector.Instance != null ? LevelDirector.Instance.player : null;
            Vector2 toward = attackVfx != null ? attackVfx.AimDirection : Vector2.right;
            if (player != null && attackVfx == null)
            {
                Vector2 offset = player.transform.position - transform.position;
                if (offset.sqrMagnitude > 0.001f) toward = offset.normalized;
            }
            float bob = brain.CurrentState == EnemyBrain.State.Approach ? Mathf.Sin(Time.time * 8f) * 0.025f : 0f;
            float reach = 0f;
            if (warning) reach = -0.07f * brain.StateProgress;
            else if (striking)
            {
                float contact = Mathf.Clamp(brain.AttackContactProgress, 0.01f, 0.99f);
                float peak = brain.role == EnemyBrain.Role.Elite ? 0.27f : 0.20f;
                // Move into the contact pose quickly, then release the thrust.
                // This uses the AI phase clock rather than a separate VFX timer.
                reach = brain.StateProgress <= contact
                    ? Mathf.Lerp(-0.07f, peak, Mathf.SmoothStep(0f, 1f, brain.StateProgress / contact))
                    : Mathf.Lerp(peak, 0.04f, Mathf.SmoothStep(0f, 1f, (brain.StateProgress - contact) / (1f - contact)));
            }
            else if (recovering) reach = Mathf.Lerp(0.04f, 0f, brain.StateProgress);
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            Vector2 visualOffset = toward * reach + Vector2.up * bob;
            transform.localPosition = restPosition + new Vector3(visualOffset.x / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                visualOffset.y / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 0);
            if ((warning || striking || recovering) && Mathf.Abs(toward.x) > 0.15f)
                graphic.flipX = toward.x < 0f;
            else if (!warning && !striking && !recovering && player != null &&
                     Mathf.Abs(player.transform.position.x - transform.position.x) > 0.15f)
                graphic.flipX = player.transform.position.x < transform.position.x;
        }
    }
}
