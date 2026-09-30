using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Changban.Tests
{
    public sealed class EnemyAttackCycleTests
    {
        private LevelDirector level;
        private GameBalance testBalance;
        private readonly List<GameObject> spawned = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            level.Begin();
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>())
                enemy.gameObject.SetActive(false);
            testBalance = Object.Instantiate(level.balance);
            testBalance.enemyWindup = 0.45f;
            testBalance.enemyRecovery = 0.18f;
            level.player.enabled = false;
            level.player.GetComponent<Collider2D>().enabled = false;
            level.player.GetComponent<Rigidbody2D>().position = Vector2.zero;
            level.player.Health.SetMaximum(100);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            foreach (GameObject item in spawned) if (item != null) Object.Destroy(item);
            spawned.Clear();
            foreach (ArrowProjectile arrow in Object.FindObjectsByType<ArrowProjectile>())
                Object.Destroy(arrow.gameObject);
            if (testBalance != null) Object.Destroy(testBalance);
            yield return null;
        }

        // Re-emitting the short warning clusters must fail this: the original burst
        // should remain readable through the windup without adding new particles.
        [UnityTest]
        public IEnumerator EveryRoleKeepsOneStableWarningCue()
        {
            foreach (EnemyBrain prefab in Prefabs())
            {
                EnemyBrain enemy = Spawn(prefab);
                yield return WaitForState(enemy, EnemyBrain.State.Warning);
                ParticleSystem particles = enemy.transform.Find("Enemy Warning Particles").GetComponent<ParticleSystem>();
                Assert.That(particles.particleCount, Is.InRange(1, 8));
                HashSet<uint> initialParticles = ParticleSeeds(particles);
                float deadline = Time.time + 0.24f;
                while (Time.time < deadline)
                {
                    Assert.AreEqual(EnemyBrain.State.Warning, enemy.CurrentState);
                    Assert.That(particles.particleCount, Is.InRange(1, 8),
                        prefab.role + " needs one sustained warning cue, without repeated flashes.");
                    Assert.That(ParticleSeeds(particles), Is.EquivalentTo(initialParticles),
                        "The same warning particles should persist rather than being cleared and re-emitted.");
                    yield return null;
                }
                Object.Destroy(enemy.gameObject);
                yield return null;
            }
        }

        // An immediate hit/release at Attack entry must fail. The movement into
        // the contact pose must precede damage, arrow creation and the strike burst.
        [UnityTest]
        public IEnumerator EveryRoleReachesContactAfterAttackStarts()
        {
            foreach (EnemyBrain prefab in Prefabs())
            {
                EnemyBrain enemy = Spawn(prefab);
                int health = level.player.Health.Current;
                yield return WaitForState(enemy, EnemyBrain.State.Attack);
                Assert.AreEqual(health, level.player.Health.Current,
                    prefab.role + " must not deal damage before its contact frame.");
                Assert.IsEmpty(Object.FindObjectsByType<ArrowProjectile>(),
                    "The archer must not release before its contact frame.");
                ParticleSystem particles = enemy.transform.Find(prefab.role == EnemyBrain.Role.Archer
                    ? "Enemy Strike Dust" : "Enemy Strike Particles").GetComponent<ParticleSystem>();
                Assert.AreEqual(0, particles.particleCount, "Strike particles must mark contact rather than Attack entry.");
                Object.Destroy(enemy.gameObject);
                yield return null;
            }
        }

        // Clear hit immunity between frames so duplicate attack decisions cannot
        // hide behind PlayerMotor's legitimate 0.45-second damage protection.
        [UnityTest]
        public IEnumerator MeleeAndEliteDamageOnlyOnceAcrossTheirAttackAndRecovery()
        {
            FieldInfo immunity = typeof(PlayerMotor).GetField("invulnerableUntil", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(immunity);
            foreach (EnemyBrain prefab in new[] { level.meleePrefab, level.elitePrefab })
            {
                EnemyBrain enemy = Spawn(prefab);
                int health = level.player.Health.Current;
                int damageEvents = 0;
                System.Action<Health> onChanged = _ => damageEvents++;
                level.player.Health.Changed += onChanged;
                try
                {
                    yield return WaitForState(enemy, EnemyBrain.State.Warning);
                    float deadline = Time.time + 2f;
                    while (enemy.CurrentState != EnemyBrain.State.Approach && Time.time < deadline)
                    {
                        immunity.SetValue(level.player, -1f);
                        yield return null;
                    }
                    Assert.AreEqual(EnemyBrain.State.Approach, enemy.CurrentState, "The cycle must finish.");
                    Assert.AreEqual(1, damageEvents, "One attack must make one damage decision, even without hit immunity.");
                    Assert.AreEqual(health - (prefab.role == EnemyBrain.Role.Elite ? 2 : 1), level.player.Health.Current);
                }
                finally { level.player.Health.Changed -= onChanged; }
                Object.Destroy(enemy.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ArcherReleasesOneArrowDuringOneCycle()
        {
            EnemyBrain enemy = Spawn(level.archerPrefab);
            yield return WaitForState(enemy, EnemyBrain.State.Warning);
            var arrows = new HashSet<ArrowProjectile>();
            float deadline = Time.time + 2f;
            while (enemy.CurrentState != EnemyBrain.State.Approach && Time.time < deadline)
            {
                foreach (ArrowProjectile arrow in Object.FindObjectsByType<ArrowProjectile>())
                    arrows.Add(arrow);
                yield return null;
            }
            Assert.AreEqual(EnemyBrain.State.Approach, enemy.CurrentState);
            Assert.AreEqual(1, arrows.Count, "A single archer attack cycle must release exactly one visible projectile.");
        }

        [UnityTest]
        public IEnumerator EveryRoleEmitsOneStrikeParticleBurstAcrossItsCycle()
        {
            foreach (EnemyBrain prefab in Prefabs())
            {
                EnemyBrain enemy = Spawn(prefab);
                yield return WaitForState(enemy, EnemyBrain.State.Warning);
                ParticleSystem particles = enemy.transform.Find(prefab.role == EnemyBrain.Role.Archer
                    ? "Enemy Strike Dust" : "Enemy Strike Particles").GetComponent<ParticleSystem>();
                var seen = new HashSet<uint>();
                float deadline = Time.time + 2f;
                while (enemy.CurrentState != EnemyBrain.State.Approach && Time.time < deadline)
                {
                    seen.UnionWith(ParticleSeeds(particles));
                    yield return null;
                }
                int oneBurst = prefab.role == EnemyBrain.Role.Elite ? 28 : prefab.role == EnemyBrain.Role.Archer ? 4 : 22;
                Assert.AreEqual(oneBurst, seen.Count,
                    prefab.role + " must emit one strike burst without repeated flashes during recovery.");
                Object.Destroy(enemy.gameObject);
                foreach (ArrowProjectile arrow in Object.FindObjectsByType<ArrowProjectile>()) Object.Destroy(arrow.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator WarningLocksDirectionAndMovingBehindAvoidsMeleeHit()
        {
            EnemyBrain enemy = Spawn(level.meleePrefab);
            yield return WaitForState(enemy, EnemyBrain.State.Warning);
            EnemyAttackVfx vfx = enemy.GetComponent<EnemyAttackVfx>();
            Vector2 locked = vfx.AimDirection;
            int health = level.player.Health.Current;
            level.player.GetComponent<Rigidbody2D>().position = (Vector2)enemy.transform.position - locked * 0.7f;
            float deadline = Time.time + 2f;
            while (enemy.CurrentState != EnemyBrain.State.Approach && Time.time < deadline)
            {
                Assert.That(Vector2.Dot(locked, vfx.AimDirection), Is.GreaterThan(0.999f),
                    "An already telegraphed attack must keep its direction when the player circles behind.");
                yield return null;
            }
            Assert.AreEqual(health, level.player.Health.Current, "Moving behind the locked strike should avoid melee damage.");
        }

        [UnityTest]
        public IEnumerator RecoveryPoseBeginsWhenBrainFinishesAttack()
        {
            EnemyBrain enemy = Spawn(level.meleePrefab);
            yield return WaitForState(enemy, EnemyBrain.State.Recover);
            yield return null;
            EnemyAttackVfx vfx = enemy.GetComponent<EnemyAttackVfx>();
            EnemyVisual visual = enemy.GetComponentInChildren<EnemyVisual>();
            Assert.IsFalse(vfx.IsStriking, "Recovery must not be extended by a separate VFX strike timer.");
            Assert.AreEqual(visual.Recover, visual.GetComponent<SpriteRenderer>().sprite,
                "The rendered pose must agree with the Brain recovery state.");
        }

        [UnityTest]
        public IEnumerator ShortRecoveryReturnsToIdleBeforeAnotherWarning()
        {
            EnemyBrain enemy = Spawn(level.meleePrefab);
            yield return WaitForState(enemy, EnemyBrain.State.Recover);
            float recoverAt = Time.time;
            yield return WaitForState(enemy, EnemyBrain.State.Approach);
            Assert.That(Time.time - recoverAt, Is.LessThan(0.32f), "The visible recovery should be short.");
            EnemyVisual visual = enemy.GetComponentInChildren<EnemyVisual>();
            float deadline = Time.time + 0.30f;
            while (Time.time < deadline)
            {
                yield return null;
                Assert.AreEqual(EnemyBrain.State.Approach, enemy.CurrentState,
                    "The cooldown should give an idle beat before the next warning.");
                Assert.AreEqual(visual.Idle, visual.GetComponent<SpriteRenderer>().sprite);
            }
        }

        [UnityTest]
        public IEnumerator PausedWindupFreezesAndDeadEnemiesNeverReachContact()
        {
            foreach (EnemyBrain prefab in Prefabs())
            {
                EnemyBrain enemy = Spawn(prefab);
                yield return WaitForState(enemy, EnemyBrain.State.Warning);
                EnemyAttackVfx vfx = enemy.GetComponent<EnemyAttackVfx>();
                int health = level.player.Health.Current;
                float progress = vfx.WarningProgress;
                level.Pause();
                yield return new WaitForSecondsRealtime(0.20f);
                Assert.AreEqual(EnemyBrain.State.Warning, enemy.CurrentState);
                Assert.AreEqual(progress, vfx.WarningProgress, 0.001f);
                Assert.AreEqual(health, level.player.Health.Current);
                Assert.IsEmpty(Object.FindObjectsByType<ArrowProjectile>());
                enemy.Health.TakeDamage(100);
                Assert.AreEqual(EnemyBrain.State.Dead, enemy.CurrentState);
                level.Resume();
                yield return new WaitForSeconds(0.55f);
                Assert.AreEqual(health, level.player.Health.Current, "Dead enemies must never resolve a pending attack.");
                Assert.IsEmpty(Object.FindObjectsByType<ArrowProjectile>());
            }
        }

        [UnityTest]
        public IEnumerator PausedAttackCannotReachContactAndDeathCancelsPendingStrike()
        {
            foreach (EnemyBrain prefab in Prefabs())
            {
                EnemyBrain enemy = Spawn(prefab);
                int health = level.player.Health.Current;
                yield return WaitForState(enemy, EnemyBrain.State.Attack);
                EnemyAttackVfx vfx = enemy.GetComponent<EnemyAttackVfx>();
                ParticleSystem strikeParticles = enemy.transform.Find("Enemy Strike Particles").GetComponent<ParticleSystem>();
                Assert.AreEqual(health, level.player.Health.Current, "The fixture must still be before contact.");
                Assert.AreEqual(0, strikeParticles.particleCount);
                Assert.IsEmpty(Object.FindObjectsByType<ArrowProjectile>());
                float progress = vfx.StrikeProgress;
                level.Pause();
                yield return new WaitForSecondsRealtime(0.20f);
                Assert.AreEqual(EnemyBrain.State.Attack, enemy.CurrentState);
                Assert.AreEqual(progress, vfx.StrikeProgress, 0.001f,
                    "Pausing before contact must freeze the shared attack clock.");
                Assert.AreEqual(health, level.player.Health.Current);
                Assert.AreEqual(0, strikeParticles.particleCount,
                    "No strike burst should appear while the pending contact is paused.");
                Assert.IsEmpty(Object.FindObjectsByType<ArrowProjectile>());
                enemy.Health.TakeDamage(100);
                Assert.AreEqual(EnemyBrain.State.Dead, enemy.CurrentState);
                Assert.IsFalse(vfx.IsStriking);
                level.Resume();
                yield return new WaitForSeconds(0.55f);
                Assert.AreEqual(health, level.player.Health.Current,
                    "Killing the attacker before contact must cancel its pending damage.");
                Assert.IsEmpty(Object.FindObjectsByType<ArrowProjectile>(),
                    "Killing an archer before contact must cancel its pending arrow.");
            }
        }

        private EnemyBrain[] Prefabs() => new[] { level.meleePrefab, level.archerPrefab, level.elitePrefab };

        private static HashSet<uint> ParticleSeeds(ParticleSystem system)
        {
            var particles = new ParticleSystem.Particle[system.main.maxParticles];
            int count = system.GetParticles(particles);
            var seeds = new HashSet<uint>();
            for (int i = 0; i < count; i++) seeds.Add(particles[i].randomSeed);
            return seeds;
        }

        private EnemyBrain Spawn(EnemyBrain prefab)
        {
            float distance = prefab.role == EnemyBrain.Role.Archer ? 4.1f : 0.7f;
            EnemyBrain enemy = Object.Instantiate(prefab,
                (Vector2)level.player.transform.position + Vector2.right * distance, Quaternion.identity);
            enemy.balance = testBalance;
            enemy.GetComponent<Collider2D>().enabled = false;
            spawned.Add(enemy.gameObject);
            return enemy;
        }

        private static IEnumerator WaitForState(EnemyBrain enemy, EnemyBrain.State state)
        {
            float deadline = Time.time + 2f;
            while (enemy != null && enemy.CurrentState != state && Time.time < deadline) yield return null;
            Assert.NotNull(enemy);
            Assert.AreEqual(state, enemy.CurrentState, "The enemy must reach " + state + " within the deadline.");
        }
    }
}
