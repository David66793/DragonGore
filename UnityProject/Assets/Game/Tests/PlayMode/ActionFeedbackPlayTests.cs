using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Changban.Tests
{
    public sealed class ActionFeedbackPlayTests
    {
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior oldBackground;
        private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ChangbanDemo"); yield return null;
            foreach (EnemyBrain enemy in UnityEngine.Object.FindObjectsByType<EnemyBrain>()) enemy.enabled = false;
            LevelDirector.Instance.Begin();
        }
        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            Time.timeScale = 1f;
            if (keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealKeyboardCooldownPressesAndPausedKeysDoNotReplayHudFeedback()
        {
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            SkillHud hud = UnityEngine.Object.FindAnyObjectByType<SkillHud>();
            Assert.NotNull(hud, "The main scene must contain the new skill HUD.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K));
            yield return null; yield return null;
            Assert.AreEqual(1, hud.FeedbackVersion(1));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K));
            yield return null; yield return null;
            Assert.AreEqual(1, hud.FeedbackVersion(1), "Cooldown K must not execute or flash again.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.L));
            yield return null; yield return null;
            Assert.AreEqual(1, hud.FeedbackVersion(2));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.L));
            yield return null; yield return null;
            Assert.AreEqual(1, hud.FeedbackVersion(2), "Cooldown L must not execute or flash again.");
            LevelDirector.Instance.Pause();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J, Key.K, Key.L));
            yield return null; yield return null;
            Assert.AreEqual(0, hud.FeedbackVersion(0));
            Assert.AreEqual(1, hud.FeedbackVersion(1));
            Assert.AreEqual(1, hud.FeedbackVersion(2));
            for (int i = 0; i < 3; i++) Assert.IsFalse(hud.IsFlashing(i));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null; yield return null;
            Assert.IsTrue(LevelDirector.Instance.IsPlaying, "Esc must resume from the paused menu.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null; yield return null;
            Assert.AreEqual(LevelDirector.Phase.Paused, LevelDirector.Instance.CurrentPhase);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            yield return null; yield return null;
            Assert.IsTrue(LevelDirector.Instance.IsPlaying, "R must reload and begin a fresh game from pause.");
            SkillHud freshHud = UnityEngine.Object.FindAnyObjectByType<SkillHud>();
            Assert.AreNotSame(hud, freshHud);
            for (int i = 0; i < 3; i++) Assert.AreEqual(0, freshHud.FeedbackVersion(i));
        }

        [UnityTest]
        public IEnumerator DodgeFeedbackOnlyFiresForAcceptedActions()
        {
            PlayerMotor motor = LevelDirector.Instance.player;
            EventInfo ev = typeof(PlayerMotor).GetEvent("Dodged"); Assert.NotNull(ev);
            int count = 0; Action listener = () => count++;
            ev.AddEventHandler(motor, listener);
            Assert.IsTrue(motor.TryDodge()); Assert.AreEqual(1, count);
            Assert.IsFalse(motor.TryDodge()); Assert.AreEqual(1, count);
            yield return new WaitForSeconds(motor.balance.dodgeCooldown + 0.1f);
            LevelDirector.Instance.Pause();
            Assert.IsFalse(motor.TryDodge()); Assert.AreEqual(1, count);
            LevelDirector.Instance.Resume();
            Assert.IsTrue(motor.TryDodge()); Assert.AreEqual(2, count);
            ev.RemoveEventHandler(motor, listener);
        }

        [UnityTest]
        public IEnumerator ThrustFeedbackDoesNotRepeatDuringCooldownOrPause()
        {
            PlayerMotor motor = LevelDirector.Instance.player;
            EventInfo ev = typeof(PlayerMotor).GetEvent("SkillUsed"); Assert.NotNull(ev);
            int count = 0; Action listener = () => count++;
            ev.AddEventHandler(motor, listener);
            Assert.IsTrue(motor.TryUseSkill()); Assert.AreEqual(1, count);
            for (int i = 0; i < 4; i++) Assert.IsFalse(motor.TryUseSkill());
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(motor.TryUseSkill()); Assert.AreEqual(1, count);
            LevelDirector.Instance.Pause();
            Assert.IsFalse(motor.TryUseSkill()); Assert.AreEqual(1, count);
            ev.RemoveEventHandler(motor, listener);
        }

        [UnityTest]
        public IEnumerator ComboFeedbackTracksExecutedStrikesRatherThanQueuedKeySpam()
        {
            PlayerAttack attack = LevelDirector.Instance.player.GetComponent<PlayerAttack>();
            EventInfo ev = typeof(PlayerAttack).GetEvent("AttackStarted"); Assert.NotNull(ev);
            var steps = new List<int>(); Action<int> listener = step => steps.Add(step);
            ev.AddEventHandler(attack, listener);
            attack.RequestAttack();
            Assert.AreEqual(new[] { 1 }, steps);
            attack.RequestAttack(); attack.RequestAttack();
            Assert.AreEqual(new[] { 1 }, steps);
            float deadline = Time.time + 3f;
            bool thirdQueued = false;
            while (Time.time < deadline && (steps.Count < 3 || attack.IsAttacking))
            {
                if (attack.CurrentComboStep == 2 && !thirdQueued)
                { attack.RequestAttack(); attack.RequestAttack(); thirdQueued = true; }
                yield return null;
            }
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, steps);
            LevelDirector.Instance.Pause(); attack.RequestAttack();
            Assert.AreEqual(3, steps.Count);
            ev.RemoveEventHandler(attack, listener);
        }
    }
}
