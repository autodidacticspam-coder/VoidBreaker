using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Events;
using VoidBreaker.Sector;

namespace VoidBreaker.Combat
{
    /// <summary>
    /// Manages combat encounters - the core gameplay loop.
    /// Handles turn flow, AI decisions, projectiles, and combat resolution.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        [Header("Combat State")]
        [SerializeField] private CombatState currentState = CombatState.None;
        [SerializeField] private bool isPaused = true;
        [SerializeField] private float combatTime = 0f;

        [Header("Combatants")]
        [SerializeField] private ShipController playerShip;
        [SerializeField] private ShipController enemyShip;

        [Header("Projectiles")]
        [SerializeField] private List<Projectile> activeProjectiles = new List<Projectile>();
        [SerializeField] private Transform projectileContainer;

        [Header("Settings")]
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float combatStartDelay = 1f;

        [Header("Enemy AI")]
        [SerializeField] private EnemyAI enemyAI;

        [Header("Rewards")]
        [SerializeField] private RewardDrafter rewardDrafter;

        // Public accessors
        public CombatState CurrentState => currentState;
        public bool IsPaused => isPaused;
        public ShipController PlayerShip => playerShip;
        public ShipController EnemyShip => enemyShip;
        public float CombatTime => combatTime;

        // Events
        public event Action OnCombatStarted;
        public event Action<bool> OnCombatEnded;
        public event Action OnPauseToggled;

        public void Initialize(ShipController player)
        {
            playerShip = player;

            // Create projectile container
            var containerObj = new GameObject("ProjectileContainer");
            containerObj.transform.SetParent(transform);
            projectileContainer = containerObj.transform;

            // Create enemy AI
            var aiObj = new GameObject("EnemyAI");
            aiObj.transform.SetParent(transform);
            enemyAI = aiObj.AddComponent<EnemyAI>();

            // Get or create reward drafter
            rewardDrafter = GetComponent<RewardDrafter>();
            if (rewardDrafter == null)
            {
                rewardDrafter = gameObject.AddComponent<RewardDrafter>();
            }

            Debug.Log("[CombatManager] Initialized");
        }

        // ==================== COMBAT FLOW ====================

        /// <summary>
        /// Starts a combat encounter with an enemy ship.
        /// </summary>
        public void StartCombat(ShipController enemy, bool isAmbush = false)
        {
            if (currentState != CombatState.None)
            {
                Debug.LogWarning("[CombatManager] Combat already in progress");
                return;
            }

            enemyShip = enemy;
            currentState = CombatState.Starting;
            combatTime = 0f;
            isPaused = true; // Start paused so player can assess

            // Initialize enemy AI
            enemyAI.Initialize(enemyShip, playerShip);

            // Clear any lingering projectiles
            ClearProjectiles();

            // Start combat sequence
            StartCoroutine(CombatStartSequence(isAmbush));

            Debug.Log($"[CombatManager] Combat started against {enemy.Definition?.shipName ?? "Unknown"}");
        }

        private IEnumerator CombatStartSequence(bool isAmbush)
        {
            // Brief delay for dramatic effect
            yield return new WaitForSecondsRealtime(combatStartDelay);

            currentState = CombatState.Active;

            OnCombatStarted?.Invoke();

            GameEvents.TriggerCombatStarted(new CombatStartedArgs
            {
                playerShip = playerShip,
                enemyShip = enemyShip,
                isAmbush = isAmbush
            });

            // If ambush, enemy gets a free volley
            if (isAmbush)
            {
                Debug.Log("[CombatManager] AMBUSH! Enemy fires first!");
                enemyAI.ForceFireAll();
            }
        }

        /// <summary>
        /// Ends combat with a result.
        /// </summary>
        public void EndCombat(bool playerWon)
        {
            if (currentState == CombatState.None)
                return;

            currentState = CombatState.Ending;
            isPaused = true;

            // Calculate rewards
            int scrapReward = 0;
            int mutationReward = 0;

            if (playerWon)
            {
                scrapReward = CalculateScrapReward();
                mutationReward = CalculateMutationReward();
            }

            // Clear combat state
            ClearProjectiles();

            if (enemyShip != null)
            {
                Destroy(enemyShip.gameObject);
                enemyShip = null;
            }

            currentState = CombatState.None;

            OnCombatEnded?.Invoke(playerWon);

            GameEvents.TriggerCombatEnded(new CombatEndedArgs
            {
                playerWon = playerWon,
                scrapReward = scrapReward,
                mutationPointsReward = mutationReward
            });

            // Present rewards if victory
            if (playerWon)
            {
                var context = new DraftContext
                {
                    sectorNumber = GameManager.Instance?.CurrentRun?.State.currentSector ?? 1,
                    encounterType = BeaconType.Combat,
                    wasVictory = true,
                    difficultyRating = enemyAI?.DifficultyRating ?? 1
                };

                rewardDrafter.GenerateDraftOptions(context);
            }

            Debug.Log($"[CombatManager] Combat ended. Player won: {playerWon}");
        }

        /// <summary>
        /// Player attempts to flee combat.
        /// </summary>
        public bool TryFlee()
        {
            var engines = playerShip.GetSystem<EngineSystem>();
            if (engines == null || !engines.IsFTLReady)
            {
                Debug.Log("[CombatManager] FTL not ready, cannot flee");
                return false;
            }

            // Flee successful
            EndCombat(false); // Not a victory, but not a defeat either
            return true;
        }

