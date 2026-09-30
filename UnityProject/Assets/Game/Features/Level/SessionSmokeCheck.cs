using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Changban
{
    // Opt-in verification for the built player; never runs in normal games or the Editor.
    public sealed class SessionSmokeCheck : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {
            if (Application.isEditor ||
                Array.IndexOf(Environment.GetCommandLineArgs(), "-verify-session-controls") < 0) return;
            // Hidden QA windows have no focus; only this explicitly requested check runs in the background.
            Application.runInBackground = true;
            var host = new GameObject("Session smoke verification");
            DontDestroyOnLoad(host);
            host.AddComponent<SessionSmokeCheck>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            LevelDirector level = LevelDirector.Instance;
            Require(level != null && level.CurrentPhase == LevelDirector.Phase.Ready, "initial Ready scene");
            Require(Object.FindObjectsByType<SkillHud>().Length == 1, "one skill bar");
            level.Begin();
            foreach (EnemyBrain enemy in Object.FindObjectsByType<EnemyBrain>()) enemy.enabled = false;
            SkillHud hud = Object.FindAnyObjectByType<SkillHud>();
            Require(level.player.TryDodge() && hud.FeedbackVersion(1) == 1, "accepted dodge feedback");
            Require(!level.player.TryDodge() && hud.FeedbackVersion(1) == 1, "blocked dodge has no feedback");
            Require(level.player.TryUseSkill() && hud.FeedbackVersion(2) == 1, "accepted thrust feedback");
            Require(!level.player.TryUseSkill() && hud.FeedbackVersion(2) == 1, "blocked thrust has no feedback");
            yield return new WaitForSeconds(0.24f);
            yield return new WaitForSeconds(0.25f);
            level.player.GetComponent<PlayerAttack>().RequestAttack();
            Require(hud.FeedbackVersion(0) == 1, "accepted combo feedback");
            level.Pause();
            float pausedTime = Time.time;
            yield return new WaitForSecondsRealtime(0.15f);
            Require(!level.IsPlaying && Mathf.Approximately(Time.time, pausedTime), "pause freezes gameplay time");
            Require(!hud.IsFlashing(1) && !hud.IsFlashing(2), "pause clears feedback");
            level.Resume();
            Require(level.IsPlaying && Time.timeScale == 1f, "resume");
            level.Pause();
            level.RestartGame();
            yield return null;
            yield return null;
            level = LevelDirector.Instance;
            Require(level.IsPlaying && !level.Rescued && !level.EliteDefeated && level.DefeatedCount == 0, "restart resets progress and starts");
            Require(level.player.Health.Current == level.player.Health.Maximum, "restart resets health");
            Require(Object.FindObjectsByType<EnemyBrain>().Length == 6, "restart resets enemies");
            hud = Object.FindAnyObjectByType<SkillHud>();
            Require(Object.FindObjectsByType<SkillHud>().Length == 1, "restart does not duplicate UI");
            for (int i = 0; i < 3; i++) Require(hud.FeedbackVersion(i) == 0, "restart clears feedback");
            level.Pause();
            Debug.Log("SESSION_SMOKE_PASS: begin, accepted/blocked skill feedback, pause, resume, restart, and quit from pause.");
            level.QuitGame();
        }

        private static void Require(bool condition, string description)
        {
            if (condition) return;
            Debug.LogError("SESSION_SMOKE_FAIL: " + description);
            Application.Quit(1);
            throw new InvalidOperationException(description);
        }
    }
}
