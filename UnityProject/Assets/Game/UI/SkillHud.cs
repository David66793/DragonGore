using UnityEngine;
using UnityEngine.UI;

namespace Changban
{
    // Keyboard hints only. Action events provide feedback; input and combat stay with the player.
    public sealed class SkillHud : MonoBehaviour
    {
        [SerializeField] private LevelDirector level;
        private PlayerMotor motor;
        private PlayerAttack attack;
        private Canvas canvas;
        private bool subscribed;
        private readonly int[] feedbackVersion = new int[3];
        private readonly float[] flashUntil = new float[3];
        private readonly Slot[] slots = new Slot[3];
        private const float FlashDuration = 0.18f;

        private static readonly Color Iron = new Color(0.075f, 0.094f, 0.098f, 0.97f);
        private static readonly Color Gold = new Color(0.66f, 0.52f, 0.29f, 0.94f);
        private static readonly Color Silver = new Color(0.87f, 0.85f, 0.78f);
        private static readonly Color DimGold = new Color(0.34f, 0.31f, 0.25f, 0.85f);
        private static readonly Color DimSilver = new Color(0.44f, 0.46f, 0.44f);
        private static readonly Color FlashGold = new Color(0.97f, 0.84f, 0.58f);

        private sealed class Slot
        {
            public Image frame;
            public Image surface;
            public SkillIconGraphic icon;
            public Image veil;
            public Text countdown;
            public Text key;
        }

        public void Configure(LevelDirector owner)
        {
            bool changed = level != owner;
            Unsubscribe();
            level = owner;
            if (!Application.isPlaying) return;
            motor = level != null ? level.player : null;
            attack = motor != null ? motor.GetComponent<PlayerAttack>() : null;
            if (changed)
                for (int i = 0; i < 3; i++) { feedbackVersion[i] = 0; flashUntil[i] = 0f; }
            EnsureCanvas();
            canvas.gameObject.SetActive(isActiveAndEnabled);
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            if (level == null) level = Object.FindAnyObjectByType<LevelDirector>();
            Configure(level);
        }

