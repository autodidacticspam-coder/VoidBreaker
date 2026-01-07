using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBreaker.Core;
using VoidBreaker.Ship;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Controls the main game HUD - resources, alerts, and status displays.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Resource Displays")]
        [SerializeField] private TextMeshProUGUI scrapText;
        [SerializeField] private TextMeshProUGUI fuelText;
        [SerializeField] private TextMeshProUGUI missilesText;
        [SerializeField] private TextMeshProUGUI dronePartsText;

        [Header("Hull Display")]
        [SerializeField] private Slider hullBar;
        [SerializeField] private TextMeshProUGUI hullText;
        [SerializeField] private Image hullFill;
        [SerializeField] private Color hullHealthyColor = Color.green;
        [SerializeField] private Color hullDamagedColor = Color.yellow;
        [SerializeField] private Color hullCriticalColor = Color.red;

        [Header("Shield Display")]
        [SerializeField] private Transform shieldContainer;
        [SerializeField] private GameObject shieldLayerPrefab;
        [SerializeField] private Image[] shieldLayers;

        [Header("Evolution Display")]
        [SerializeField] private TextMeshProUGUI mutationPointsText;
        [SerializeField] private Slider evolutionBar;
        [SerializeField] private Image evolutionIcon;

        [Header("Sector Info")]
        [SerializeField] private TextMeshProUGUI sectorText;
        [SerializeField] private TextMeshProUGUI locationText;

        [Header("FTL Charge")]
        [SerializeField] private Slider ftlChargeBar;
        [SerializeField] private TextMeshProUGUI ftlText;
        [SerializeField] private GameObject ftlReadyIndicator;

        [Header("Alert Displays")]
        [SerializeField] private GameObject oxygenWarning;
        [SerializeField] private GameObject fireWarning;
        [SerializeField] private GameObject breachWarning;
        [SerializeField] private GameObject intruderWarning;

        [Header("Pause Indicator")]
        [SerializeField] private GameObject pausedOverlay;
        [SerializeField] private TextMeshProUGUI pausedText;

        [Header("References")]
        [SerializeField] private ShipController playerShip;

        private RunManager currentRun;

        public void Initialize(ShipController ship)
        {
            playerShip = ship;
            currentRun = GameManager.Instance?.CurrentRun;

            SubscribeToEvents();
            UpdateAllDisplays();
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnScrapChanged += UpdateScrapDisplay;
            GameEvents.OnFuelChanged += UpdateFuelDisplay;
            GameEvents.OnHullDamaged += UpdateHullDisplay;
            GameEvents.OnShieldChanged += UpdateShieldDisplay;
            GameEvents.OnOxygenCritical += ShowOxygenWarning;
            GameEvents.OnGamePaused += ShowPausedOverlay;
            GameEvents.OnGameResumed += HidePausedOverlay;
        }

        private void OnDestroy()
        {
            GameEvents.OnScrapChanged -= UpdateScrapDisplay;
            GameEvents.OnFuelChanged -= UpdateFuelDisplay;
            GameEvents.OnHullDamaged -= UpdateHullDisplay;
            GameEvents.OnShieldChanged -= UpdateShieldDisplay;
            GameEvents.OnOxygenCritical -= ShowOxygenWarning;
            GameEvents.OnGamePaused -= ShowPausedOverlay;
            GameEvents.OnGameResumed -= HidePausedOverlay;
        }

        private void Update()
        {
            UpdateFTLCharge();
            UpdateWarnings();
        }

        // ==================== RESOURCE UPDATES ====================

        private void UpdateAllDisplays()
        {
            if (currentRun == null) return;

            UpdateScrapDisplay(new ScrapChangedArgs { newAmount = currentRun.State.scrap });
            UpdateFuelDisplay(new FuelChangedArgs { newAmount = currentRun.State.fuel });
            UpdateMissilesDisplay();
            UpdateDronePartsDisplay();
            UpdateHullDisplay(null);
            UpdateShieldDisplay(null);
            UpdateSectorDisplay();
            UpdateEvolutionDisplay();
        }

        private void UpdateScrapDisplay(ScrapChangedArgs args)
        {
            if (scrapText) scrapText.text = args.newAmount.ToString();
        }

        private void UpdateFuelDisplay(FuelChangedArgs args)
        {
            if (fuelText) fuelText.text = args.newAmount.ToString();
        }

        private void UpdateMissilesDisplay()
        {
            if (missilesText && currentRun != null)
                missilesText.text = currentRun.State.missiles.ToString();
        }

        private void UpdateDronePartsDisplay()
        {
            if (dronePartsText && currentRun != null)
                dronePartsText.text = currentRun.State.droneParts.ToString();
        }

        private void UpdateHullDisplay(HullDamagedArgs args)
        {
            if (playerShip == null) return;

            float hullPercent = (float)playerShip.CurrentHealth / playerShip.MaxHealth;

            if (hullBar)
            {
                hullBar.value = hullPercent;
            }

            if (hullText)
            {
                hullText.text = $"{playerShip.CurrentHealth}/{playerShip.MaxHealth}";
            }

            if (hullFill)
            {
                if (hullPercent > 0.5f)
                    hullFill.color = hullHealthyColor;
                else if (hullPercent > 0.25f)
                    hullFill.color = hullDamagedColor;
                else
                    hullFill.color = hullCriticalColor;
            }
        }

        private void UpdateShieldDisplay(ShieldChangedArgs args)
        {
            if (playerShip == null) return;

            var shields = playerShip.GetSystem<ShieldSystem>();
            if (shields == null) return;

            int current = shields.CurrentLayers;
            int max = shields.MaxLayers;

            // Update shield layer visuals
            if (shieldLayers != null)
            {
                for (int i = 0; i < shieldLayers.Length; i++)
                {
                    if (shieldLayers[i] != null)
                    {
                        if (i < max)
                        {
                            shieldLayers[i].gameObject.SetActive(true);
                            shieldLayers[i].color = i < current ?
                                new Color(0.3f, 0.7f, 1f, 1f) :
                                new Color(0.3f, 0.3f, 0.3f, 0.5f);
                        }
                        else
                        {
                            shieldLayers[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void UpdateSectorDisplay()
        {
            if (currentRun == null) return;

            if (sectorText)
            {
                sectorText.text = $"Sector {currentRun.State.currentSector}";
            }
        }

        private void UpdateEvolutionDisplay()
        {
            if (currentRun == null) return;

            if (mutationPointsText)
            {
                mutationPointsText.text = currentRun.State.mutationPoints.ToString();
            }

            // Update evolution bar based on path progress
            var evolution = playerShip?.GetComponent<EvolutionManager>();
            if (evolution != null && evolutionBar)
            {
                var (path, level) = evolution.GetDominantPath();
                evolutionBar.value = level / 4f; // 4 tiers
            }
        }

        // ==================== FTL CHARGE ====================

        private void UpdateFTLCharge()
        {
            if (playerShip == null) return;

            var engines = playerShip.GetSystem<EngineSystem>();
            if (engines == null) return;

            if (ftlChargeBar)
            {
                ftlChargeBar.value = engines.FTLChargePercent;
            }

            if (ftlText)
            {
                if (engines.IsFTLReady)
                    ftlText.text = "FTL READY";
                else if (engines.IsChargingFTL)
                    ftlText.text = $"FTL {(engines.FTLChargePercent * 100):F0}%";
                else
                    ftlText.text = "FTL OFFLINE";
            }

            if (ftlReadyIndicator)
            {
                ftlReadyIndicator.SetActive(engines.IsFTLReady);
            }
        }

        // ==================== WARNINGS ====================

        private void UpdateWarnings()
        {
            if (playerShip == null) return;

            // Check for fires
            bool hasFire = false;
            bool hasBreach = false;
            bool hasIntruder = false;

            foreach (var room in playerShip.Rooms)
            {
                if (room.HasFire) hasFire = true;
                if (room.HasBreach) hasBreach = true;
                if (room.HasHostileCrew) hasIntruder = true;
            }

            if (fireWarning) fireWarning.SetActive(hasFire);
            if (breachWarning) breachWarning.SetActive(hasBreach);
            if (intruderWarning) intruderWarning.SetActive(hasIntruder);
        }

        private void ShowOxygenWarning(OxygenCriticalArgs args)
        {
            if (oxygenWarning)
            {
                oxygenWarning.SetActive(true);
                // Flash warning
            }
        }

        private void ShowPausedOverlay()
        {
            if (pausedOverlay) pausedOverlay.SetActive(true);
        }

        private void HidePausedOverlay()
        {
            if (pausedOverlay) pausedOverlay.SetActive(false);
        }

        // ==================== PUBLIC METHODS ====================

        public void RefreshAllDisplays()
        {
            currentRun = GameManager.Instance?.CurrentRun;
            UpdateAllDisplays();
        }

        public void SetLocation(string location)
        {
            if (locationText)
            {
                locationText.text = location;
            }
        }
    }
}
