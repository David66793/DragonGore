using UnityEngine;

namespace Changban
{
    public sealed class ArtValidationControls : MonoBehaviour
    {
        public Camera testCamera;
        public SpriteRenderer character;
        public Sprite[] poses;
        public Material idleMaterial;
        public Material attackMaterial;
        public Material verticalAttackMaterial;
        public Material northWalkMaterial;
        public Material southWalkMaterial;
        public Material comboMaterial;
        public Material northComboMaterial;
        public Material southComboMaterial;
        public Material runMaterial;
        private readonly Color[] backgrounds = {
            new Color(0.72f, 0.72f, 0.71f),
            new Color(0.17f, 0.19f, 0.20f),
            new Color(0.43f, 0.40f, 0.35f)
        };
        private readonly int[] heights = { 128, 160, 192 };
        private int backgroundIndex;
        private int heightIndex = 1;
        private int poseIndex;

        private void Start()
        {
            if (poses != null)
            {
                int idleIndex = System.Array.FindIndex(poses, sprite => sprite != null && sprite.name == "ZY_SE_Idle_A");
                if (idleIndex >= 0) poseIndex = idleIndex;
            }
            Apply();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;
            var keys = UnityEngine.InputSystem.Keyboard.current;
            if (keys.digit1Key.wasPressedThisFrame) backgroundIndex = 0;
            if (keys.digit2Key.wasPressedThisFrame) backgroundIndex = 1;
            if (keys.digit3Key.wasPressedThisFrame) backgroundIndex = 2;
            if (keys.digit4Key.wasPressedThisFrame) heightIndex = 0;
            if (keys.digit5Key.wasPressedThisFrame) heightIndex = 1;
            if (keys.digit6Key.wasPressedThisFrame) heightIndex = 2;
            if (keys.digit7Key.wasPressedThisFrame && poses != null && poses.Length > 0)
                poseIndex = (poseIndex + poses.Length - 1) % poses.Length;
            if (keys.digit8Key.wasPressedThisFrame && poses != null && poses.Length > 0)
                poseIndex = (poseIndex + 1) % poses.Length;
            Apply();
        }

        private void Apply()
        {
            if (testCamera == null || character == null || character.sprite == null) return;
            if (poses != null && poses.Length > 0)
            {
                character.sprite = poses[poseIndex];
                string pose = character.sprite.name;
                Material desired = pose.StartsWith("ZY_E_RunV4_") ? runMaterial : pose.Contains("WalkV3")
                    ? pose.StartsWith("ZY_N_") ? northWalkMaterial : southWalkMaterial
                    : pose.Contains("Combo") ? pose.StartsWith("ZY_N_") ? northComboMaterial :
                        pose.StartsWith("ZY_S_") ? southComboMaterial : comboMaterial
                    : pose.StartsWith("ZY_N_") || pose.StartsWith("ZY_S_")
                        ? verticalAttackMaterial
                        : pose.Contains("AttackV2") ? attackMaterial : idleMaterial;
                if (desired != null && character.sharedMaterial != desired) character.sharedMaterial = desired;
            }
            testCamera.backgroundColor = backgrounds[backgroundIndex];
            float worldHeight = character.sprite.bounds.size.y;
            testCamera.orthographicSize = worldHeight * Screen.height / (2f * heights[heightIndex]);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 440, 97), "Art Validation — southeast source poses");
            GUI.Label(new Rect(25, 40, 410, 62), $"1–3 background   4–6 size: {heights[heightIndex]} px   7/8 pose\n" +
                $"{character.sprite.name} — check alpha, spear, cape and feet.");
        }
    }
}