        private void OnEnable()
        {
            if (canvas != null) canvas.gameObject.SetActive(true);
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            for (int i = 0; i < 3; i++) flashUntil[i] = 0f;
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private void OnDestroy() { Unsubscribe(); }

        private void Subscribe()
        {
            if (subscribed || !Application.isPlaying || !isActiveAndEnabled || motor == null || attack == null) return;
            attack.AttackStarted += OnAttack;
            motor.Dodged += OnDodge;
            motor.SkillUsed += OnSkill;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (attack != null) attack.AttackStarted -= OnAttack;
            if (motor != null)
            {
                motor.Dodged -= OnDodge;
                motor.SkillUsed -= OnSkill;
            }
            subscribed = false;
        }

        private bool CanShowAction => isActiveAndEnabled && level != null && level.IsPlaying &&
                                      motor != null && motor.Health != null && !motor.Health.IsDead;

        public int FeedbackVersion(int index) => index >= 0 && index < 3 ? feedbackVersion[index] : 0;
        public bool IsFlashing(int index) => index >= 0 && index < 3 && CanShowAction && Time.time < flashUntil[index];

        public bool IsAvailable(int index)
        {
            if (!CanShowAction || attack == null) return false;
            switch (index)
            {
                case 0: return attack.CanRequestAttack;
                case 1: return motor.CanDodge;
                case 2: return motor.CanUseSkill;
                default: return false;
            }
        }

        private void OnAttack(int _) { Feedback(0); }
        private void OnDodge() { Feedback(1); }
        private void OnSkill() { Feedback(2); }

        private void Feedback(int index)
        {
            if (!CanShowAction) return;
            feedbackVersion[index]++;
            flashUntil[index] = Time.time + FlashDuration;
            Refresh();
        }

        private void Update() { Refresh(); }

        private void Refresh()
        {
            if (canvas == null) return;
            if (level == null || !level.IsPlaying)
                for (int i = 0; i < 3; i++) flashUntil[i] = 0f;
            for (int i = 0; i < 3; i++)
            {
                Slot slot = slots[i];
                bool available = IsAvailable(i);
                bool flashing = IsFlashing(i);
                slot.frame.color = flashing ? FlashGold : available ? Gold : DimGold;
                slot.surface.color = flashing ? new Color(0.22f, 0.20f, 0.13f, 0.97f) : Iron;
                slot.icon.color = flashing ? Color.white : available ? Silver : DimSilver;
                slot.key.color = flashing ? FlashGold : available ? Silver : DimSilver;
                float remaining = motor == null || i == 0 ? 0f : i == 1 ? motor.DodgeReadyIn : motor.SkillReadyIn;
                float duration = motor == null || motor.balance == null ? 1f :
                    i == 1 ? motor.balance.dodgeCooldown : motor.balance.skillCooldown;
                float ratio = Mathf.Clamp01(remaining / Mathf.Max(0.01f, duration));
                slot.veil.gameObject.SetActive(remaining > 0f);
                slot.veil.rectTransform.sizeDelta = new Vector2(69f, 69f * ratio);
                slot.countdown.gameObject.SetActive(remaining > 0f);
                slot.countdown.text = remaining > 0f ? remaining.ToString("0.0") : "";
            }
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            GameObject holder = new GameObject("Skill HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            holder.transform.SetParent(transform, false);
            canvas = holder.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = holder.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform bar = MakeRect("Skill Bar", holder.transform, new Vector2(270f, 136f),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 22f));
            Image plate = bar.gameObject.AddComponent<Image>();
            plate.color = new Color(0.045f, 0.06f, 0.065f, 0.88f);
            plate.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string[] names = { "Combo", "Evade", "Thrust" };
            string[] keys = { "J", "K", "L" };
            for (int i = 0; i < 3; i++)
            {
                RectTransform card = MakeRect(names[i] + " [" + keys[i] + "]", bar, new Vector2(72f, 72f),
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(15f + i * 84f, -6f));
                var slot = new Slot();
                slots[i] = slot;
                slot.frame = card.gameObject.AddComponent<Image>();
                slot.frame.raycastTarget = false;
                slot.frame.color = Gold;
                RectTransform surface = MakeRect("Iron", card, new Vector2(69f, 69f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                slot.surface = surface.gameObject.AddComponent<Image>();
                slot.surface.color = Iron;
                slot.surface.raycastTarget = false;
                RectTransform icon = MakeRect("Icon", card, new Vector2(47f, 47f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                slot.icon = icon.gameObject.AddComponent<SkillIconGraphic>();
                slot.icon.SetKind(i);
                slot.icon.color = Silver;
                slot.icon.raycastTarget = false;
                RectTransform veil = MakeRect("Cooldown veil", card, new Vector2(69f, 69f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 1.5f));
                slot.veil = veil.gameObject.AddComponent<Image>();
                slot.veil.color = new Color(0.02f, 0.025f, 0.03f, 0.75f);
                slot.veil.raycastTarget = false;
                slot.countdown = MakeText("Cooldown value", card, font, 23, new Vector2(68f, 36f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, "");
                slot.countdown.color = new Color(0.98f, 0.89f, 0.71f);
                slot.countdown.fontStyle = FontStyle.Bold;
                slot.key = MakeText("Key", card, font, 18, new Vector2(72f, 24f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), keys[i]);
                slot.key.fontStyle = FontStyle.Bold;
                Text label = MakeText("Label", card, font, 12, new Vector2(80f, 18f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), names[i]);
                label.color = new Color(0.69f, 0.68f, 0.61f);
            }
        }

        private static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 anchor,
            Vector2 pivot, Vector2 position)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            RectTransform rect = holder.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Text MakeText(string name, Transform parent, Font font, int size, Vector2 bounds,
            Vector2 anchor, Vector2 pivot, Vector2 position, string value)
        {
            RectTransform rect = MakeRect(name, parent, bounds, anchor, pivot, position);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.text = value;
            return label;
        }
    }

    // Small procedural silver silhouettes, using no imported artwork or runtime materials.
    public sealed class SkillIconGraphic : Graphic
    {
        private int kind;
        public void SetKind(int value) { kind = value; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (kind == 0)
            {
                Spear(mesh, new Vector2(-0.65f, -0.65f), new Vector2(0.49f, 0.49f), 0.08f);
                Line(mesh, new Vector2(-0.47f, -0.55f), new Vector2(-0.58f, -0.44f), 0.05f);
            }
            else if (kind == 1)
            {
                Circle(mesh, new Vector2(0.02f, 0.47f), 0.14f);
                Line(mesh, new Vector2(-0.01f, 0.25f), new Vector2(0.15f, -0.10f), 0.10f);
                Line(mesh, new Vector2(0.00f, 0.16f), new Vector2(-0.29f, 0.04f), 0.07f);
                Line(mesh, new Vector2(0.00f, 0.16f), new Vector2(0.33f, 0.21f), 0.07f);
                Line(mesh, new Vector2(0.15f, -0.10f), new Vector2(-0.24f, -0.51f), 0.09f);
                Line(mesh, new Vector2(0.15f, -0.10f), new Vector2(0.48f, -0.32f), 0.09f);
                Line(mesh, new Vector2(-0.76f, 0.29f), new Vector2(-0.32f, 0.29f), 0.05f, 0.5f);
                Line(mesh, new Vector2(-0.85f, 0.03f), new Vector2(-0.42f, 0.03f), 0.05f, 0.5f);
                Line(mesh, new Vector2(-0.74f, -0.23f), new Vector2(-0.36f, -0.23f), 0.05f, 0.5f);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    float offset = (i - 1) * 0.32f;
                    Spear(mesh, new Vector2(-0.73f, -0.30f + offset), new Vector2(0.43f, 0.28f + offset), 0.06f);
                }
            }
        }

        private void Spear(VertexHelper mesh, Vector2 from, Vector2 tipBase, float width)
        {
            Line(mesh, from, tipBase, width);
            Vector2 forward = (tipBase - from).normalized;
            Vector2 side = new Vector2(-forward.y, forward.x);
            Triangle(mesh, tipBase + forward * 0.27f, tipBase - forward * 0.10f + side * 0.12f,
                tipBase - forward * 0.10f - side * 0.12f, 1f);
            Line(mesh, tipBase - forward * 0.13f - side * 0.16f,
                tipBase - forward * 0.20f - side * 0.20f, 0.055f, 0.7f);
        }

        private void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width, float alpha = 1f)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x) * (width * 0.5f);
            Triangle(mesh, from - side, from + side, to + side, alpha);
            Triangle(mesh, from - side, to + side, to - side, alpha);
        }

        private void Circle(VertexHelper mesh, Vector2 centre, float radius)
        {
            const int count = 12;
            for (int i = 0; i < count; i++)
            {
                float first = i * Mathf.PI * 2f / count;
                float next = (i + 1) * Mathf.PI * 2f / count;
                Triangle(mesh, centre, centre + new Vector2(Mathf.Cos(first), Mathf.Sin(first)) * radius,
                    centre + new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * radius, 1f);
            }
        }

        private void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, float alpha)
        {
            Rect bounds = rectTransform.rect;
            Color tint = color;
            tint.a *= alpha;
            int first = mesh.currentVertCount;
            AddVertex(mesh, bounds, a, tint);
            AddVertex(mesh, bounds, b, tint);
            AddVertex(mesh, bounds, c, tint);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        private static void AddVertex(VertexHelper mesh, Rect bounds, Vector2 point, Color tint)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = bounds.center + Vector2.Scale(point, bounds.size * 0.5f);
            vertex.color = tint;
            mesh.AddVert(vertex);
        }
    }
}
