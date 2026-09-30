using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Changban.Tests
{
    public sealed class ArenaCombatPlayTests
    {
        [UnityTest]
        public IEnumerator PlayerIsClampedAndGroundCoversBothCameraExtremes()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            Assert.NotNull(level.balance);
            Assert.NotNull(level.player);
            GameObject groundObject = GameObject.Find("Continuous Ground");
            Assert.NotNull(groundObject, "The arena needs one continuous background behind the roads.");
            SpriteRenderer ground = groundObject.GetComponent<SpriteRenderer>();
            Assert.NotNull(ground);
            Assert.IsTrue(ground.enabled);
            foreach (string edge in new[] { "North", "South", "West", "East" })
            {
                GameObject fence = GameObject.Find("Palisade " + edge + " 00");
                Assert.NotNull(fence, edge + " border needs visible palisade art.");
                SpriteRenderer graphic = fence.GetComponent<SpriteRenderer>();
                Assert.NotNull(graphic.sprite);
                Assert.IsTrue(graphic.enabled && graphic.color.a > 0.9f);
            }
            Camera camera = Camera.main;
            Assert.NotNull(camera);

            level.Begin();
            Vector2[] outsideCorners =
            {
                new Vector2(level.balance.playableMin.x - 3f, level.balance.playableMin.y - 3f),
                new Vector2(level.balance.playableMax.x + 3f, level.balance.playableMax.y + 3f)
            };
            Vector2[] arenaCorners = { level.balance.playableMin, level.balance.playableMax };
            Rigidbody2D body = level.player.GetComponent<Rigidbody2D>();
            Assert.NotNull(body);
            for (int i = 0; i < outsideCorners.Length; i++)
            {
                // The motor also respects the current camera view. Put the camera near
                // this end of the arena before calculating the expected clamp point.
                camera.transform.position = new Vector3(arenaCorners[i].x, arenaCorners[i].y,
                    camera.transform.position.z);
                Vector2 clamped = level.player.ClampToPlayArea(outsideCorners[i]);
                AssertInsideArena(clamped, level.balance);
                body.position = outsideCorners[i];
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null; // The previous frame's CameraFollow.LateUpdate has completed.
                AssertInsideArena(body.position, level.balance);
                Vector3 viewport = camera.WorldToViewportPoint(level.player.transform.position);
                Assert.That(viewport.x, Is.InRange(0.02f, 0.98f));
                Assert.That(viewport.y, Is.InRange(0.02f, 0.98f));
                AssertGroundCoversCamera(camera, ground);
            }
        }

        [UnityTest]
        public IEnumerator ForwardThrustHitsOneEnemyOnceAndLeavesSideEnemyUntouched()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            level.Begin();
            DisableExistingEnemies();
            Vector3 origin = level.player.transform.position;
            EnemyBrain front = Object.Instantiate(level.meleePrefab, origin + Vector3.right * 1.25f, Quaternion.identity);
            EnemyBrain side = Object.Instantiate(level.meleePrefab, origin + Vector3.up * 1.25f, Quaternion.identity);
            yield return null; // Health is initialized in EnemyBrain.Start.
            front.enabled = false;
            side.enabled = false;
            int frontHealth = front.Health.Current;
            int sideHealth = side.Health.Current;
            PlayerAttack attack = level.player.GetComponent<PlayerAttack>();
            Assert.NotNull(attack);
            ParticleSystem sparks = level.player.transform.Find("Spear sparks")?.GetComponent<ParticleSystem>();
            SpriteRenderer oldTrail = level.player.transform.Find("Spear Trail")?.GetComponent<SpriteRenderer>();
            Assert.NotNull(sparks);
            Assert.NotNull(oldTrail);

            attack.RequestAttack();
            bool sawParticles = false;
            bool sawImpact = false;
            bool capturedAttack = false;
            float impactAt = -1f;
            float deadline = Time.time + level.balance.attackWindup + level.balance.attackActive +
                             level.balance.attackRecovery + 0.15f;
            while (Time.time < deadline)
            {
                sawParticles |= attack.CurrentPhase == 2 && sparks.particleCount > 0;
                sawImpact |= front.Health.Current < frontHealth && sparks.particleCount >= 8;
                if (sawImpact && impactAt < 0f) impactAt = Time.time;
                if (sawImpact && !capturedAttack && Time.time >= impactAt + 0.03f)
                {
                    CaptureGameFrame("OrdinaryAttackParticles.png");
                    capturedAttack = true;
                }
                yield return null;
            }
            Assert.IsTrue(sawParticles, "The thrust needs real particles during its damage window.");
            Assert.IsTrue(sawImpact, "Enemy contact should add an impact particle burst.");
            Assert.IsFalse(oldTrail.enabled, "The old light-bar trail must stay hidden.");
            Assert.AreEqual(frontHealth - level.balance.attackDamage, front.Health.Current,
                "A single thrust should damage the front target exactly once.");
            Assert.AreEqual(sideHealth, side.Health.Current,
                "An enemy to the side should remain outside the narrow forward thrust.");
            Object.Destroy(front.gameObject);
            Object.Destroy(side.gameObject);
        }

        [UnityTest]
        public IEnumerator DodgePassesThroughEnemyWithoutPushingOrDamagingIt()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            level.Begin();
            DisableExistingEnemies();

            PlayerMotor motor = level.player;
            Rigidbody2D playerBody = motor.GetComponent<Rigidbody2D>();
            Collider2D playerCollider = motor.GetComponent<Collider2D>();
            Assert.NotNull(playerCollider);
            Assert.IsFalse(playerCollider.isTrigger, "Ordinary player movement needs a solid collider.");
            Vector2 origin = playerBody.position;
            EnemyBrain enemy = Object.Instantiate(level.meleePrefab,
                origin + Vector2.right * 1.0f, Quaternion.identity);
            yield return null; // EnemyBrain.Start initializes health.
            enemy.enabled = false;
            Rigidbody2D enemyBody = enemy.GetComponent<Rigidbody2D>();
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            Vector2 enemyOrigin = enemyBody.position;
            int enemyHealth = enemy.Health.Current;

            Assert.IsTrue(motor.TryDodge(), "K should start an evasive move.");
            Assert.IsFalse(playerCollider.isTrigger, "Dodge must keep solid obstacle collision.");
            Assert.IsTrue(Physics2D.GetIgnoreCollision(playerCollider, enemyCollider),
                "Dodge should temporarily ignore enemy body collision.");
            Assert.IsFalse(motor.TryDodge(), "A dodge cannot be restarted during its cooldown.");
            ParticleSystem dust = motor.transform.Find("Foot dust")?.GetComponent<ParticleSystem>();
            Assert.NotNull(dust);
            bool sawDust = dust.particleCount > 0;
            float deadline = Time.time + level.balance.dodgeDuration + 0.45f;
            while (Time.time < deadline && (motor.IsDodging ||
                Physics2D.GetIgnoreCollision(playerCollider, enemyCollider)))
            {
                sawDust |= dust.particleCount > 0;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsFalse(motor.IsDodging);
            Assert.IsFalse(playerCollider.isTrigger, "Static obstacles should remain solid throughout K.");
            Assert.IsFalse(Physics2D.GetIgnoreCollision(playerCollider, enemyCollider),
                "Enemy contact should be restored after the player leaves it.");
            Assert.That(Vector2.Distance(playerBody.position, origin), Is.GreaterThan(1.5f));
            Assert.That(Vector2.Distance(enemyBody.position, enemyOrigin), Is.LessThan(0.08f),
                "K is a dodge, so it must not physically launch the enemy.");
            Assert.AreEqual(enemyHealth, enemy.Health.Current, "K must not deal attack damage.");
            Assert.IsTrue(sawDust, "K should leave a visible dust particle cue.");
            Object.Destroy(enemy.gameObject);
        }

        [UnityTest]
        public IEnumerator ThreeQueuedAttacksUseDistinctStatesAndDamageOncePerStep()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            level.Begin();
            DisableExistingEnemies();

            EnemyBrain target = Object.Instantiate(level.elitePrefab,
                level.player.transform.position + Vector3.right * 0.95f, Quaternion.identity);
            yield return null;
            target.enabled = false;
            // The target remains in the hit query, but does not physically steer the player
            // while successive thrusts take their small forward steps.
            target.GetComponent<Collider2D>().isTrigger = true;
            int startingHealth = target.Health.Current;
            PlayerAttack attack = level.player.GetComponent<PlayerAttack>();
            PlayerVisualAnimator visual = level.player.GetComponent<PlayerVisualAnimator>();
            Assert.NotNull(attack);
            Assert.NotNull(visual);
            Assert.NotNull(visual.Target);
            Assert.NotNull(visual.CharacterRenderer);

            attack.RequestAttack();
            bool[] sawStrike = new bool[4];
            bool[] sawActive = new bool[4];
            bool[] queuedNext = new bool[4];
            bool[] capturedPose = new bool[4];
            string[] activeSprite = new string[4];
            int[] healthEntering = { -1, -1, -1, -1 };
            float fullCombo = (level.balance.attackWindup + level.balance.attackActive +
                               level.balance.attackRecovery) * 3f + 0.7f;
            float deadline = Time.time + fullCombo;
            while (Time.time < deadline)
            {
                int step = attack.CurrentComboStep;
                if (step >= 1 && step <= 3)
                {
                    if (healthEntering[step] < 0) healthEntering[step] = target.Health.Current;
                    string expectedState = step == 1 ? "Attack" : "Attack" + step;
                    sawStrike[step] |= visual.Target.GetCurrentAnimatorStateInfo(0).IsName(expectedState);
                    if (attack.CurrentPhase == 2)
                    {
                        sawActive[step] = true;
                        if (visual.CharacterRenderer.sprite != null)
                            activeSprite[step] = visual.CharacterRenderer.sprite.name;
                        if (step >= 2 && !capturedPose[step] && activeSprite[step] != null &&
                            activeSprite[step].Contains(step == 2 ? "Combo2_Sweep" : "Combo3_Lunge"))
                        {
                            CaptureCloseupFrame("Combo" + step + "AttackCloseup.png", level.player.transform.position);
                            capturedPose[step] = true;
                        }
                        if (step < 3 && !queuedNext[step])
                        {
                            attack.RequestAttack();
                            queuedNext[step] = true;
                        }
                    }
                }
                if (sawActive[3] && !attack.IsAttacking) break;
                yield return null;
            }

            for (int step = 1; step <= 3; step++)
            {
                Assert.IsTrue(sawStrike[step], "Combo step " + step + " needs its own attack state.");
                Assert.IsTrue(sawActive[step], "Combo step " + step + " must reach the hit window.");
                Assert.IsFalse(string.IsNullOrEmpty(activeSprite[step]),
                    "Combo step " + step + " needs a visible pose.");
                if (step >= 2) Assert.IsTrue(capturedPose[step],
                    "Combo step " + step + " needs a visible strike frame for screenshot review.");
            }
            Assert.AreNotEqual(activeSprite[1], activeSprite[2], "The second attack needs distinct pose art.");
            Assert.AreNotEqual(activeSprite[2], activeSprite[3], "The third attack needs distinct pose art.");
            Assert.AreEqual(startingHealth - level.balance.attackDamage, healthEntering[2],
                "The first attack should hit once before the second begins.");
            Assert.AreEqual(startingHealth - level.balance.attackDamage * 2, healthEntering[3],
                "The second attack should hit once before the third begins.");
            Assert.AreEqual(startingHealth - level.balance.attackDamage * 3 - 1, target.Health.Current,
                "Each strike should hit this target once; the third strike adds one damage.");
            Assert.IsFalse(attack.IsAttacking);
            Object.Destroy(target.gameObject);
        }

        [UnityTest]
        public IEnumerator PlayerHitFlashesAndEmitsImpactParticles()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            level.Begin();
            DisableExistingEnemies();

            PlayerMotor motor = level.player;
            PlayerVisualAnimator visual = motor.GetComponent<PlayerVisualAnimator>();
            Assert.NotNull(visual);
            SpriteRenderer renderer = visual.CharacterRenderer;
            Assert.NotNull(renderer);
            Color before = renderer.color;
            int health = motor.Health.Current;
            ParticleSystem sparks = motor.transform.Find("Spear sparks")?.GetComponent<ParticleSystem>();
            ParticleSystem ring = motor.transform.Find("Skill shock ring")?.GetComponent<ParticleSystem>();
            Assert.NotNull(sparks);
            Assert.NotNull(ring);

            Assert.IsTrue(motor.ReceiveHit(1, (Vector2)motor.transform.position + Vector2.right));
            yield return null; // PlayerVisualAnimator applies tint in LateUpdate.
            Assert.AreEqual(health - 1, motor.Health.Current);
            Assert.That(renderer.color.g, Is.LessThan(before.g - 0.04f),
                "Damage should briefly tint the character red.");
            Assert.That(sparks.particleCount, Is.GreaterThan(0), "The hit needs impact sparks.");
            Assert.That(ring.particleCount, Is.GreaterThan(0), "The hit needs a short shock ring.");
            Assert.IsFalse(motor.ReceiveHit(1, (Vector2)motor.transform.position + Vector2.right),
                "The brief hit immunity should prevent a duplicate hit.");
            CaptureCloseupFrame("PlayerHurtParticles.png", motor.transform.position);
        }

        [UnityTest]
        public IEnumerator NorthAndSouthWalkClipsRenderDifferentFrames()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            level.Begin();
            DisableExistingEnemies();
            PlayerVisualAnimator visual = level.player.GetComponent<PlayerVisualAnimator>();
            Assert.NotNull(visual);
            visual.enabled = false; // Sample the clips without the idle input state replacing them.
            Animator animator = visual.Target;
            SpriteRenderer renderer = visual.CharacterRenderer;
            Assert.NotNull(visual.NorthWalkMaterial);
            Assert.NotNull(visual.SouthWalkMaterial);

            renderer.sharedMaterial = visual.NorthWalkMaterial;
            animator.Play("WalkNorth", 0, 0f);
            animator.Update(0.16f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("WalkNorth"));
            Assert.That(renderer.sprite.name, Does.StartWith("ZY_N_WalkV3_"));
            CaptureCloseupFrame("WalkNorthCloseup.png", level.player.transform.position);

            renderer.sharedMaterial = visual.SouthWalkMaterial;
            animator.Play("WalkSouth", 0, 0f);
            animator.Update(0.16f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("WalkSouth"));
            Assert.That(renderer.sprite.name, Does.StartWith("ZY_S_WalkV3_"));
            CaptureCloseupFrame("WalkSouthCloseup.png", level.player.transform.position);
        }

        [UnityTest]
        public IEnumerator LungeHitsTargetsAlongItsPathWithoutDamagingSideOrRear()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindAnyObjectByType<LevelDirector>();
            level.Begin();
            DisableExistingEnemies();
            Vector2 origin = level.player.transform.position;
            EnemyBrain front = Object.Instantiate(level.meleePrefab, origin + Vector2.right * 1.8f, Quaternion.identity);
            EnemyBrain side = Object.Instantiate(level.meleePrefab, origin + Vector2.up * 1.2f, Quaternion.identity);
            EnemyBrain rear = Object.Instantiate(level.meleePrefab, origin + Vector2.left * 0.8f, Quaternion.identity);
            yield return null;
            front.enabled = false;
            side.enabled = false;
            rear.enabled = false;
            int frontHealth = front.Health.Current;
            int sideHealth = side.Health.Current;
            int rearHealth = rear.Health.Current;
            Vector2 destination = origin + Vector2.right * 2.5f;
            level.player.GetComponent<Rigidbody2D>().position = destination;
            level.player.GetComponent<PlayerAttack>().SkillStrike(origin, destination);
            Assert.AreEqual(frontHealth - level.balance.attackDamage - 1, front.Health.Current);
            Assert.AreEqual(sideHealth, side.Health.Current);
            Assert.AreEqual(rearHealth, rear.Health.Current);
            Object.Destroy(front.gameObject);
            Object.Destroy(side.gameObject);
            Object.Destroy(rear.gameObject);
        }

        [UnityTest]
        public IEnumerator BreakthroughTravelsVisiblyWithSkillParticlesAndCooldown()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            level.Begin();
            DisableExistingEnemies();
            PlayerMotor motor = level.player;
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            Vector2 origin = body.position;
            EnemyBrain front = Object.Instantiate(level.meleePrefab,
                origin + Vector2.right * 1.8f, Quaternion.identity);
            yield return null;
            front.enabled = false;
            int health = front.Health.Current;
            ParticleSystem wind = motor.transform.Find("Skill wind")?.GetComponent<ParticleSystem>();
            ParticleSystem ring = motor.transform.Find("Skill shock ring")?.GetComponent<ParticleSystem>();
            Assert.NotNull(wind);
            Assert.NotNull(ring);
            Assert.IsTrue(motor.TryUseSkill());
            Assert.That(Vector2.Distance(body.position, origin), Is.LessThan(0.1f),
                "The skill should begin a dash instead of teleporting instantly.");
            Assert.IsFalse(motor.TryUseSkill(), "The skill cannot be used twice during cooldown.");
            bool sawIntermediate = false;
            bool sawWind = false;
            bool sawImpact = false;
            bool capturedImpact = false;
            float impactAt = -1f;
            float deadline = Time.time + 0.50f;
            while (Time.time < deadline)
            {
                float distance = Vector2.Distance(body.position, origin);
                sawIntermediate |= distance > 0.25f && distance < level.balance.skillDistance - 0.25f;
                sawWind |= wind.particleCount > 0;
                if (front.Health.Current < health && ring.particleCount > 0)
                {
                    sawImpact = true;
                    if (impactAt < 0f) impactAt = Time.time;
                }
                if (sawImpact && !capturedImpact && Time.time >= impactAt + 0.07f)
                {
                    CaptureGameFrame("BreakthroughParticles.png");
                    capturedImpact = true;
                }
                yield return null;
            }
            Assert.IsTrue(sawIntermediate, "Breakthrough should have visible in-between positions.");
            Assert.IsTrue(sawWind, "Breakthrough should leave distinct wind particles.");
            Assert.IsTrue(sawImpact, "A struck enemy should show the skill impact ring.");
            Assert.AreEqual(health - level.balance.attackDamage - 1, front.Health.Current);
            Assert.That(motor.SkillReadyIn, Is.GreaterThan(4f));
            Object.Destroy(front.gameObject);
        }

        [UnityTest]
        public IEnumerator EveryEnemyRoleShowsWarningAndAttackEffects()
        {
            SceneManager.LoadScene("ChangbanDemo");
            yield return null;
            LevelDirector level = Object.FindFirstObjectByType<LevelDirector>();
            Assert.NotNull(level);
            level.Begin();
            DisableExistingEnemies();

            EnemyBrain[] prefabs = { level.meleePrefab, level.archerPrefab, level.elitePrefab };
            string[] strikeNames =
            {
                "Enemy_Melee_Strike_SE_v02",
                "Enemy_Archer_Release_SE_v02",
                "Enemy_Elite_Strike_SE_v02"
            };
            for (int i = 0; i < prefabs.Length; i++)
            {
                Assert.NotNull(prefabs[i]);
                bool archer = prefabs[i].role == EnemyBrain.Role.Archer;
                float distance = archer ? 4.1f : 0.72f;
                EnemyBrain enemy = Object.Instantiate(prefabs[i],
                    level.player.transform.position + Vector3.right * distance, Quaternion.identity);
                EnemyVisual visual = enemy.GetComponentInChildren<EnemyVisual>();
                Assert.NotNull(visual);
                SpriteRenderer graphic = visual.GetComponent<SpriteRenderer>();
                bool sawWarning = false;
                bool sawWarningParticles = false;
                bool sawStrike = false;
                bool sawStroke = false;
                bool sawArrow = false;
                bool capturedEnemy = false;
                bool capturedWarning = false;
                float deadline = Time.time + level.balance.enemyWindup + 0.65f;
                while (Time.time < deadline)
                {
                    SpriteRenderer directionLine = enemy.transform.Find("Attack Direction")?.GetComponent<SpriteRenderer>();
                    SpriteRenderer warningDisc = enemy.transform.Find("Warning")?.GetComponent<SpriteRenderer>();
                    ParticleSystem warningParticles = enemy.transform.Find("Enemy Warning Particles")?.GetComponent<ParticleSystem>();
                    ParticleSystem stroke = enemy.transform.Find(archer ? "Enemy Strike Dust" : "Enemy Strike Particles")?.GetComponent<ParticleSystem>();
                    sawWarning |= enemy.CurrentState == EnemyBrain.State.Warning;
                    sawWarningParticles |= enemy.CurrentState == EnemyBrain.State.Warning &&
                                           warningParticles != null && warningParticles.particleCount > 0;
                    if (i == 0 && sawWarningParticles && !capturedWarning)
                    {
                        CaptureCloseupFrame("EnemyWarningParticles.png", enemy.transform.position);
                        capturedWarning = true;
                    }
                    Assert.IsTrue(directionLine == null || !directionLine.enabled,
                        "An enemy warning must not draw the old light column.");
                    Assert.IsTrue(warningDisc == null || !warningDisc.enabled,
                        "The old glowing warning decal must stay hidden.");
                    sawStrike |= graphic.sprite != null && graphic.sprite.name == strikeNames[i];
                    sawStroke |= stroke != null && stroke.particleCount > 0;
                    if (i == 0 && sawStroke && !capturedEnemy)
                    {
                        CaptureCloseupFrame("EnemyAttackParticles.png", enemy.transform.position);
                        capturedEnemy = true;
                    }
                    ArrowProjectile arrow = Object.FindFirstObjectByType<ArrowProjectile>();
                    SpriteRenderer arrowGraphic = arrow != null ? arrow.GetComponent<SpriteRenderer>() : null;
                    SpriteRenderer arrowTrail = arrow != null ? arrow.transform.Find("Arrow Trail")?.GetComponent<SpriteRenderer>() : null;
                    ParticleSystem arrowParticles = arrow != null ?
                        arrow.transform.Find("Arrow Particles")?.GetComponent<ParticleSystem>() : null;
                    Assert.IsTrue(arrowTrail == null || !arrowTrail.enabled,
                        "The arrow must not have a SpriteRenderer light tail.");
                    sawArrow |= arrowGraphic != null && arrowGraphic.sprite != null && arrowGraphic.enabled &&
                                arrowParticles != null && arrowParticles.particleCount > 0;
                    if (sawWarning && sawWarningParticles && sawStrike && sawStroke &&
                        (!archer || sawArrow) && (i != 0 || capturedEnemy)) break;
                    yield return null;
                }
                Assert.IsTrue(sawWarning, prefabs[i].role + " should telegraph its attack.");
                Assert.IsTrue(sawWarningParticles, prefabs[i].role + " should display warning particles.");
                Assert.IsTrue(sawStrike, prefabs[i].role + " should use its new strike/release pose.");
                Assert.IsTrue(sawStroke, prefabs[i].role + " should emit attack particles.");
                if (i == 0) Assert.IsTrue(capturedEnemy, "The melee particle frame should be captured.");
                Assert.IsNull(enemy.transform.Find("Attack Stroke"),
                    "Enemy attacks should no longer use a stretched light bar.");
                if (archer) Assert.IsTrue(sawArrow, "The archer release should create a visible projectile.");
                Object.Destroy(enemy.gameObject);
                foreach (ArrowProjectile arrow in Object.FindObjectsByType<ArrowProjectile>(FindObjectsSortMode.None))
                    Object.Destroy(arrow.gameObject);
                yield return null;
            }
        }

        private static void DisableExistingEnemies()
        {
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                enemy.enabled = false;
        }

        private static void CaptureGameFrame(string filename)
        {
            Camera camera = Camera.main;
            Assert.NotNull(camera);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Screenshots"));
            Directory.CreateDirectory(folder);
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            RenderTexture oldTarget = camera.targetTexture;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(folder, filename), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
            }
        }

        private static void CaptureCloseupFrame(string filename, Vector2 focus)
        {
            Camera camera = Camera.main;
            Assert.NotNull(camera);
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            try
            {
                camera.transform.position = new Vector3(focus.x, focus.y + 0.45f, oldPosition.z);
                camera.orthographicSize = 2.75f;
                CaptureGameFrame(filename);
            }
            finally
            {
                camera.transform.position = oldPosition;
                camera.orthographicSize = oldSize;
            }
        }

        private static void AssertInsideArena(Vector2 position, GameBalance balance)
        {
            const float tolerance = 0.03f;
            Assert.That(position.x, Is.InRange(balance.playableMin.x - tolerance, balance.playableMax.x + tolerance),
                "The player should stay within the horizontal arena bounds.");
            Assert.That(position.y, Is.InRange(balance.playableMin.y - tolerance, balance.playableMax.y + tolerance),
                "The player should stay within the vertical arena bounds.");
        }

        private static void AssertGroundCoversCamera(Camera camera, SpriteRenderer ground)
        {
            Bounds bounds = ground.bounds;
            float distance = Mathf.Abs(camera.transform.position.z - ground.transform.position.z);
            foreach (float x in new[] { 0f, 1f })
            foreach (float y in new[] { 0f, 1f })
            {
                Vector3 corner = camera.ViewportToWorldPoint(new Vector3(x, y, distance));
                Assert.That(corner.x, Is.InRange(bounds.min.x - 0.02f, bounds.max.x + 0.02f));
                Assert.That(corner.y, Is.InRange(bounds.min.y - 0.02f, bounds.max.y + 0.02f));
            }
        }
    }
}