        // ==================== COMBAT UPDATE ====================

        private void Update()
        {
            if (currentState != CombatState.Active)
                return;

            // Handle pause toggle
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePause();
            }

            if (isPaused)
                return;

            float deltaTime = Time.deltaTime;
            combatTime += deltaTime;

            // Update enemy AI
            enemyAI?.Tick(deltaTime);

            // Update projectiles
            UpdateProjectiles(deltaTime);

            // Check for combat end conditions
            CheckCombatEndConditions();

            // Update FTL charging
            var engines = playerShip.GetSystem<EngineSystem>();
            if (engines != null && !engines.IsChargingFTL)
            {
                engines.StartFTLCharge();
            }
        }

        private void UpdateProjectiles(float deltaTime)
        {
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = activeProjectiles[i];

                if (projectile == null || projectile.IsDestroyed)
                {
                    activeProjectiles.RemoveAt(i);
                    continue;
                }

                projectile.Tick(deltaTime);
            }
        }

        private void CheckCombatEndConditions()
        {
            // Check player defeat
            if (playerShip.IsDestroyed)
            {
                EndCombat(false);
                return;
            }

            // Check enemy defeat
            if (enemyShip != null && enemyShip.IsDestroyed)
            {
                EndCombat(true);
                return;
            }
        }

        // ==================== PAUSE CONTROL ====================

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;

            OnPauseToggled?.Invoke();

            if (isPaused)
                GameEvents.TriggerGamePaused();
            else
                GameEvents.TriggerGameResumed();
        }

        public void SetPaused(bool paused)
        {
            if (isPaused == paused) return;
            TogglePause();
        }

        // ==================== PROJECTILE MANAGEMENT ====================

        /// <summary>
        /// Spawns a projectile from a weapon.
        /// </summary>
        public void SpawnProjectile(Weapon weapon, Room targetRoom, bool isPlayerProjectile)
        {
            var projectileObj = new GameObject($"Projectile_{weapon.WeaponName}");
            projectileObj.transform.SetParent(projectileContainer);

            var projectile = projectileObj.AddComponent<Projectile>();
            projectile.Initialize(weapon, targetRoom, isPlayerProjectile, projectileSpeed);

            activeProjectiles.Add(projectile);

            projectile.OnHit += HandleProjectileHit;
            projectile.OnMiss += HandleProjectileMiss;
        }

        private void HandleProjectileHit(Projectile projectile, Room targetRoom)
        {
            var targetShip = targetRoom.GetComponentInParent<ShipController>();
            if (targetShip == null) return;

            // Apply damage
            var damage = projectile.CreateDamageInfo();
            targetShip.TakeDamageToRoom(targetRoom, damage);

            // Cleanup
            projectile.Destroy();
            activeProjectiles.Remove(projectile);
        }

        private void HandleProjectileMiss(Projectile projectile)
        {
            GameEvents.TriggerProjectileMissed(new ProjectileMissedArgs
            {
                targetShip = projectile.IsPlayerProjectile ? enemyShip : playerShip,
                evasionRoll = 0
            });

            projectile.Destroy();
            activeProjectiles.Remove(projectile);
        }

        private void ClearProjectiles()
        {
            foreach (var projectile in activeProjectiles)
            {
                if (projectile != null)
                {
                    projectile.Destroy();
                }
            }
            activeProjectiles.Clear();
        }

        // ==================== REWARDS ====================

        private int CalculateScrapReward()
        {
            int baseSector = GameManager.Instance?.CurrentRun?.State.currentSector ?? 1;
            int baseReward = 15 + (baseSector * 5);

            // Bonus for difficulty
            baseReward += enemyAI?.DifficultyRating ?? 0 * 3;

            // Random variance
            baseReward += UnityEngine.Random.Range(-5, 10);

            return Mathf.Max(10, baseReward);
        }

        private int CalculateMutationReward()
        {
            int baseReward = UnityEngine.Random.Range(2, 6);

            // Bonus for difficult fights
            baseReward += (enemyAI?.DifficultyRating ?? 0) / 2;

            return baseReward;
        }

        // ==================== FINAL BOSS ====================

        /// <summary>
        /// Starts the final boss encounter based on evolution path.
        /// </summary>
        public void StartFinalBattle(FinalBossType bossType)
        {
            Debug.Log($"[CombatManager] Starting final battle: {bossType}");

            switch (bossType)
            {
                case FinalBossType.Dreadnought:
                    StartDreadnoughtBattle();
                    break;

                case FinalBossType.Station:
                    // Station is handled by EventManager
                    break;

                case FinalBossType.Council:
                    // Council is handled by EventManager
                    break;
            }
        }

        private void StartDreadnoughtBattle()
        {
            // Create the Dreadnought - a massive 3-phase boss
            var bossObj = new GameObject("Dreadnought");
            bossObj.transform.SetParent(transform);

            var boss = bossObj.AddComponent<DreadnoughtBoss>();
            boss.Initialize(playerShip);

            // The Dreadnought handles its own combat flow
        }
    }

    public enum CombatState
    {
        None,
        Starting,
        Active,
        Ending
    }
}
