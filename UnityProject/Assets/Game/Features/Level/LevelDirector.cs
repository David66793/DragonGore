using UnityEngine;
using UnityEngine.SceneManagement;

namespace Changban
{
    public sealed class LevelDirector : MonoBehaviour
    {
        public enum Phase { Ready, Playing, Paused, Victory, Defeat }
        public static LevelDirector Instance { get; private set; }
        public GameBalance balance;
        public PlayerMotor player;
        public Transform rescuePoint;
        public Transform exitPoint;
        public EnemyBrain meleePrefab;
        public EnemyBrain archerPrefab;
        public EnemyBrain elitePrefab;
        public Phase CurrentPhase { get; private set; } = Phase.Ready;
        public bool Rescued { get; private set; }
        public bool EliteDefeated { get; private set; }
        public bool IsPlaying => CurrentPhase == Phase.Playing && !ExitRequested;
        public bool ExitRequested { get; private set; }
        public static event System.Action EditorExitRequested;
        public int DefeatedCount { get; private set; }
        private bool reinforcementsSpawned;
        private static bool beginAfterReload;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (player != null) player.Health.Died += OnPlayerDied;
            if (beginAfterReload)
            {
                beginAfterReload = false;
                Begin();
            }
        }

        private void OnDestroy()
        {
            if (player != null && player.Health != null) player.Health.Died -= OnPlayerDied;
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (ExitRequested) return;
            if (CurrentPhase != Phase.Ready && GameInput.Restart) { RestartGame(); return; }
            if (CurrentPhase == Phase.Ready)
            {
                if (GameInput.Start) Begin();
                return;
            }
            if (CurrentPhase == Phase.Victory || CurrentPhase == Phase.Defeat)
            {
                return;
            }
            if (GameInput.Pause)
            {
                if (CurrentPhase == Phase.Playing) Pause();
                else if (CurrentPhase == Phase.Paused) Resume();
            }
            if (!IsPlaying || player == null) return;
            if (GameInput.Interact) TryRescue();
            if (exitPoint != null && LevelRules.CanEscape(Rescued, EliteDefeated,
                    Vector2.Distance(player.transform.position, exitPoint.position) < 1.4f, !player.Health.IsDead))
                CurrentPhase = Phase.Victory;
        }

        public void Begin() { if (CurrentPhase != Phase.Ready || ExitRequested) return; CurrentPhase = Phase.Playing; Time.timeScale = 1f; }
        public void Pause() { if (!IsPlaying) return; CurrentPhase = Phase.Paused; Time.timeScale = 0f; }
        public void Resume() { if (CurrentPhase != Phase.Paused || ExitRequested) return; CurrentPhase = Phase.Playing; Time.timeScale = 1f; }

        public bool TryRescue()
        {
            if (!IsPlaying || Rescued || player == null || rescuePoint == null ||
                Vector2.Distance(player.transform.position, rescuePoint.position) >= 1.25f) return false;
            Rescue();
            return true;
        }

        private void Rescue()
        {
            Rescued = true;
            if (rescuePoint != null)
            {
                var aura = rescuePoint.Find("Rescue Aura")?.GetComponent<SpriteRenderer>();
                if (aura != null) aura.color = new Color(0.34f, 0.78f, 0.53f, 0.46f);
            }
            SpawnReinforcements();
        }

        private void SpawnReinforcements()
        {
            if (reinforcementsSpawned) return;
            reinforcementsSpawned = true;
            Spawn(meleePrefab, new Vector2(11, 1.8f));
            Spawn(meleePrefab, new Vector2(13, -1.8f));
            Spawn(archerPrefab, new Vector2(14, 2.1f));
            Spawn(elitePrefab, new Vector2(16.5f, 0));
        }

        private void Spawn(EnemyBrain prefab, Vector2 position)
        {
            if (prefab != null) Instantiate(prefab, position, Quaternion.identity);
        }

        public void EnemyDefeated(EnemyBrain enemy)
        {
            DefeatedCount++;
            if (enemy.role == EnemyBrain.Role.Elite) EliteDefeated = true;
        }

        private void OnPlayerDied(Health _) { CurrentPhase = Phase.Defeat; }

        public void Restart()
        {
            Reload(false);
        }

        public void RestartGame() { Reload(true); }

        private void Reload(bool immediatelyPlay)
        {
            beginAfterReload = immediatelyPlay;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            if (ExitRequested) return;
            ExitRequested = true;
            Time.timeScale = 1f;
            if (Application.isEditor) EditorExitRequested?.Invoke();
            else Application.Quit();
        }

        public string Objective
        {
            get
            {
                if (!Rescued) return "Find A Dou at the ruined camp. Press E nearby.";
                if (!EliteDefeated) return "Defeat the elite guarding the exit.";
                return "Reach the green evacuation marker.";
            }
        }
    }
}
