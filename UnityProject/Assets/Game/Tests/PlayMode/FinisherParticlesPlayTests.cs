using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Changban.Tests
{
    public sealed class FinisherParticlesPlayTests
    {
        private LevelDirector level;
        private PlayerAttack attack;
        private ParticleSystem wind;
        private ParticleSystem sparks;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            level = Object.FindAnyObjectByType<LevelDirector>();
            Assert.NotNull(level);
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>())
                Object.Destroy(enemy.gameObject);
            yield return null;
            level.Begin();
            attack = level.player.GetComponent<PlayerAttack>();
            wind = level.player.transform.Find("Combo Finisher wind")?.GetComponent<ParticleSystem>();
            sparks = level.player.transform.Find("Combo Finisher sparks")?.GetComponent<ParticleSystem>();
            Assert.NotNull(wind, "The third J attack needs its own short spear-tip wind burst, separate from L.");
            Assert.NotNull(sparks, "The third J attack needs its own silver spark burst even when it misses.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyThirdStrikeEmitsAtActiveFrameAndLeavesBriefRecoveryAfterglow()
        {
            yield return ReachThirdActive();
            Assert.That(wind.particleCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(sparks.particleCount, Is.GreaterThanOrEqualTo(6));
            Assert.That(wind.particleCount + sparks.particleCount, Is.LessThanOrEqualTo(40),
                "An ordinary finisher should remain a restrained burst.");
            Assert.That(level.player.transform.Find("Foot dust").GetComponent<ParticleSystem>().particleCount,
                Is.GreaterThan(0), "The active lunge needs foot dust as well as spear-tip particles.");
            AssertShortVisibleParticles(wind, 0.65f);
            AssertShortVisibleParticles(sparks, 0.32f);

            float deadline = Time.time + level.balance.attackActive + 0.2f;
            while (attack.CurrentPhase == 2 && Time.time < deadline) yield return null;
            Assert.AreEqual(3, attack.CurrentPhase);
            Assert.That(wind.particleCount + sparks.particleCount, Is.GreaterThan(0),
                "Normal recovery must preserve the burst's brief afterglow.");
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount,
                "The burst must fade away without continuous emission.");
        }

        [UnityTest]
        public IEnumerator PauseFreezesFinisherParticlesUntilResume()
        {
            yield return ReachThirdActive();
            level.Pause();
            yield return null;
            ParticleSystem.Particle[] before = ReadParticles(wind);
            Assert.That(before.Length, Is.GreaterThan(0));
            yield return new WaitForSecondsRealtime(0.2f);
            ParticleSystem.Particle[] after = ReadParticles(wind);
            Assert.AreEqual(before.Length, after.Length);
            for (int i = 0; i < before.Length; i++)
            {
                Assert.That(Vector3.Distance(before[i].position, after[i].position), Is.LessThan(0.001f));
                Assert.That(after[i].remainingLifetime, Is.EqualTo(before[i].remainingLifetime).Within(0.001f));
            }
            level.Resume();
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount);
        }

        [UnityTest]
        public IEnumerator DodgeCancelsFinisherWithoutContinuingItsBurst()
        {
            yield return ReachThirdActive();
            Assert.IsTrue(level.player.TryDodge());
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount,
                "K cancellation must clear the J finisher independently of its own dodge dust.");
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount);
            Assert.IsFalse(attack.IsAttacking);
        }

        [UnityTest]
        public IEnumerator DisabledVfxDuringThirdWindupDoesNotEmitAtActiveFrame()
        {
            attack.RequestAttack();
            bool[] queued = new bool[4];
            float deadline = Time.time + 3f;
            while (Time.time < deadline && !(attack.CurrentComboStep == 3 && attack.CurrentPhase == 1))
            {
                int step = attack.CurrentComboStep;
                if (step > 0 && step < 3 && attack.CurrentPhase == 2 && !queued[step])
                {
                    attack.RequestAttack();
                    queued[step] = true;
                }
                yield return null;
            }
            Assert.AreEqual(3, attack.CurrentComboStep);
            Assert.AreEqual(1, attack.CurrentPhase, "Disable the VFX before the third strike starts emitting.");
            level.player.GetComponent<PlayerAttackVfx>().enabled = false;
            deadline = Time.time + 0.5f;
            while (attack.CurrentPhase == 1 && Time.time < deadline) yield return null;
            Assert.AreEqual(2, attack.CurrentPhase, "Gameplay should still reach its active frame.");
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount,
                "PlayerAttack may call Strike on a disabled component; it must not emit finisher particles.");
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount);
        }

        [UnityTest]
        public IEnumerator DeathClearsFinisherAndPreventsAnotherBurst()
        {
            yield return ReachThirdActive();
            level.player.Health.TakeDamage(100);
            yield return null;
            Assert.AreEqual(LevelDirector.Phase.Defeat, level.CurrentPhase);
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount,
                "A dead player must not keep emitting or displaying the finisher burst.");
            attack.RequestAttack();
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount);
        }

        [UnityTest]
        public IEnumerator FinisherFollowsGivenDirectionAndDoesNotFollowLaterPlayerMovement()
        {
            PlayerAttackVfx effects = level.player.GetComponent<PlayerAttackVfx>();
            foreach (Vector2 direction in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
            {
                wind.Clear();
                sparks.Clear();
                Vector2 origin = level.player.transform.position;
                Vector2 body = origin + Vector2.up * 0.57f;
                effects.Strike(direction, level.balance.attackReach * 1.2f, level.balance.attackActive, 3);
                ParticleSystem.Particle[] particles = ReadParticles(wind);
                Assert.That(particles.Length, Is.GreaterThan(0));
                foreach (ParticleSystem.Particle particle in particles)
                {
                    Assert.That(Vector2.Dot((Vector2)particle.position - body, direction), Is.GreaterThan(0.25f),
                        "The finisher wind belongs ahead of the spear, in its locked attack direction.");
                    Assert.That(Vector2.Dot(particle.velocity, direction), Is.GreaterThan(0f));
                }
                level.player.transform.position += Vector3.right * 0.5f;
                ParticleSystem.Particle[] moved = ReadParticles(wind);
                for (int i = 0; i < particles.Length; i++)
                    Assert.That(Vector3.Distance(particles[i].position, moved[i].position), Is.LessThan(0.001f),
                        "Already emitted particles must remain in world space when the player moves.");
                level.player.transform.position = origin;
            }
            yield return null;
        }

        private IEnumerator ReachThirdActive()
        {
            Assert.AreEqual(0, wind.particleCount + sparks.particleCount);
            attack.RequestAttack();
            bool[] queued = new bool[4];
            float deadline = Time.time + 3f;
            while (Time.time < deadline)
            {
                int step = attack.CurrentComboStep;
                if (step == 3 && attack.CurrentPhase == 2) yield break;
                Assert.AreEqual(0, wind.particleCount + sparks.particleCount,
                    "The independent finisher particles must stay absent in J steps 1/2 and step 3 windup.");
                if (step > 0 && step < 3 && attack.CurrentPhase == 2 && !queued[step])
                {
                    attack.RequestAttack();
                    queued[step] = true;
                }
                yield return null;
            }
            Assert.Fail("Three queued J strikes did not reach the third active frame.");
        }

        private static ParticleSystem.Particle[] ReadParticles(ParticleSystem system)
        {
            var particles = new ParticleSystem.Particle[system.particleCount];
            system.GetParticles(particles);
            return particles;
        }

        private static void AssertShortVisibleParticles(ParticleSystem system, float largestSize)
        {
            foreach (ParticleSystem.Particle particle in ReadParticles(system))
            {
                Assert.That(particle.startLifetime, Is.InRange(0.24f, 0.45f),
                    "The small burst needs enough screen time to read, then a quick fade.");
                Assert.That(particle.startSize, Is.LessThanOrEqualTo(largestSize));
                Assert.That(particle.startColor.a, Is.GreaterThanOrEqualTo(180));
            }
        }
    }
}
