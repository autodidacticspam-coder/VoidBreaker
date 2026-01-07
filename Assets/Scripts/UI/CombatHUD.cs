using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Combat;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Combat-specific HUD elements.
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        [Header("Enemy Display")]
        [SerializeField] private GameObject enemyPanel;
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private Slider enemyHullBar;
        [SerializeField] private TextMeshProUGUI enemyHullText;
        [SerializeField] private Transform enemyShieldContainer;
        [SerializeField] private Image[] enemyShieldLayers;

        [Header("Combat Timer")]
        [SerializeField] private TextMeshProUGUI combatTimeText;

        [Header("Pause Button")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI pauseButtonText;

        [Header("Flee Button")]
        [SerializeField] private Button fleeButton;
        [SerializeField] private TextMeshProUGUI fleeButtonText;
        [SerializeField] private Image fleeCooldownFill;

        [Header("Boss Display")]
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private Slider bossPhaseBar;
        [SerializeField] private TextMeshProUGUI bossPhaseText;

        [Header("Combat Log")]
        [SerializeField] private Transform logContainer;
        [SerializeField] private TextMeshProUGUI logEntryPrefab;
        [SerializeField] private int maxLogEntries = 10;

        private ShipController playerShip;
        private ShipController enemyShip;
        private CombatManager combatManager;

        public void Initialize(ShipController player, ShipController enemy)
        {
            playerShip = player;
            enemyShip = enemy;
            combatManager = FindObjectOfType<CombatManager>();

            UpdateEnemyDisplay();

            if (bossPanel) bossPanel.SetActive(false);

            // Subscribe to combat events
            GameEvents.OnHullDamaged += HandleDamage;
            GameEvents.OnProjectileFired += HandleProjectileFired;
            GameEvents.OnProjectileMissed += HandleProjectileMissed;
            GameEvents.OnBossPhaseChanged += HandleBossPhaseChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnHullDamaged -= HandleDamage;
            GameEvents.OnProjectileFired -= HandleProjectileFired;
            GameEvents.OnProjectileMissed -= HandleProjectileMissed;
            GameEvents.OnBossPhaseChanged -= HandleBossPhaseChanged;
        }

        private void Update()
        {
            UpdateCombatTime();
            UpdateFleeButton();
            UpdateEnemyDisplay();
        }

        // ==================== DISPLAY UPDATES ====================

        private void UpdateEnemyDisplay()
        {
            if (enemyShip == null || enemyPanel == null) return;

            if (enemyNameText)
            {
                enemyNameText.text = enemyShip.Definition?.shipName ?? "Enemy Ship";
            }

            float hullPercent = (float)enemyShip.CurrentHealth / enemyShip.MaxHealth;

            if (enemyHullBar)
            {
                enemyHullBar.value = hullPercent;
            }

            if (enemyHullText)
            {
                enemyHullText.text = $"{enemyShip.CurrentHealth}/{enemyShip.MaxHealth}";
            }

            // Update enemy shields
            var shields = enemyShip.GetSystem<ShieldSystem>();
            if (shields != null && enemyShieldLayers != null)
            {
                for (int i = 0; i < enemyShieldLayers.Length; i++)
                {
                    if (enemyShieldLayers[i] != null)
                    {
                        if (i < shields.MaxLayers)
                        {
                            enemyShieldLayers[i].gameObject.SetActive(true);
                            enemyShieldLayers[i].color = i < shields.CurrentLayers ?
                                new Color(1f, 0.3f, 0.3f, 1f) :
                                new Color(0.3f, 0.3f, 0.3f, 0.5f);
                        }
                        else
                        {
                            enemyShieldLayers[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void UpdateCombatTime()
        {
            if (combatManager == null || combatTimeText == null) return;

            float time = combatManager.CombatTime;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);

            combatTimeText.text = $"{minutes:00}:{seconds:00}";
        }

        private void UpdateFleeButton()
        {
            if (playerShip == null || fleeButton == null) return;

            var engines = playerShip.GetSystem<EngineSystem>();
            if (engines == null)
            {
                fleeButton.interactable = false;
                return;
            }

            bool canFlee = engines.IsFTLReady;
            fleeButton.interactable = canFlee;

            if (fleeButtonText)
            {
                fleeButtonText.text = canFlee ? "FLEE" : $"FTL {(engines.FTLChargePercent * 100):F0}%";
            }

            if (fleeCooldownFill)
            {
                fleeCooldownFill.fillAmount = 1f - engines.FTLChargePercent;
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleDamage(HullDamagedArgs args)
        {
            string target = args.ship == playerShip ? "Player" : "Enemy";
            AddLogEntry($"{target} takes {args.damage} damage!");
        }

        private void HandleProjectileFired(ProjectileFiredArgs args)
        {
            string shooter = args.weapon.IsPlayerOwned ? "You fire" : "Enemy fires";
            AddLogEntry($"{shooter} {args.weapon.WeaponName}!");
        }

        private void HandleProjectileMissed(ProjectileMissedArgs args)
        {
            string target = args.targetShip == playerShip ? "You evade" : "Enemy evades";
            AddLogEntry($"{target} the attack!");
        }

        private void HandleBossPhaseChanged(BossPhaseChangedArgs args)
        {
            if (bossPanel) bossPanel.SetActive(true);

            if (bossNameText)
                bossNameText.text = args.bossName;

            if (bossPhaseBar)
                bossPhaseBar.value = (float)args.phase / args.maxPhases;

            if (bossPhaseText)
                bossPhaseText.text = $"Phase {args.phase}/{args.maxPhases}";

            AddLogEntry($"BOSS: {args.bossName} - Phase {args.phase}!");
        }

        // ==================== COMBAT LOG ====================

        private void AddLogEntry(string message)
        {
            if (logContainer == null || logEntryPrefab == null) return;

            // Remove old entries if at max
            while (logContainer.childCount >= maxLogEntries)
            {
                Destroy(logContainer.GetChild(0).gameObject);
            }

            // Create new entry
            var entry = Instantiate(logEntryPrefab, logContainer);
            entry.text = message;
        }

        // ==================== BUTTON HANDLERS ====================

        public void OnPauseClicked()
        {
            if (combatManager != null)
            {
                combatManager.TogglePause();
                UpdatePauseButton();
            }
        }

        public void OnFleeClicked()
        {
            if (combatManager != null)
            {
                combatManager.TryFlee();
            }
        }

        private void UpdatePauseButton()
        {
            if (pauseButtonText && combatManager != null)
            {
                pauseButtonText.text = combatManager.IsPaused ? "RESUME" : "PAUSE";
            }
        }

        public void SetEnemy(ShipController enemy)
        {
            enemyShip = enemy;
            UpdateEnemyDisplay();
        }
    }
}
