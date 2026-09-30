using UnityEngine;

namespace Changban
{
    public sealed class GameHud : MonoBehaviour
    {
        public LevelDirector level;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle centered;
        private GUIStyle button;
        private GUIStyle skill;
        private PlayerAttack attack;
        private int lastHealth = -1;
        private float hurtFlashUntil;

        private static readonly Color Ink = new Color(0.08f, 0.10f, 0.10f, 0.88f);
        private static readonly Color Gold = new Color(0.81f, 0.66f, 0.40f);
        private static readonly Color Cream = new Color(0.93f, 0.89f, 0.79f);

        private void EnsureStyles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            heading.normal.textColor = Cream;
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            body.normal.textColor = Cream;
            small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            small.normal.textColor = new Color(0.78f, 0.78f, 0.72f);
            skill = new GUIStyle(small) { fontSize = 14, fontStyle = FontStyle.Bold };
            skill.normal.textColor = new Color(0.55f, 0.82f, 0.92f);
            centered = new GUIStyle(heading) { fontSize = 29, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };
            button.normal.background = null;
            button.hover.background = null;
            button.active.background = null;
            button.normal.textColor = Cream;
            button.hover.textColor = Color.white;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void OnGUI()
        {
            if (level == null || level.player == null) return;
            EnsureStyles();
            if (attack == null) attack = level.player.GetComponent<PlayerAttack>();
            int currentHealth = level.player.Health.Current;
            if (lastHealth >= 0 && currentHealth < lastHealth) hurtFlashUntil = Time.unscaledTime + 0.32f;
            lastHealth = currentHealth;
            float hurtAlpha = Mathf.Clamp01((hurtFlashUntil - Time.unscaledTime) / 0.32f) * 0.34f;
            if (hurtAlpha > 0f)
            {
                Color red = new Color(0.87f, 0.17f, 0.12f, hurtAlpha);
                Fill(new Rect(0, 0, Screen.width, 20), red);
                Fill(new Rect(0, Screen.height - 20, Screen.width, 20), red);
                Fill(new Rect(0, 0, 20, Screen.height), red);
                Fill(new Rect(Screen.width - 20, 0, 20, Screen.height), red);
            }

            float panelWidth = Mathf.Min(455f, Screen.width - 24f);
            panelWidth = Mathf.Min(panelWidth, Screen.width - 165f);
            Fill(new Rect(12, 12, panelWidth, 151), Ink);
            Fill(new Rect(12, 12, 3, 151), Gold);
            GUI.Label(new Rect(28, 18, panelWidth - 40, 33), "CHANGBAN  /  BREAKOUT", heading);
            GUI.Label(new Rect(28, 54, 50, 20), "VITAL", small);
            int maximum = Mathf.Max(1, level.player.Health.Maximum);
            for (int i = 0; i < maximum; i++)
            {
                float width = Mathf.Min(39f, (panelWidth - 110f) / maximum - 4f);
                Fill(new Rect(81 + i * (width + 4), 56, width, 14), i < level.player.Health.Current ?
                    new Color(0.73f, 0.25f, 0.21f) : new Color(0.30f, 0.29f, 0.27f));
            }
            GUI.Label(new Rect(28, 79, panelWidth - 40, 43), level.Objective, body);
            Fill(new Rect(28, 124, panelWidth - 56, 1), new Color(0.54f, 0.47f, 0.35f));
            GUI.Label(new Rect(28, 132, panelWidth - 40, 23),
                attack != null && attack.CurrentComboStep > 0
                    ? $"FOES {level.DefeatedCount}    /    J COMBO {attack.CurrentComboStep} / 3"
                    : $"FOES {level.DefeatedCount}    /    J COMBO 1 - 2 - 3", small);
            GUI.Label(new Rect(18, Screen.height - 30, 310, 22), "WASD move   /   E rescue   /   R restart", small);

            if (level.CurrentPhase == LevelDirector.Phase.Playing)
            {
                if (MenuButton(new Rect(Screen.width - 143, 12, 131, 37), "Pause  /  Esc")) level.Pause();
                if (level.CurrentPhase == LevelDirector.Phase.Playing) return;
            }
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.015f, 0.02f, 0.02f, 0.38f));
            bool paused = level.CurrentPhase == LevelDirector.Phase.Paused;
            float panelX = Screen.width / 2f - 231f;
            float panelHeight = paused ? 345f : 294f;
            float panelY = Screen.height / 2f - panelHeight / 2f;
            Rect panel = new Rect(panelX, panelY, 462, panelHeight);
            Fill(panel, new Color(0.06f, 0.075f, 0.075f, 0.95f));
            Fill(new Rect(panel.x, panel.y, panel.width, 4), Gold);
            Fill(new Rect(panel.x, panel.y + panel.height - 4, panel.width, 4), Gold);
            string title = level.CurrentPhase == LevelDirector.Phase.Ready ? "CHANGBAN PASS" :
                level.CurrentPhase == LevelDirector.Phase.Paused ? "PAUSED" :
                level.CurrentPhase == LevelDirector.Phase.Victory ? "A DOU IS SAFE" : "FALLEN AT CHANGBAN";
            GUI.Label(new Rect(panel.x + 22, panel.y + 29, panel.width - 44, 48), title, centered);
            GUI.Label(new Rect(panel.x + 42, panel.y + 84, panel.width - 84, 30),
                level.CurrentPhase == LevelDirector.Phase.Ready ? "Break through the ruined village on foot." :
                level.CurrentPhase == LevelDirector.Phase.Victory ? "The escape route is open." :
                level.CurrentPhase == LevelDirector.Phase.Defeat ? "The route can be tried again." : "The battle waits for you.",
                new GUIStyle(body) { alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(panel.x + 22, panel.y + 117, panel.width - 44, 22),
                "J combo    /    K evade    /    L thrust", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            string label = level.CurrentPhase == LevelDirector.Phase.Ready ? "BEGIN  /  ENTER" :
                level.CurrentPhase == LevelDirector.Phase.Paused ? "RESUME  /  ESC" : "RESTART  /  R";
            if (MenuButton(new Rect(panel.x + 91, panel.y + 151, 280, 44), label))
            {
                if (level.CurrentPhase == LevelDirector.Phase.Ready) level.Begin();
                else if (level.CurrentPhase == LevelDirector.Phase.Paused) level.Resume();
                else level.RestartGame();
            }
            float quitY = panel.y + 205f;
            if (paused)
            {
                if (MenuButton(new Rect(panel.x + 91, panel.y + 205, 280, 44), "RESTART  /  R")) level.RestartGame();
                quitY += 54f;
            }
            if (MenuButton(new Rect(panel.x + 91, quitY, 280, 44), "QUIT GAME")) level.QuitGame();
            GUI.Label(new Rect(panel.x + 30, panel.y + panel.height - 31, panel.width - 60, 20),
                "Esc pause / resume    -    R restart", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }

        private bool MenuButton(Rect rect, string label)
        {
            bool hovered = rect.Contains(new Vector2(InputMouseX(), Screen.height - InputMouseY()));
            Fill(rect, hovered ? new Color(0.25f, 0.23f, 0.18f, 1f) : new Color(0.10f, 0.12f, 0.12f, 1f));
            Fill(new Rect(rect.x, rect.y, rect.width, 1), Gold);
            Fill(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Gold);
            Fill(new Rect(rect.x, rect.y, 1, rect.height), Gold);
            Fill(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Gold);
            return GUI.Button(rect, label, button);
        }

        private static float InputMouseX() => UnityEngine.InputSystem.Mouse.current?.position.ReadValue().x ?? -1f;
        private static float InputMouseY() => UnityEngine.InputSystem.Mouse.current?.position.ReadValue().y ?? -1f;
    }
}
