using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Changban.Tests
{
    public sealed class SkillHudPlayTests
    {
        private LevelDirector level;
        private PlayerAttack attack;
        private MonoBehaviour hud;
        private Type hudType;
        private GameObject host;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            level = Object.FindAnyObjectByType<LevelDirector>();
            Assert.NotNull(level);
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>()) Object.Destroy(enemy.gameObject);
            yield return null;
            attack = level.player.GetComponent<PlayerAttack>();
            hudType = Type.GetType("Changban.SkillHud, Changban.Game");
            Assert.NotNull(hudType, "The separate UGUI skill hint bar must be implemented.");
            // Disable the scene-owned bar when testing a separately configured instance.
            foreach (MonoBehaviour component in Object.FindObjectsByType<MonoBehaviour>())
                if (component != null && component.GetType() == hudType) component.enabled = false;
            host = new GameObject("Skill HUD test host");
            hud = (MonoBehaviour)host.AddComponent(hudType);
            Configure();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            if (host != null) Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OverlayHasThreeReadableNonInteractiveSkillHints()
        {
            level.Begin();
            yield return null;
            Canvas canvas = host.GetComponentInChildren<Canvas>();
            Assert.NotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            foreach (string name in new[] { "Combo [J]", "Evade [K]", "Thrust [L]" })
            {
                RectTransform slot = canvas.transform.Find("Skill Bar/" + name) as RectTransform;
                Assert.NotNull(slot, "Each skill needs a labeled hint slot.");
                Assert.AreEqual(new Vector2(72f, 72f), slot.sizeDelta);
                Assert.NotNull(slot.Find("Icon"));
                Assert.NotNull(slot.Find("Key"));
                Assert.NotNull(slot.Find("Label"));
                foreach (Component component in slot.GetComponentsInChildren<Component>())
                {
                    Assert.AreNotEqual("Button", component.GetType().Name,
                        "The skill bar provides keyboard hints; it must not add mouse casting.");
                    PropertyInfo raycast = component.GetType().GetProperty("raycastTarget");
                    if (raycast != null) Assert.IsFalse((bool)raycast.GetValue(component));
                }
            }
            Assert.IsTrue(Available(0));
            Assert.IsTrue(Available(1));
            Assert.IsTrue(Available(2));
            yield return null;
            CaptureOverlay("SkillHudReady.png");
        }

        [UnityTest]
        public IEnumerator ComboFeedbackWaitsForActualQueuedStrikeRatherThanTheExtraRequest()
        {
            level.Begin();
            attack.RequestAttack();
            Assert.AreEqual(1, Version(0));
            Assert.IsTrue(Flashing(0));
            attack.RequestAttack();
            Assert.AreEqual(1, Version(0), "Queuing the next J must not flash before it actually starts.");
            yield return null;
            yield return null;
            CaptureOverlay("SkillHudComboFeedback.png");
            float deadline = Time.time + 1.2f;
            while (Version(0) < 2 && Time.time < deadline) yield return null;
            Assert.AreEqual(2, Version(0), "The queued second strike should emit one feedback event.");
            Assert.AreEqual(2, attack.CurrentComboStep);
            Assert.IsTrue(Flashing(0));
            yield return new WaitForSeconds(0.25f);
            Assert.IsFalse(Flashing(0), "Feedback must end rather than stay lit through the whole combo.");
        }

        [UnityTest]
        public IEnumerator SuccessfulEvadeAndThrustFlashOnceWhileRepeatedCooldownRequestsStayQuiet()
        {
            level.Begin();
            Assert.IsTrue(level.player.TryDodge());
            Assert.AreEqual(1, Version(1));
            Assert.IsTrue(Flashing(1), "A successful K should flash even though its cooldown has just begun.");
            Assert.IsFalse(Available(1));
            Assert.IsFalse(level.player.TryDodge());
            Assert.AreEqual(1, Version(1));
            RectTransform evadeVeil = FindSlot("Evade [K]").Find("Cooldown veil") as RectTransform;
            yield return null;
            Assert.IsTrue(evadeVeil.gameObject.activeSelf);
            Assert.That(evadeVeil.sizeDelta.y, Is.GreaterThan(0f));
            yield return null;
            CaptureOverlay("SkillHudEvadeFeedback.png");
            yield return new WaitForSeconds(level.balance.dodgeCooldown + 0.05f);
            Assert.IsTrue(Available(1));
            Assert.IsFalse(evadeVeil.gameObject.activeSelf);

            Assert.IsTrue(level.player.TryUseSkill());
            Assert.AreEqual(1, Version(2));
            Assert.IsTrue(Flashing(2));
            Assert.IsFalse(Available(2));
            Assert.IsFalse(level.player.TryUseSkill());
            Assert.AreEqual(1, Version(2));
            yield return null;
            RectTransform thrustVeil = FindSlot("Thrust [L]").Find("Cooldown veil") as RectTransform;
            Assert.IsTrue(thrustVeil.gameObject.activeSelf);
            Assert.That(thrustVeil.sizeDelta.y, Is.GreaterThan(30f));
            Component countdown = FindSlot("Thrust [L]").Find("Cooldown value").GetComponent("Text");
            Assert.NotNull(countdown);
            Assert.That(countdown.GetType().GetProperty("text").GetValue(countdown).ToString(), Is.Not.Empty);
            yield return null;
            CaptureOverlay("SkillHudThrustCooldown.png");
        }

        [UnityTest]
        public IEnumerator ReadyAndPausedPhasesCannotStartFeedbackAndPauseRemovesCurrentHighlight()
        {
            Assert.AreEqual(LevelDirector.Phase.Ready, level.CurrentPhase);
            attack.RequestAttack();
            Assert.IsFalse(level.player.TryDodge());
            Assert.IsFalse(level.player.TryUseSkill());
            AssertAllInactive();
            yield return null;
            yield return null;
            CaptureOverlay("SkillHudInactive.png");
            level.Begin();
            attack.RequestAttack();
            Assert.IsTrue(Flashing(0));
            level.Pause();
            yield return null;
            int before = Version(0);
            attack.RequestAttack();
            Assert.IsFalse(level.player.TryDodge());
            Assert.IsFalse(level.player.TryUseSkill());
            AssertAllInactive();
            Assert.AreEqual(before, Version(0));
            yield return null;
            CaptureOverlay("SkillHudPausedState.png");
            level.Resume();
            yield return null;
            Assert.IsFalse(Flashing(0), "Resuming must not replay a stale pre-pause flash.");
        }

        [UnityTest]
        public IEnumerator ReconfigureDoesNotDuplicateCanvasOrEventSubscriptionsAndDisableUnsubscribes()
        {
            Configure();
            Configure();
            Assert.AreEqual(1, host.GetComponentsInChildren<Canvas>(true).Length);
            level.Begin();
            Assert.IsTrue(level.player.TryDodge());
            Assert.AreEqual(1, Version(1), "Repeated Configure calls must leave exactly one listener.");
            yield return new WaitForSeconds(level.balance.dodgeDuration + 0.1f);
            hud.enabled = false;
            Assert.IsFalse(host.GetComponentInChildren<Canvas>(true).gameObject.activeSelf);
            Assert.IsTrue(level.player.TryUseSkill());
            Assert.AreEqual(0, Version(2), "A disabled HUD must not keep receiving action events.");
            yield return new WaitForSeconds(0.4f);
            hud.enabled = true;
            attack.RequestAttack();
            Assert.AreEqual(1, Version(0), "Re-enabling should restore exactly one listener.");
            Assert.IsTrue(host.GetComponentInChildren<Canvas>(true).gameObject.activeSelf);
        }

        private void Configure() { hudType.GetMethod("Configure").Invoke(hud, new object[] { level }); }
        private int Version(int index) => (int)hudType.GetMethod("FeedbackVersion").Invoke(hud, new object[] { index });
        private bool Flashing(int index) => (bool)hudType.GetMethod("IsFlashing").Invoke(hud, new object[] { index });
        private bool Available(int index) => (bool)hudType.GetMethod("IsAvailable").Invoke(hud, new object[] { index });
        private Transform FindSlot(string name) => host.GetComponentInChildren<Canvas>(true).transform.Find("Skill Bar/" + name);

        private void AssertAllInactive()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.IsFalse(Flashing(i));
                Assert.IsFalse(Available(i));
            }
        }

        private static void CaptureOverlay(string filename)
        {
            // A hidden/batch window has no usable backbuffer. Render the real
            // skill canvas through the scene camera, then restore its Overlay mode.
            // IMGUI menus are not part of this camera/skill-bar capture.
            Camera camera = Camera.main;
            Assert.NotNull(camera);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>();
            var modes = new RenderMode[canvases.Length];
            var owners = new Camera[canvases.Length];
            var distances = new float[canvases.Length];
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture oldTarget = camera.targetTexture;
            RenderTexture oldActive = RenderTexture.active;
            Texture2D capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                for (int i = 0; i < canvases.Length; i++)
                {
                    modes[i] = canvases[i].renderMode;
                    owners[i] = canvases[i].worldCamera;
                    distances[i] = canvases[i].planeDistance;
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = 1f;
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                capture.Apply();
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Screenshots"));
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, filename), capture.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = modes[i];
                    canvases[i].worldCamera = owners[i];
                    canvases[i].planeDistance = distances[i];
                }
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(capture);
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
