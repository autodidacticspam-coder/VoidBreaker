using System.Collections;
using UnityEngine;
using VoidBreaker.Ship;
using VoidBreaker.Sector;
using VoidBreaker.Combat;
using VoidBreaker.Evolution;
using VoidBreaker.Events;
using VoidBreaker.Audio;
using VoidBreaker.UI;
using VoidBreaker.Data;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Main controller for gameplay scenes. Orchestrates all gameplay systems.
    /// </summary>
    public class GameplayController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private ShipController playerShip;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Managers")]
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private EventManager eventManager;
        [SerializeField] private SectorGenerator sectorGenerator;
        [SerializeField] private InputManager inputManager;

        [Header("UI")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private HUDController hudController;
        [SerializeField] private SectorMapDisplay sectorMap;

        [Header("Settings")]
        [SerializeField] private float combatStartDelay = 1f;
        [SerializeField] private float eventDisplayDelay = 0.5f;

        private Beacon currentBeacon;
        private ShipController currentEnemy;
        private GameplayState currentState = GameplayState.Exploring;

        public ShipController PlayerShip => playerShip;
        public Beacon CurrentBeacon => currentBeacon;
        public GameplayState CurrentState => currentState;

        private void Awake()
        {
            // Subscribe to events
            GameEvents.OnBeaconSelected += HandleBeaconSelected;
            GameEvents.OnCombatStart += HandleCombatStart;
            GameEvents.OnCombatEnd += HandleCombatEnd;
            GameEvents.OnEventStart += HandleEventStart;
            GameEvents.OnEventComplete += HandleEventComplete;
            GameEvents.OnShopEnter += HandleShopEnter;
            GameEvents.OnShopExit += HandleShopExit;
            GameEvents.OnPlayerDefeated += HandlePlayerDefeated;
            GameEvents.OnSectorComplete += HandleSectorComplete;
        }

        private void OnDestroy()
        {
            GameEvents.OnBeaconSelected -= HandleBeaconSelected;
            GameEvents.OnCombatStart -= HandleCombatStart;
            GameEvents.OnCombatEnd -= HandleCombatEnd;
            GameEvents.OnEventStart -= HandleEventStart;
            GameEvents.OnEventComplete -= HandleEventComplete;
            GameEvents.OnShopEnter -= HandleShopEnter;
            GameEvents.OnShopExit -= HandleShopExit;
            GameEvents.OnPlayerDefeated -= HandlePlayerDefeated;
            GameEvents.OnSectorComplete -= HandleSectorComplete;
        }

        private void Start()
        {
            StartCoroutine(InitializeGameplay());
        }

        private IEnumerator InitializeGameplay()
        {
            Debug.Log("[Gameplay] Initializing...");

            // Wait for game manager
            while (GameManager.Instance == null)
            {
                yield return null;
            }

            // Setup player ship if not already set
            if (playerShip == null)
            {
                playerShip = FindObjectOfType<ShipController>();
            }

            if (inputManager != null)
            {
                inputManager.SetPlayerShip(playerShip);
            }

            // Generate initial sector if needed
            if (sectorGenerator != null && GameManager.Instance.CurrentSector == null)
            {
                sectorGenerator.GenerateSector(1, SectorType.Civilian);
            }

            // Set initial state
            SetState(GameplayState.Exploring);

            // Play exploration music
            AudioManager.Instance?.SetMusicState(MusicState.Exploration);

            // Update UI
            if (hudController != null)
            {
                hudController.RefreshAll();
            }

            Debug.Log("[Gameplay] Initialization complete.");
        }

        // ==================== STATE MANAGEMENT ====================

        private void SetState(GameplayState newState)
        {
            Debug.Log($"[Gameplay] State: {currentState} -> {newState}");
            currentState = newState;

            switch (newState)
            {
                case GameplayState.Exploring:
                    Time.timeScale = 1f;
                    break;
                case GameplayState.InCombat:
                    Time.timeScale = 1f;
                    break;
                case GameplayState.InEvent:
                    Time.timeScale = 0f; // Pause during events
                    break;
                case GameplayState.InShop:
                    Time.timeScale = 0f;
                    break;
                case GameplayState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameplayState.GameOver:
                    Time.timeScale = 0f;
                    break;
                case GameplayState.Victory:
                    Time.timeScale = 0f;
                    break;
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleBeaconSelected(BeaconSelectedArgs args)
        {
            if (currentState == GameplayState.InCombat)
                return;

            currentBeacon = GameManager.Instance.GetBeacon(args.BeaconId);

            if (currentBeacon != null)
            {
                StartCoroutine(TravelToBeacon(currentBeacon));
            }
        }

        private IEnumerator TravelToBeacon(Beacon beacon)
        {
            Debug.Log($"[Gameplay] Traveling to beacon: {beacon.id}");

            // Consume fuel
            if (!GameManager.Instance.ConsumeFuel(1))
            {
                Debug.LogWarning("Not enough fuel!");
                yield break;
            }

            // Play jump sound/animation
            AudioManager.Instance?.PlaySFX("ftl_jump");

            // Brief delay for jump effect
            yield return new WaitForSeconds(0.5f);

            // Mark beacon as visited
            beacon.isVisited = true;
            GameManager.Instance.SetCurrentBeacon(beacon);

            // Handle beacon content
            yield return ProcessBeaconContent(beacon);
        }

        private IEnumerator ProcessBeaconContent(Beacon beacon)
        {
            // Check for combat
            if (beacon.hasEnemy)
            {
                yield return StartCombatEncounter(beacon);
            }
            // Check for event
            else if (!string.IsNullOrEmpty(beacon.eventId))
            {
                yield return new WaitForSeconds(eventDisplayDelay);
                eventManager?.TriggerEvent(beacon.eventId);
            }
            // Check for shop
            else if (beacon.type == BeaconType.Store)
            {
                GameEvents.TriggerShopEnter(new ShopEnterArgs());
            }
            // Check for exit
            else if (beacon.type == BeaconType.Exit)
            {
                CheckForSectorExit();
            }
            else
            {
                // Empty beacon - just exploring
                SetState(GameplayState.Exploring);
            }
        }

        private IEnumerator StartCombatEncounter(Beacon beacon)
        {
            yield return new WaitForSeconds(combatStartDelay);

            // Spawn enemy
            if (beacon.enemyDefinition != null && enemySpawnPoint != null)
            {
                // TODO: Spawn actual enemy ship from definition
                Debug.Log($"[Gameplay] Starting combat with: {beacon.enemyDefinition.enemyName}");
            }

            if (combatManager != null && playerShip != null)
            {
                combatManager.StartCombat(playerShip, currentEnemy);
            }
        }

        private void HandleCombatStart(CombatStartArgs args)
        {
            SetState(GameplayState.InCombat);
            AudioManager.Instance?.SetMusicState(MusicState.Combat);
            uiManager?.SetState(UIState.Combat);
        }

        private void HandleCombatEnd(CombatEndArgs args)
        {
            SetState(GameplayState.Exploring);

            if (args.PlayerWon)
            {
                AudioManager.Instance?.SetMusicState(MusicState.Victory);

                // Award rewards
                var reward = combatManager?.CalculateRewards();
                if (reward != null)
                {
                    // TODO: Show reward screen with draft options
                }
            }
            else
            {
                AudioManager.Instance?.SetMusicState(MusicState.Defeat);
            }

            // Clean up enemy
            if (currentEnemy != null)
            {
                Destroy(currentEnemy.gameObject);
                currentEnemy = null;
            }

            uiManager?.SetState(UIState.Playing);
        }

        private void HandleEventStart(EventStartArgs args)
        {
            SetState(GameplayState.InEvent);
            uiManager?.SetState(UIState.Event);
        }

        private void HandleEventComplete(EventCompleteArgs args)
        {
            SetState(GameplayState.Exploring);
            AudioManager.Instance?.SetMusicState(MusicState.Exploration);
            uiManager?.SetState(UIState.Playing);
        }

        private void HandleShopEnter(ShopEnterArgs args)
        {
            SetState(GameplayState.InShop);
            AudioManager.Instance?.SetMusicState(MusicState.Shop);
            uiManager?.SetState(UIState.Shop);
        }

        private void HandleShopExit()
        {
            SetState(GameplayState.Exploring);
            AudioManager.Instance?.SetMusicState(MusicState.Exploration);
            uiManager?.SetState(UIState.Playing);
        }

        private void HandlePlayerDefeated(PlayerDefeatedArgs args)
        {
            SetState(GameplayState.GameOver);
            AudioManager.Instance?.SetMusicState(MusicState.Defeat);
            uiManager?.SetState(UIState.GameOver);

            // Save meta progress
            SaveManager.Instance?.SaveMetaProgress();
        }

        private void HandleSectorComplete(SectorCompleteArgs args)
        {
            int nextSector = args.SectorNumber + 1;

            if (nextSector > 8)
            {
                // Reached final boss
                StartFinalBoss();
            }
            else
            {
                // Generate next sector
                StartCoroutine(TransitionToNextSector(nextSector));
            }
        }

        private IEnumerator TransitionToNextSector(int sectorNumber)
        {
            // Show sector transition screen
            uiManager?.ShowSectorTransition(sectorNumber);

            yield return new WaitForSecondsRealtime(2f);

            // Determine sector type
            SectorType sectorType = DetermineSectorType(sectorNumber);

            // Generate new sector
            sectorGenerator?.GenerateSector(sectorNumber, sectorType);

            // Refresh UI
            sectorMap?.RefreshDisplay();
            hudController?.RefreshAll();

            SetState(GameplayState.Exploring);
        }

        private SectorType DetermineSectorType(int sectorNumber)
        {
            // Progression of sector types
            return sectorNumber switch
            {
                1 or 2 => SectorType.Civilian,
                3 or 4 => SectorType.Hostile,
                5 => SectorType.Nebula,
                6 or 7 => SectorType.Hostile,
                8 => SectorType.FinalBoss,
                _ => SectorType.Civilian
            };
        }

        private void CheckForSectorExit()
        {
            // Trigger sector complete
            var currentSector = GameManager.Instance.CurrentSector;
            if (currentSector != null)
            {
                GameEvents.TriggerSectorComplete(new SectorCompleteArgs
                {
                    SectorNumber = currentSector.sectorNumber,
                    SectorType = currentSector.type
                });
            }
        }

        private void StartFinalBoss()
        {
            // Determine which boss based on evolution path
            var evolution = playerShip?.GetComponent<ShipEvolution>();
            if (evolution == null)
            {
                Debug.LogWarning("No evolution component found on player ship!");
                return;
            }

            EvolutionPath dominantPath = evolution.GetDominantPath();

            Debug.Log($"[Gameplay] Starting final boss for path: {dominantPath}");

            switch (dominantPath)
            {
                case EvolutionPath.Predator:
                    StartCoroutine(StartDreadnoughtBoss());
                    break;
                case EvolutionPath.Phantom:
                    StartCoroutine(StartInfiltrationBoss());
                    break;
                case EvolutionPath.Herald:
                    StartCoroutine(StartNegotiationBoss());
                    break;
            }
        }

        private IEnumerator StartDreadnoughtBoss()
        {
            AudioManager.Instance?.SetMusicState(MusicState.Boss);
            uiManager?.ShowBossIntro("THE DREADNOUGHT", "A vessel of pure destruction. Destroy or be destroyed.");

            yield return new WaitForSecondsRealtime(3f);

            // Spawn and start Dreadnought encounter
            // The DreadnoughtBoss component handles the rest
        }

        private IEnumerator StartInfiltrationBoss()
        {
            AudioManager.Instance?.SetMusicState(MusicState.Boss);
            uiManager?.ShowBossIntro("STATION OMEGA", "Infiltrate. Sabotage. Escape.");

            yield return new WaitForSecondsRealtime(3f);

            // Spawn and start Station Infiltration encounter
        }

        private IEnumerator StartNegotiationBoss()
        {
            AudioManager.Instance?.SetMusicState(MusicState.Boss);
            uiManager?.ShowBossIntro("THE COUNCIL", "Words are your weapons. Convince them.");

            yield return new WaitForSecondsRealtime(3f);

            // Start Council Negotiation encounter
        }

        // ==================== PAUSE HANDLING ====================

        private void Update()
        {
            // Toggle pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameplayState.Paused)
                {
                    ResumeGame();
                }
                else if (currentState == GameplayState.Exploring || currentState == GameplayState.InCombat)
                {
                    PauseGame();
                }
            }

            // Quick save (F5)
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (currentState == GameplayState.Exploring)
                {
                    SaveManager.Instance?.SaveGame();
                    hudController?.ShowMessage("Game Saved");
                }
            }
        }

        public void PauseGame()
        {
            if (currentState == GameplayState.GameOver || currentState == GameplayState.Victory)
                return;

            SetState(GameplayState.Paused);
            uiManager?.SetState(UIState.Paused);
            GameEvents.TriggerPauseToggle();
        }

        public void ResumeGame()
        {
            SetState(GameplayState.Exploring);
            uiManager?.SetState(UIState.Playing);
            GameEvents.TriggerPauseToggle();
        }

        public void ReturnToMenu()
        {
            // Save before leaving
            if (currentState != GameplayState.GameOver)
            {
                SaveManager.Instance?.SaveGame();
            }

            Time.timeScale = 1f;
            GameBootstrap.ReturnToMainMenu();
        }
    }

    public enum GameplayState
    {
        Exploring,
        InCombat,
        InEvent,
        InShop,
        Paused,
        GameOver,
        Victory,
        InBoss
    }
}
