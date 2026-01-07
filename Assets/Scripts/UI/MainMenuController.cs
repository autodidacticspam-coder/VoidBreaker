using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using VoidBreaker.Core;
using VoidBreaker.Audio;
using VoidBreaker.Data;
using VoidBreaker.Save;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Controls the main menu flow and ship selection.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject shipSelectPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject tutorialPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Ship Selection")]
        [SerializeField] private Transform shipListContainer;
        [SerializeField] private GameObject shipCardPrefab;
        [SerializeField] private Image selectedShipPreview;
        [SerializeField] private Text selectedShipName;
        [SerializeField] private Text selectedShipDescription;
        [SerializeField] private Text selectedShipStats;
        [SerializeField] private Button startGameButton;

        [Header("Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Dropdown resolutionDropdown;

        [Header("Content")]
        [SerializeField] private ContentDatabase contentDatabase;

        private ShipDefinition selectedShip;
        private List<ShipCard> shipCards = new List<ShipCard>();
        private MenuState currentState = MenuState.Main;

        private enum MenuState
        {
            Main,
            ShipSelect,
            Settings,
            Credits,
            Tutorial
        }

        private void Start()
        {
            InitializeMenu();
            ShowPanel(MenuState.Main);

            // Setup audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicState(MusicState.MainMenu);
            }
        }

        private void InitializeMenu()
        {
            // Main menu buttons
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                continueButton.interactable = SaveManager.Instance?.HasSaveData ?? false;
            }

            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Start game button
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
                startGameButton.interactable = false;
            }

            // Settings sliders
            InitializeSettings();

            // Build content
            if (contentDatabase != null)
            {
                contentDatabase.BuildLookups();
            }
        }

        private void InitializeSettings()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (resolutionDropdown != null)
            {
                PopulateResolutions();
            }
        }

        // ==================== NAVIGATION ====================

        private void ShowPanel(MenuState state)
        {
            currentState = state;

            if (mainPanel != null)
                mainPanel.SetActive(state == MenuState.Main);
            if (shipSelectPanel != null)
                shipSelectPanel.SetActive(state == MenuState.ShipSelect);
            if (settingsPanel != null)
                settingsPanel.SetActive(state == MenuState.Settings);
            if (creditsPanel != null)
                creditsPanel.SetActive(state == MenuState.Credits);
            if (tutorialPanel != null)
                tutorialPanel.SetActive(state == MenuState.Tutorial);

            if (state == MenuState.ShipSelect)
            {
                PopulateShipList();
            }
        }

        public void OnBackClicked()
        {
            PlayClickSound();
            ShowPanel(MenuState.Main);
        }

        // ==================== MAIN MENU HANDLERS ====================

        private void OnContinueClicked()
        {
            PlayClickSound();
            GameBootstrap.ContinueGame();
        }

        private void OnNewGameClicked()
        {
            PlayClickSound();
            ShowPanel(MenuState.ShipSelect);
        }

        private void OnSettingsClicked()
        {
            PlayClickSound();
            ShowPanel(MenuState.Settings);
        }

        private void OnCreditsClicked()
        {
            PlayClickSound();
            ShowPanel(MenuState.Credits);
        }

        private void OnQuitClicked()
        {
            PlayClickSound();
            GameBootstrap.QuitGame();
        }

        // ==================== SHIP SELECTION ====================

        private void PopulateShipList()
        {
            // Clear existing cards
            foreach (var card in shipCards)
            {
                if (card != null && card.gameObject != null)
                    Destroy(card.gameObject);
            }
            shipCards.Clear();

            // Get unlocked ships
            List<ShipDefinition> ships;
            if (contentDatabase != null)
            {
                ships = contentDatabase.GetUnlockedShips();
            }
            else
            {
                ships = new List<ShipDefinition>();
            }

            // Create cards
            foreach (var ship in ships)
            {
                if (shipCardPrefab == null || shipListContainer == null)
                    continue;

                var cardObj = Instantiate(shipCardPrefab, shipListContainer);
                var card = cardObj.GetComponent<ShipCard>();

                if (card != null)
                {
                    card.Setup(ship, OnShipCardClicked);
                    shipCards.Add(card);
                }
            }

            // Select first ship by default
            if (ships.Count > 0)
            {
                SelectShip(ships[0]);
            }
        }

        private void OnShipCardClicked(ShipDefinition ship)
        {
            PlayClickSound();
            SelectShip(ship);
        }

        private void SelectShip(ShipDefinition ship)
        {
            selectedShip = ship;

            // Update preview
            if (selectedShipPreview != null && ship.shipSprite != null)
            {
                selectedShipPreview.sprite = ship.shipSprite;
            }

            if (selectedShipName != null)
            {
                selectedShipName.text = ship.shipName;
            }

            if (selectedShipDescription != null)
            {
                selectedShipDescription.text = ship.description;
            }

            if (selectedShipStats != null)
            {
                selectedShipStats.text = FormatShipStats(ship);
            }

            // Update cards to show selection
            foreach (var card in shipCards)
            {
                card.SetSelected(card.Ship == ship);
            }

            // Enable start button
            if (startGameButton != null)
            {
                startGameButton.interactable = true;
            }
        }

        private string FormatShipStats(ShipDefinition ship)
        {
            return $"Hull: {ship.maxHull}\n" +
                   $"Shields: {ship.maxShieldLayers}\n" +
                   $"Power: {ship.reactorPower}\n" +
                   $"Evasion: {ship.baseEvasion}%\n" +
                   $"Weapons: {ship.weaponSlots}\n" +
                   $"Drones: {ship.droneSlots}";
        }

        private void OnStartGameClicked()
        {
            if (selectedShip == null)
                return;

            PlayClickSound();
            GameBootstrap.StartNewGame(selectedShip);
        }

        // ==================== SETTINGS HANDLERS ====================

        private void OnMasterVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            AudioManager.Instance?.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            AudioManager.Instance?.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            AudioManager.Instance?.SetSFXVolume(value);
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        private void PopulateResolutions()
        {
            resolutionDropdown.ClearOptions();

            var resolutions = Screen.resolutions;
            var options = new List<string>();
            int currentIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                var res = resolutions[i];
                options.Add($"{res.width} x {res.height} @ {res.refreshRateRatio}Hz");

                if (res.width == Screen.currentResolution.width &&
                    res.height == Screen.currentResolution.height)
                {
                    currentIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentIndex;
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        private void OnResolutionChanged(int index)
        {
            var resolution = Screen.resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }

        // ==================== UTILITY ====================

        private void PlayClickSound()
        {
            AudioManager.Instance?.PlaySFX("ui_click");
        }

        private void Update()
        {
            // Handle escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState != MenuState.Main)
                {
                    OnBackClicked();
                }
            }
        }
    }

    /// <summary>
    /// UI component for ship selection cards.
    /// </summary>
    public class ShipCard : MonoBehaviour
    {
        [SerializeField] private Image shipIcon;
        [SerializeField] private Text shipName;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private Button button;
        [SerializeField] private Image lockedOverlay;

        public ShipDefinition Ship { get; private set; }
        private System.Action<ShipDefinition> onClick;

        public void Setup(ShipDefinition ship, System.Action<ShipDefinition> clickCallback)
        {
            Ship = ship;
            onClick = clickCallback;

            if (shipIcon != null && ship.shipIcon != null)
            {
                shipIcon.sprite = ship.shipIcon;
            }

            if (shipName != null)
            {
                shipName.text = ship.shipName;
            }

            if (button != null)
            {
                button.onClick.AddListener(() => onClick?.Invoke(Ship));
            }

            if (lockedOverlay != null)
            {
                lockedOverlay.gameObject.SetActive(false); // For now, all shown ships are unlocked
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionBorder != null)
            {
                selectionBorder.enabled = selected;
            }
        }
    }
}
