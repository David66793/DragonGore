using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Changban.Tests
{
    public sealed class CombatAndLevelTests
    {
        [Test]
        public void EachStrikeCanHitATargetOnlyOnce()
        {
            var enemy = new object();
            var firstStrike = new HitRegistry<object>();
            Assert.IsTrue(firstStrike.TryMark(enemy));
            Assert.IsFalse(firstStrike.TryMark(enemy));
            Assert.IsTrue(new HitRegistry<object>().TryMark(enemy), "A new strike may hit again.");
        }

        [Test]
        public void EscapeNeedsRescueEliteDeathExitAndSurvival()
        {
            Assert.IsFalse(LevelRules.CanEscape(false, true, true, true));
            Assert.IsFalse(LevelRules.CanEscape(true, false, true, true));
            Assert.IsFalse(LevelRules.CanEscape(true, true, false, true));
            Assert.IsFalse(LevelRules.CanEscape(true, true, true, false));
            Assert.IsTrue(LevelRules.CanEscape(true, true, true, true));
        }

        [Test]
        public void TwoMovementKeysKeepDiagonalSpeedAtOne()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                InputSystem.Update();
                Assert.AreEqual(1f, GameInput.Move.magnitude, 0.001f);
                Assert.Greater(GameInput.Move.x, 0f);
                Assert.Greater(GameInput.Move.y, 0f);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }
    }
}
