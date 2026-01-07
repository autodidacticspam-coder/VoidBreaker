using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Combat;
using VoidBreaker.Events;
using VoidBreaker.Sector;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Central UI manager - handles all game UI states and transitions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject shipSelectPanel;
        [SerializeField] private GameObject gameHUDPanel;
        [SerializeField] private GameObject sectorMapPanel;
        [SerializeField] private GameObject combatPanel;
        [SerializeField] private GameObject eventPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject evolutionPanel;
        [SerializeField] private GameObject draftPanel;

        [Header("HUD Elements")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private ShipViewController shipView;
        [SerializeField] private SystemPowerBar powerBar;
        [SerializeField] private CrewRoster crewRoster;
        [SerializeField] private WeaponBar weaponBar;

        [Header("Combat UI")]
        [SerializeField] private CombatHUD combatHUD;
        [SerializeField] private TargetingReticle targetingReticle;

        [Header("Event UI")]
        [SerializeField] private EventDisplay eventDisplay;
        [SerializeField] private ShopDisplay shopDisplay;
        [SerializeField] private DraftDisplay draftDisplay;

        [Header("Map UI")]
        [SerializeField] private SectorMapDisplay mapDisplay;
        [SerializeField] private BeaconInfoPanel beaconInfo;

        [Header("Settings")]
        [SerializeField] private float fadeTime = 0.3f;

        private UIState currentState = UIState.MainMenu;
        private Stack<UIState> stateHistory = new Stack<UIState>();

        public UIState CurrentState => currentState;

        // Events
        public event Action<UIState> OnStateChanged;

        private void Awake()
        {
            // Hide all panels initially
            HideAllPanels();
        }

        private void Start()
        {
            // Subscribe to game events
            GameEvents.OnCombatStarted += HandleCombatStarted;
            GameEvents.OnCombatEnded += HandleCombatEnded;
            GameEvents.OnEventStarted += HandleEventStarted;
            GameEvents.OnEventEnded += HandleEventEnded;
            GameEvents.OnGamePaused += HandleGamePaused;
            GameEvents.OnGameResumed += HandleGameResumed;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnShopOpened += HandleShopOpened;

            ShowMainMenu();
        }

        private void OnDestroy()
        {
            GameEvents.OnCombatStarted -= HandleCombatStarted;
            GameEvents.OnCombatEnded -= HandleCombatEnded;
            GameEvents.OnEventStarted -= HandleEventStarted;
            GameEvents.OnEventEnded -= HandleEventEnded;
            GameEvents.OnGamePaused -= HandleGamePaused;
            GameEvents.OnGameResumed -= HandleGameResumed;
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnShopOpened -= HandleShopOpened;
        }

        // ==================== STATE MANAGEMENT ====================

        public void SetState(UIState newState)
        {
            if (currentState == newState) return;

            stateHistory.Push(currentState);
            currentState = newState;

            UpdatePanelVisibility();

            OnStateChanged?.Invoke(currentState);
        }

        public void GoBack()
        {
            if (stateHistory.Count > 0)
            {
                currentState = stateHistory.Pop();
                UpdatePanelVisibility();
                OnStateChanged?.Invoke(currentState);
            }
        }

        private void UpdatePanelVisibility()
        {
            HideAllPanels();

            switch (currentState)
            {
                case UIState.MainMenu:
                    ShowPanel(mainMenuPanel);
                    break;

                case UIState.ShipSelect:
                    ShowPanel(shipSelectPanel);
                    break;

                case UIState.Playing:
                    ShowPanel(gameHUDPanel);
                    break;

                case UIState.SectorMap:
                    ShowPanel(gameHUDPanel);
                    ShowPanel(sectorMapPanel);
                    break;

                case UIState.Combat:
                    ShowPanel(gameHUDPanel);
                    ShowPanel(combatPanel);
                    break;

                case UIState.Event:
                    ShowPanel(gameHUDPanel);
                    ShowPanel(eventPanel);
                    break;

                case UIState.Shop:
                    ShowPanel(gameHUDPanel);
                    ShowPanel(shopPanel);
                    break;

                case UIState.Paused:
                    ShowPanel(gameHUDPanel);
                    ShowPanel(pausePanel);
                    break;

                case UIState.GameOver:
                    ShowPanel(gameOverPanel);
                    break;

                case UIState.Evolution:
                    ShowPanel(gameHUDPanel);
                    ShowPanel(evolutionPanel);
                    break;

                case UIState.Draft:
                    ShowPanel(gameHUDPanel);
                    ShowPanel(draftPanel);
                    break;
            }
        }

        private void HideAllPanels()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (shipSelectPanel) shipSelectPanel.SetActive(false);
            if (gameHUDPanel) gameHUDPanel.SetActive(false);
            if (sectorMapPanel) sectorMapPanel.SetActive(false);
            if (combatPanel) combatPanel.SetActive(false);
            if (eventPanel) eventPanel.SetActive(false);
            if (shopPanel) shopPanel.SetActive(false);
            if (pausePanel) pausePanel.SetActive(false);
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (evolutionPanel) evolutionPanel.SetActive(false);
            if (draftPanel) draftPanel.SetActive(false);
        }

        private void ShowPanel(GameObject panel)
        {
            if (panel) panel.SetActive(true);
        }

        // ==================== SPECIFIC VIEWS ====================

        public void ShowMainMenu()
        {
            SetState(UIState.MainMenu);
        }

        public void ShowShipSelect()
        {
            SetState(UIState.ShipSelect);
        }

        public void ShowGameHUD()
        {
            SetState(UIState.Playing);
        }

        public void ShowSectorMap()
        {
            SetState(UIState.SectorMap);
        }

        public void ShowCombat()
        {
            SetState(UIState.Combat);
        }

        public void ShowEvent(GameEvent evt)
        {
            SetState(UIState.Event);
            eventDisplay?.DisplayEvent(evt);
        }

        public void ShowShop(ShopInventory inventory)
        {
            SetState(UIState.Shop);
            shopDisplay?.DisplayShop(inventory);
        }

        public void ShowPause()
        {
            SetState(UIState.Paused);
        }

        public void HidePause()
        {
            GoBack();
        }

        public void ShowGameOver(bool victory, GameEndingArgs ending = null)
        {
            SetState(UIState.GameOver);
            // Configure game over panel with victory/defeat state
        }

        public void ShowEvolutionTree()
        {
            SetState(UIState.Evolution);
        }

        public void ShowDraft(DraftOption[] options)
        {
            SetState(UIState.Draft);
            draftDisplay?.DisplayOptions(options);
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleCombatStarted(CombatStartedArgs args)
        {
            ShowCombat();
            combatHUD?.Initialize(args.playerShip, args.enemyShip);
        }

        private void HandleCombatEnded(CombatEndedArgs args)
        {
            if (args.playerWon)
            {
                // Show rewards first
            }
            else
            {
                ShowSectorMap();
            }
        }

        private void HandleEventStarted(EventStartedArgs args)
        {
            // Event display handles this
        }

        private void HandleEventEnded(EventEndedArgs args)
        {
            ShowSectorMap();
        }

        private void HandleGamePaused()
        {
            ShowPause();
        }

        private void HandleGameResumed()
        {
            HidePause();
        }

        private void HandleGameOver(GameOverArgs args)
        {
            ShowGameOver(args.victory);
        }

        private void HandleShopOpened(ShopOpenedArgs args)
        {
            // Shop will be displayed by ShopManager
        }

        // ==================== INPUT HANDLING ====================

        private void Update()
        {
            // Global input handling
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapePressed();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                HandleTabPressed();
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                HandleMapPressed();
            }
        }

        private void HandleEscapePressed()
        {
            switch (currentState)
            {
                case UIState.Playing:
                case UIState.Combat:
                    GameEvents.TriggerGamePaused();
                    break;

                case UIState.Paused:
                    GameEvents.TriggerGameResumed();
                    break;

                case UIState.SectorMap:
                case UIState.Evolution:
                case UIState.Shop:
                    GoBack();
                    break;

                case UIState.Event:
                    // Cannot escape from events
                    break;
            }
        }

        private void HandleTabPressed()
        {
            // Toggle evolution tree
            if (currentState == UIState.Playing || currentState == UIState.SectorMap)
            {
                ShowEvolutionTree();
            }
            else if (currentState == UIState.Evolution)
            {
                GoBack();
            }
        }

        private void HandleMapPressed()
        {
            if (currentState == UIState.Playing)
            {
                ShowSectorMap();
            }
            else if (currentState == UIState.SectorMap)
            {
                GoBack();
            }
        }

        // ==================== NOTIFICATIONS ====================

        public void ShowNotification(string message, float duration = 3f)
        {
            Debug.Log($"[UI Notification] {message}");
            // In full implementation, show notification toast
        }

        public void ShowDamageNumber(Vector3 position, int damage, Color color)
        {
            // Spawn floating damage number
        }

        public void ShowTooltip(string text, Vector3 position)
        {
            // Show tooltip at position
        }

        public void HideTooltip()
        {
            // Hide tooltip
        }
    }

    public enum UIState
    {
        MainMenu,
        ShipSelect,
        Playing,
        SectorMap,
        Combat,
        Event,
        Shop,
        Paused,
        GameOver,
        Evolution,
        Draft
    }
}
