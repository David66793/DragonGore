using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Changban.Tests
{
    public sealed class ArrowHitFeedbackTests
    {
        private LevelDirector level;
        private SpriteRenderer playerGraphic;
        private ParticleSystem playerSparks;
        private ParticleSystem playerRing;
        private readonly List<GameObject> spawned = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            level.Begin();
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>()) enemy.gameObject.SetActive(false);
            level.player.enabled = false;
            level.player.GetComponent<Collider2D>().enabled = false;
            level.player.GetComponent<Rigidbody2D>().position = Vector2.zero;
            level.player.Health.SetMaximum(100);
            playerGraphic = level.player.GetComponent<PlayerVisualAnimator>().CharacterRenderer;
            playerSparks = level.player.transform.Find("Spear sparks").GetComponent<ParticleSystem>();
            playerRing = level.player.transform.Find("Skill shock ring").GetComponent<ParticleSystem>();
            Assert.NotNull(playerGraphic);
            Assert.AreEqual(0, playerSparks.particleCount + playerRing.particleCount);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            foreach (GameObject item in spawned) if (item != null) Object.Destroy(item);
            spawned.Clear();
            foreach (ArrowProjectile arrow in Object.FindObjectsByType<ArrowProjectile>()) Object.Destroy(arrow.gameObject);
            yield return null;
        }

        // The release must remain visible, but it must not borrow the bright
        // star-shaped particles used to communicate a successful damage impact.
        [UnityTest]
        public IEnumerator ArcherReleaseAndMissKeepBowDustAndTrailWithoutHitSparksOrRedFlash()
        {
            EnemyBrain archer = Object.Instantiate(level.archerPrefab, Vector2.right * 4.1f, Quaternion.identity);
            spawned.Add(archer.gameObject);
            archer.GetComponent<Collider2D>().enabled = false;
            float deadline = Time.time + 2f;
            while (archer.CurrentState != EnemyBrain.State.Warning && Time.time < deadline) yield return null;
            Assert.AreEqual(EnemyBrain.State.Warning, archer.CurrentState);
            // Leave the locked firing lane before release, so the real projectile
            // will pass the original position instead of hitting the player.
            level.player.GetComponent<Rigidbody2D>().position = Vector2.up * 1.5f;
            int health = level.player.Health.Current;
            Color restingColor = playerGraphic.color;
            ParticleSystem releaseSparks = archer.transform.Find("Enemy Strike Particles").GetComponent<ParticleSystem>();
            ParticleSystem releaseDust = archer.transform.Find("Enemy Strike Dust").GetComponent<ParticleSystem>();
            bool sawBowDust = false;
            bool sawTrail = false;
            bool sawReleaseSparks = false;
            ArrowProjectile shot = null;
            deadline = Time.time + 2f;
            while (Time.time < deadline)
            {
                sawBowDust |= releaseDust.particleCount > 0;
                sawReleaseSparks |= releaseSparks.particleCount > 0;
                if (shot == null) shot = Object.FindFirstObjectByType<ArrowProjectile>();
                if (shot != null)
                {
                    archer.enabled = false; // Observe one release, without a second attack cycle.
                    ParticleSystem trail = shot.transform.Find("Arrow Particles").GetComponent<ParticleSystem>();
                    sawTrail |= trail.particleCount > 0;
                    if (shot.transform.position.x < -0.5f) break;
                }
                AssertNoPlayerHitFeedback(health, restingColor);
                yield return null;
            }
            Assert.NotNull(shot, "A real archer release must create a visible projectile.");
            Assert.That(shot.transform.position.x, Is.LessThan(-0.5f), "The projectile must actually pass the missed target.");
            Assert.IsTrue(sawBowDust, "A weak bow-mouth dust cue should still communicate release.");
            Assert.IsTrue(sawTrail, "The flying arrow should retain its subtle trail.");
            Assert.IsFalse(sawReleaseSparks, "Shooting or missing must not emit the star-shaped hit-impact burst.");
            AssertNoPlayerHitFeedback(health, restingColor);
        }

        [UnityTest]
        public IEnumerator RealArrowHitDamagesOnceAndEmitsImpactAndPlayerHurtFeedback()
        {
            int health = level.player.Health.Current;
            Color restingColor = playerGraphic.color;
            ArrowProjectile arrow = SpawnArrow(Vector2.left * 0.8f + Vector2.up * 0.52f);
            float deadline = Time.time + 0.5f;
            while (level.player.Health.Current == health && Time.time < deadline) yield return null;
            yield return null; // Let the player's LateUpdate apply the accepted-hit tint.
            Assert.AreEqual(health - level.balance.enemyDamage, level.player.Health.Current);
            Assert.IsTrue(arrow == null, "The projectile must be consumed on collision.");
            GameObject impact = GameObject.Find("Arrow Impact Particles");
            Assert.NotNull(impact, "An accepted arrow hit needs a projectile impact burst.");
            Assert.That(impact.GetComponent<ParticleSystem>().particleCount, Is.GreaterThan(0));
            Assert.That(playerSparks.particleCount, Is.GreaterThan(0), "Accepted damage needs the player's warm hurt sparks.");
            Assert.That(playerRing.particleCount, Is.GreaterThan(0));
            Assert.That(playerGraphic.color.g, Is.LessThan(restingColor.g - 0.03f), "Accepted damage needs a brief red tint.");
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(health - level.balance.enemyDamage, level.player.Health.Current, "One arrow must damage once.");
        }

        [UnityTest]
        public IEnumerator ArrowCrossingInvulnerablePlayerDoesNotEmitHitFeedback()
        {
            level.player.enabled = true;
            Assert.IsTrue(level.player.TryDodge(), "Use the real dodge protection rather than a fake damage receiver.");
            level.player.enabled = false; // Keep the target fixed while retaining real dodge immunity.
            int health = level.player.Health.Current;
            Color restingColor = playerGraphic.color;
            ArrowProjectile arrow = SpawnArrow(Vector2.left * 0.10f + Vector2.up * 0.52f);
            float deadline = Time.time + 0.20f;
            while (arrow != null && Time.time < deadline)
            {
                AssertNoPlayerHitFeedback(health, restingColor);
                yield return null;
            }
            yield return null;
            Assert.IsTrue(arrow == null, "The projectile must actually cross the player's hit radius and be consumed.");
            AssertNoPlayerHitFeedback(health, restingColor);
        }

        private ArrowProjectile SpawnArrow(Vector2 position)
        {
            Assert.NotNull(level.archerPrefab.arrowPrefab);
            ArrowProjectile arrow = Object.Instantiate(level.archerPrefab.arrowPrefab, position, Quaternion.identity);
            arrow.Initialize(Vector2.right, level.balance.arrowSpeed, level.balance.enemyDamage, level.player);
            spawned.Add(arrow.gameObject);
            return arrow;
        }

        private void AssertNoPlayerHitFeedback(int health, Color restingColor)
        {
            Assert.AreEqual(health, level.player.Health.Current);
            Assert.IsNull(GameObject.Find("Arrow Impact Particles"), "A rejected or missed arrow must not spawn an impact.");
            Assert.AreEqual(0, playerSparks.particleCount + playerRing.particleCount,
                "No accepted damage means no player's hurt sparks or ring.");
            Assert.That(playerGraphic.color.g, Is.GreaterThanOrEqualTo(restingColor.g - 0.02f),
                "No accepted damage means no red hurt flash.");
        }
    }
}
