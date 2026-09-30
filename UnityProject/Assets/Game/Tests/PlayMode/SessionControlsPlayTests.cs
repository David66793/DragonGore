using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Changban.Tests
{
    public sealed class SessionControlsPlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator CleanUp() { Time.timeScale = 1f; yield return null; }

        [UnityTest]
        public IEnumerator PauseAndResumeOnlyChangeAnActiveBattle()
        {
            LevelDirector level = LevelDirector.Instance;
            level.Pause();
            Assert.AreEqual(LevelDirector.Phase.Ready, level.CurrentPhase);
            level.Begin();
            level.Pause();
            Assert.AreEqual(LevelDirector.Phase.Paused, level.CurrentPhase);
            Assert.AreEqual(0f, Time.timeScale);
            level.Resume();
            Assert.AreEqual(LevelDirector.Phase.Playing, level.CurrentPhase);
            Assert.AreEqual(1f, Time.timeScale);
            level.player.Health.TakeDamage(100);
            level.Pause();
            level.Resume();
            Assert.AreEqual(LevelDirector.Phase.Defeat, level.CurrentPhase);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RestartGameFromPauseResetsAndImmediatelyStartsFreshBattle()
        {
            MethodInfo restart = typeof(LevelDirector).GetMethod("RestartGame");
            Assert.NotNull(restart, "A live battle needs a restart entry point as well as the old return-to-ready reset.");
            for (int attempt = 0; attempt < 3; attempt++)
            {
                LevelDirector level = LevelDirector.Instance;
                if (!level.IsPlaying) level.Begin();
                level.player.transform.position = level.rescuePoint.position;
                Assert.IsTrue(level.TryRescue());
                level.Pause();
                restart.Invoke(level, null);
                yield return null;
                yield return null;
                level = LevelDirector.Instance;
                Assert.AreEqual(LevelDirector.Phase.Playing, level.CurrentPhase);
                Assert.AreEqual(1f, Time.timeScale);
                Assert.IsFalse(level.Rescued);
                Assert.IsFalse(level.EliteDefeated);
                Assert.AreEqual(0, level.DefeatedCount);
                Assert.AreEqual(level.balance.playerHealth, level.player.Health.Current);
                Assert.AreEqual(6, Object.FindObjectsByType<EnemyBrain>().Length);
                Assert.AreEqual(0f, level.player.DodgeReadyIn);
                Assert.AreEqual(0f, level.player.SkillReadyIn);
            }
        }

        [UnityTest]
        public IEnumerator ExitRequestClearsPauseAndIsObservableInEditor()
        {
            MethodInfo quit = typeof(LevelDirector).GetMethod("QuitGame");
            PropertyInfo requested = typeof(LevelDirector).GetProperty("ExitRequested");
            Assert.NotNull(quit);
            Assert.NotNull(requested);
            LevelDirector level = LevelDirector.Instance;
            level.Begin(); level.Pause();
            // Isolate the Editor adapter so this same test also runs safely in
            // an interactive Test Runner without stopping its PlayMode session.
            Type editorType = Type.GetType("Changban.EditorTools.GameUiSetup, Changban.Editor");
            Assert.NotNull(editorType);
            MethodInfo stop = editorType.GetMethod("StopEditorPlay", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(stop);
            Action editorStop = (Action)Delegate.CreateDelegate(typeof(Action), stop);
            int exitEvents = 0;
            Action observe = () => exitEvents++;
            LevelDirector.EditorExitRequested -= editorStop;
            LevelDirector.EditorExitRequested += observe;
            try
            {
                quit.Invoke(level, null);
                Assert.IsTrue((bool)requested.GetValue(level));
                Assert.AreEqual(1f, Time.timeScale);
                Assert.IsFalse(level.IsPlaying);
                Assert.AreEqual(1, exitEvents);
                quit.Invoke(level, null);
                Assert.AreEqual(1, exitEvents, "Repeated quit requests must be idempotent.");
            }
            finally
            {
                LevelDirector.EditorExitRequested -= observe;
                LevelDirector.EditorExitRequested += editorStop;
            }
            yield return null;
        }
    }
}
