using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Changban.Tests
{
    public sealed class VisualPlayTests
    {
        [UnityTest]
        public IEnumerator NorthAndSouthThrustUseDirectionSpecificFrames()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindAnyObjectByType<LevelDirector>();
            level.Begin();
            PlayerVisualAnimator visual = level.player.GetComponent<PlayerVisualAnimator>();
            Animator animator = level.player.GetComponentInChildren<Animator>();
            SpriteRenderer graphic = animator.GetComponent<SpriteRenderer>();
            float duration = level.balance.attackWindup + level.balance.attackActive + level.balance.attackRecovery;

            visual.PlayAttack(duration, Vector2.up);
            yield return new WaitForSeconds(0.20f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("AttackNorth"));
            Assert.AreEqual("ZY_N_AttackV2_Extend", graphic.sprite.name);
            Assert.AreEqual(visual.NorthAttackMaterial, graphic.sharedMaterial);

            visual.PlayAttack(duration, Vector2.down);
            yield return new WaitForSeconds(0.20f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("AttackSouth"));
            Assert.AreEqual("ZY_S_AttackV2_Extend", graphic.sprite.name);
            Assert.AreEqual(visual.SouthAttackMaterial, graphic.sharedMaterial);
        }

        [UnityTest]
        public IEnumerator CharacterClipsAndEnemyActionPoseRunInTheDemoScene()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindAnyObjectByType<LevelDirector>();
            Assert.NotNull(level);
            PlayerVisualAnimator visual = level.player.GetComponent<PlayerVisualAnimator>();
            Animator animator = level.player.GetComponentInChildren<Animator>();
            Assert.NotNull(visual);
            Assert.NotNull(animator);
            Assert.NotNull(animator.runtimeAnimatorController);
            level.Begin();
            visual.PlayAttack(level.balance.attackWindup + level.balance.attackActive + level.balance.attackRecovery);
            SpriteRenderer playerSprite = animator.GetComponent<SpriteRenderer>();
            bool sawExtend = false;
            float clipDeadline = Time.time + level.balance.attackWindup + level.balance.attackActive +
                                 level.balance.attackRecovery;
            while (Time.time < clipDeadline)
            {
                sawExtend |= animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") &&
                             playerSprite.sprite != null && playerSprite.sprite.name == "ZY_SE_AttackV2_Extend";
                yield return null;
            }
            Assert.IsTrue(sawExtend, "The player Attack clip should show its forward, level spear thrust.");

            EnemyBrain enemy = Object.Instantiate(level.meleePrefab,
                level.player.transform.position + Vector3.right * 0.72f, Quaternion.identity);
            yield return new WaitForSeconds(0.10f);
            EnemyVisual enemyVisual = enemy.GetComponentInChildren<EnemyVisual>();
            Assert.NotNull(enemyVisual);
            Assert.AreEqual(EnemyBrain.State.Warning, enemy.CurrentState);
            Assert.AreEqual("Enemy_Melee_Attack_SE", enemyVisual.GetComponent<SpriteRenderer>().sprite.name);
            Object.Destroy(enemy.gameObject);
        }
    }
}
