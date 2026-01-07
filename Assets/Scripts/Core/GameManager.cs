using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBreaker.Sector;
using VoidBreaker.Data;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Singleton manager for overall game state and scene transitions.
    /// Persists across scenes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private bool isPaused;

        [Header("Settings")]
        [SerializeField] private GameSettings settings;

        [Header("Meta Progression")]
        [SerializeField] private MetaProgressionData metaProgression;

        // Current run data (null when not in a run)
        public RunManager CurrentRun { get; private set; }

        public GameState CurrentState => currentState;
        public bool IsPaused => isPaused;
        public GameSettings Settings => settings;
        public MetaProgressionData MetaProgression => metaProgression;

        // Events
        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            // Load settings
            settings = SaveManagerStatic.LoadSettings() ?? new GameSettings();

            // Load meta progression
            metaProgression = SaveManagerStatic.LoadMetaProgression() ?? new MetaProgressionData();

            // Apply audio settings
            AudioListener.volume = settings.masterVolume;

            Debug.Log("[GameManager] Initialized");
        }

        private void Update()
        {
            // Global pause input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameState.Combat || currentState == GameState.SectorMap)
                {
                    TogglePause();
                }
            }

            // Space to pause/unpause during combat
            if (Input.GetKeyDown(KeyCode.Space) && currentState == GameState.Combat)
            {
                TogglePause();
            }
        }

        // ==================== STATE MANAGEMENT ====================

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            var oldState = currentState;
            currentState = newState;

            Debug.Log($"[GameManager] State changed: {oldState} -> {newState}");

            OnStateChanged?.Invoke(newState);

            // Handle state-specific logic
            switch (newState)
            {
                case GameState.MainMenu:
                    isPaused = false;
                    Time.timeScale = 1f;
                    break;

                case GameState.Combat:
                    // Combat starts paused so player can assess
                    isPaused = true;
                    Time.timeScale = 0f;
                    break;

                case GameState.Event:
                    isPaused = true;
                    Time.timeScale = 0f;
                    break;

                case GameState.GameOver:
                    isPaused = true;
                    Time.timeScale = 0f;
                    break;
            }
        }

        public void TogglePause()
        {
            if (currentState == GameState.MainMenu || currentState == GameState.GameOver)
                return;

            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;

            if (isPaused)
                GameEvents.TriggerGamePaused();
            else
                GameEvents.TriggerGameResumed();

            Debug.Log($"[GameManager] Paused: {isPaused}");
        }

        public void SetPaused(bool paused)
        {
            if (isPaused == paused) return;
            TogglePause();
        }

        // ==================== RUN MANAGEMENT ====================

        public void StartNewRun(ShipDefinition ship, int layoutIndex = 0)
        {
            Debug.Log($"[GameManager] Starting new run with ship: {ship.shipName}");

            // Clear any existing run
            if (CurrentRun != null)
            {
                Destroy(CurrentRun.gameObject);
            }

            // Create run manager
            var runObject = new GameObject("RunManager");
            runObject.transform.SetParent(transform);
            CurrentRun = runObject.AddComponent<RunManager>();

            // Initialize run with ship and meta bonuses
            CurrentRun.Initialize(ship, layoutIndex, metaProgression);

            // Load game scene
            SceneManager.LoadScene("GameScene");

            SetState(GameState.SectorMap);
            GameEvents.TriggerGameStarted();
        }

        public void ContinueRun()
        {
            var saveData = SaveManagerStatic.LoadRunSave();
            if (saveData == null)
            {
                Debug.LogWarning("[GameManager] No save data found");
                return;
            }

            Debug.Log("[GameManager] Continuing saved run");

            // Create run manager and load data
            var runObject = new GameObject("RunManager");
            runObject.transform.SetParent(transform);
            CurrentRun = runObject.AddComponent<RunManager>();
            CurrentRun.LoadFromSave(saveData);

            SceneManager.LoadScene("GameScene");
            SetState(GameState.SectorMap);
        }

        public bool HasSavedRun()
        {
            return SaveManagerStatic.HasRunSave();
        }

        public void EndRun(bool victory)
        {
            if (CurrentRun == null) return;

            Debug.Log($"[GameManager] Run ended. Victory: {victory}");

            // Update meta progression
            if (victory)
            {
                metaProgression.totalWins++;
                metaProgression.UnlockGeneMemory(CurrentRun.GetRunStatistics());
            }
            metaProgression.totalRuns++;
            metaProgression.totalPlayTime += CurrentRun.RunDuration;

            // Save meta progression
            SaveManagerStatic.SaveMetaProgression(metaProgression);

            // Delete run save
            SaveManagerStatic.DeleteRunSave();

            // Show game over screen
            SetState(GameState.GameOver);
            GameEvents.TriggerGameOver(new GameOverArgs
            {
                victory = victory,
                reason = victory ? "Victory!" : "Defeat",
                finalScore = metaProgression.totalRuns
            });
        }

        public void ReturnToMainMenu()
        {
            // Save current run if in progress
            if (CurrentRun != null && currentState != GameState.GameOver)
            {
                SaveManagerStatic.SaveRun(CurrentRun.GetSaveData());
            }

            // Cleanup
            if (CurrentRun != null)
            {
                Destroy(CurrentRun.gameObject);
                CurrentRun = null;
            }

            GameEvents.ClearAllListeners();
            SceneManager.LoadScene("MainMenu");
            SetState(GameState.MainMenu);
        }

        // ==================== SECTOR/NAVIGATION HELPERS ====================
        // These forward to RunManager for convenience

        /// <summary>
        /// Current sector data from the run. Null if not in a run.
        /// </summary>
        public SectorData CurrentSector => CurrentRun?.SectorManager?.CurrentSector;

        /// <summary>
        /// Gets a beacon by ID from the current sector.
        /// </summary>
        public Beacon GetBeacon(string beaconId)
        {
            return CurrentRun?.SectorManager?.GetBeacon(beaconId);
        }

        /// <summary>
        /// Sets the current beacon the player is at.
        /// </summary>
        public void SetCurrentBeacon(Beacon beacon)
        {
            CurrentRun?.SectorManager?.SetCurrentBeacon(beacon);
        }

        /// <summary>
        /// Consumes fuel for travel. Returns false if not enough fuel.
        /// </summary>
        public bool ConsumeFuel(int amount = 1)
        {
            return CurrentRun?.SpendFuel(amount) ?? false;
        }

        // ==================== SETTINGS ====================

        public void SaveSettings()
        {
            SaveManagerStatic.SaveSettings(settings);
        }

        public void ApplySettings(GameSettings newSettings)
        {
            settings = newSettings;
            AudioListener.volume = settings.masterVolume;
            SaveSettings();
        }
    }

    public enum GameState
    {
        MainMenu,
        ShipSelect,
        SectorMap,
        Combat,
        Event,
        Shop,
        Paused,
        GameOver
    }

    [Serializable]
    public class GameSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;

        public bool colorBlindMode = false;
        public bool showTutorialTips = true;
        public bool autoPauseOnEvent = true;

        public int targetFrameRate = 60;
        public bool vSync = true;
    }

    [Serializable]
    public class MetaProgressionData
    {
        public int totalRuns;
        public int totalWins;
        public float totalPlayTime;

        // Unlocked ships (by ID)
        public string[] unlockedShips = { "vagrant_a" };

        // Unlocked gene memories
        public string[] unlockedGeneMemories = { };

        // Statistics
        public int totalEnemiesDefeated;
        public int totalCrewLost;
        public int totalScrapEarned;
        public int totalMutationPoints;

        // Best runs
        public int highestSectorReached;
        public int fastestWinSeconds;

        public bool HasGeneMemory(string memoryId)
        {
            return Array.IndexOf(unlockedGeneMemories, memoryId) >= 0;
        }

        public void UnlockGeneMemory(RunStatistics stats)
        {
            // Check unlock conditions and add new gene memories
            // This will be expanded based on GDD requirements
        }
    }
}
