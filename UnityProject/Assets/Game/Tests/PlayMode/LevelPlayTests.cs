using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Changban.Tests
{
    public sealed class LevelPlayTests
    {
        [UnityTest]
        public IEnumerator PlayerDeathPauseAndRestartResetTheScene()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            Assert.AreEqual(LevelDirector.Phase.Ready, level.CurrentPhase);
            level.Begin();
            level.Pause();
            Assert.AreEqual(0f, Time.timeScale);
            level.Resume();
            Assert.AreEqual(1f, Time.timeScale);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                level.player.ReceiveHit(100, level.player.transform.position + Vector3.left);
                Assert.AreEqual(LevelDirector.Phase.Defeat, level.CurrentPhase);
                level.Restart();
                yield return null;
                level = Object.FindFirstObjectByType<LevelDirector>();
                Assert.NotNull(level);
                Assert.AreEqual(LevelDirector.Phase.Ready, level.CurrentPhase);
                Assert.IsFalse(level.Rescued);
                Assert.IsFalse(level.EliteDefeated);
                Assert.AreEqual(0, level.DefeatedCount);
                Assert.AreEqual(6, Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length);
                level.Begin();
            }
        }

        [UnityTest]
        public IEnumerator NearbyMeleeEnemyWarnsThenDamagesPlayer()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            level.Begin();
            var prefab = level.meleePrefab;
            var enemy = Object.Instantiate(prefab, level.player.transform.position + Vector3.right * 0.7f, Quaternion.identity);
            int before = level.player.Health.Current;
            float deadline = Time.time + level.balance.enemyWindup + level.balance.enemyAttackDuration + 0.4f;
            while (enemy.CurrentState != EnemyBrain.State.Recover && Time.time < deadline)
                yield return null;
            Assert.AreEqual(EnemyBrain.State.Recover, enemy.CurrentState);
            Assert.Less(level.player.Health.Current, before);
        }

        [UnityTest]
        public IEnumerator RescueEliteAndExitCompleteTheLevel()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            level.Begin();
            level.player.transform.position = level.rescuePoint.position;
            Assert.IsTrue(level.TryRescue());
            Assert.IsTrue(level.Rescued);
            Assert.IsFalse(level.EliteDefeated);
            Assert.AreEqual(LevelDirector.Phase.Playing, level.CurrentPhase);
            EnemyBrain elite = null;
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                if (enemy.role == EnemyBrain.Role.Elite) elite = enemy;
            Assert.NotNull(elite);
            elite.Health.TakeDamage(100);
            Assert.IsTrue(level.EliteDefeated);
            level.player.transform.position = level.exitPoint.position;
            yield return null;
            Assert.AreEqual(LevelDirector.Phase.Victory, level.CurrentPhase);
        }
    }
}
