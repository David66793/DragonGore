using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Changban.Tests
{
    public sealed class PlayerLocomotionPolishTests
    {
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        private bool inputConfigured;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (inputConfigured)
            {
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
                inputConfigured = false;
            }
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator IdleShadowStaysBetweenBootsForBothFacings()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            level.Begin();
            DisableEnemies();
            PlayerVisualAnimator visual = level.player.GetComponent<PlayerVisualAnimator>();
            visual.enabled = false;
            Animator animator = visual.Target;
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
            SpriteRenderer graphic = visual.CharacterRenderer;
            Transform shadow = level.player.transform.Find("Generated Shadow");
            Assert.NotNull(shadow);
            // Artist-measured centre of the two boot soles in the unmodified idle PNG.
            Vector2 bootCentre = new Vector2(0.46f, 0.12f);
            foreach (bool flip in new[] { false, true })
            {
                graphic.flipX = flip;
                Vector2 local = (Vector2.Scale(bootCentre, graphic.sprite.rect.size) - graphic.sprite.pivot) /
                                graphic.sprite.pixelsPerUnit;
                if (flip) local.x = -local.x;
                Vector3 footWorld = graphic.transform.TransformPoint(local);
                Assert.That(Vector2.Distance(shadow.position, footWorld), Is.LessThan(0.12f),
                    "The ground shadow must sit between the boots, not at the old right-boot pivot.");
                Capture(flip ? "IdleShadowWestV4.png" : "IdleShadowEastV4.png", level.player.transform.position);
            }
        }

        [UnityTest]
        public IEnumerator HorizontalRunningAlternatesSixFramesAndWaitsBeforeIdle()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            level.Begin();
            DisableEnemies();
            PlayerVisualAnimator visual = level.player.GetComponent<PlayerVisualAnimator>();
            // Batchmode has no focused Game View. Route this test's virtual keyboard to runtime.
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            inputConfigured = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            var frames = new System.Collections.Generic.HashSet<Sprite>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            Assert.That(GameInput.Move.x, Is.GreaterThan(0f), "Virtual D must reach runtime input.");
            float until = Time.time + 1.2f;
            while (Time.time < until && frames.Count < 6)
            {
                yield return null;
                Sprite sprite = visual.CharacterRenderer.sprite;
                if (sprite != null && sprite.name.StartsWith("ZY_E_RunV4_")) frames.Add(sprite);
            }
            Assert.That(frames.Count, Is.EqualTo(6), "Run must have six separate alternating-leg poses.");
            Assert.IsFalse(visual.CharacterRenderer.flipX);
            Capture("RunEastV4.png", level.player.transform.position);
            level.Pause();
            Sprite pausedSprite = visual.CharacterRenderer.sprite;
            AnimatorStateInfo pausedState = visual.Target.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.AreSame(pausedSprite, visual.CharacterRenderer.sprite, "Pause must hold the run pose.");
            Assert.That(visual.Target.GetCurrentAnimatorStateInfo(0).normalizedTime,
                Is.EqualTo(pausedState.normalizedTime).Within(0.001f));
            Assert.IsTrue(visual.Target.GetCurrentAnimatorStateInfo(0).IsName("Walk"));
            foreach (Sprite frame in frames.OrderBy(s => s.name))
            {
                visual.CharacterRenderer.sprite = frame;
                Capture(frame.name + ".png", level.player.transform.position);
            }
            visual.CharacterRenderer.sprite = pausedSprite;
            level.Resume();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return new WaitForSeconds(0.16f);
            Assert.IsFalse(visual.Target.GetCurrentAnimatorStateInfo(0).IsName("Idle"),
                "A brief stop must retain the combat-ready stance rather than flash to idle.");
            yield return new WaitForSeconds(1.10f);
            Assert.IsTrue(visual.Target.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            yield return new WaitForSeconds(0.18f);
            Assert.That(visual.CharacterRenderer.sprite.name, Does.StartWith("ZY_E_RunV4_"));
            Assert.IsTrue(visual.CharacterRenderer.flipX);
            Capture("RunWestV4.png", level.player.transform.position);
        }

        private static void DisableEnemies()
        {
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                enemy.enabled = false;
        }

        private static void Capture(string filename, Vector2 focus)
        {
            Camera camera = Camera.main;
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            RenderTexture target = RenderTexture.GetTemporary(960, 720, 24);
            RenderTexture previous = RenderTexture.active;
            RenderTexture oldTarget = camera.targetTexture;
            var pixels = new Texture2D(960, 720, TextureFormat.RGB24, false);
            try
            {
                camera.transform.position = new Vector3(focus.x, focus.y + 0.7f, oldPosition.z);
                camera.orthographicSize = 1.9f;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                pixels.Apply();
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Screenshots"));
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, filename), pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.transform.position = oldPosition;
                camera.orthographicSize = oldSize;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(pixels);
            }
        }
    }
}
