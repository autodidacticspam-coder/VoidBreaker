using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Events;

namespace VoidBreaker.Combat
{
    /// <summary>
    /// The Dreadnought - Final boss for the Predator evolution path.
    /// A massive 3-phase boss that tests pure combat mastery.
    /// </summary>
    public class DreadnoughtBoss : MonoBehaviour
    {
        [Header("Boss State")]
        [SerializeField] private DreadnoughtPhase currentPhase = DreadnoughtPhase.Phase1;
        [SerializeField] private int phaseHealth = 100;
        [SerializeField] private int maxPhaseHealth = 100;
        [SerializeField] private bool isTransitioning = false;

        [Header("References")]
        [SerializeField] private ShipController bossShip;
        [SerializeField] private ShipController playerShip;

        [Header("Phase 1 - Artillery Barrage")]
        [SerializeField] private float artilleryInterval = 3f;
        [SerializeField] private float artilleryTimer = 0f;
        [SerializeField] private int artilleryDamage = 3;

        [Header("Phase 2 - Drone Swarm")]
        [SerializeField] private List<DroneUnit> activeDrones = new List<DroneUnit>();
        [SerializeField] private int maxDrones = 6;
        [SerializeField] private float droneSpawnInterval = 5f;
        [SerializeField] private float droneTimer = 0f;

        [Header("Phase 3 - Desperate Assault")]
        [SerializeField] private bool shieldsOvercharged = false;
        [SerializeField] private float overchargeTimer = 0f;
        [SerializeField] private float overchargeDuration = 10f;
        [SerializeField] private int boardingWaves = 0;

        [Header("Visuals")]
        [SerializeField] private float bossScale = 3f;

        // Events
        public event Action<DreadnoughtPhase> OnPhaseChanged;
        public event Action OnBossDefeated;

        public void Initialize(ShipController player)
        {
            playerShip = player;

            // Create the Dreadnought ship
            CreateDreadnoughtShip();

            // Start phase 1
            currentPhase = DreadnoughtPhase.Phase1;
            phaseHealth = GetPhaseMaxHealth(currentPhase);
            maxPhaseHealth = phaseHealth;

            Debug.Log("[DreadnoughtBoss] DREADNOUGHT AWAKENS - Phase 1 begins!");
            GameEvents.TriggerBossPhaseChanged(new BossPhaseChangedArgs
            {
                bossName = "THE DREADNOUGHT",
                phase = 1,
                maxPhases = 3
            });
        }

        private void CreateDreadnoughtShip()
        {
            // Create boss ship controller
            var shipObj = new GameObject("DreadnoughtShip");
            shipObj.transform.SetParent(transform);
            shipObj.transform.localScale = Vector3.one * bossScale;

            bossShip = shipObj.AddComponent<ShipController>();

            // Create a custom ship definition for the Dreadnought
            var definition = ScriptableObject.CreateInstance<ShipDefinition>();
            definition.shipName = "THE DREADNOUGHT";
            definition.description = "The ultimate weapon of the Hegemony. A ship-killer of legendary proportions.";
            definition.maxHull = 50; // Per phase
            definition.startingCrew = 12;
            definition.maxPower = 20;
            definition.startingWeaponSlots = 8;

            bossShip.Initialize(definition, false);

            // Add devastating weapons
            SetupPhase1Weapons();
        }

        private void SetupPhase1Weapons()
        {
            var weapons = bossShip.GetSystem<WeaponControlSystem>();
            if (weapons == null) return;

            // Artillery cannons - high damage, slow fire
            for (int i = 0; i < 4; i++)
            {
                var weaponDef = ScriptableObject.CreateInstance<WeaponDefinition>();
                weaponDef.weaponName = $"Artillery Cannon {i + 1}";
                weaponDef.weaponType = WeaponType.Laser;
                weaponDef.damage = 3;
                weaponDef.chargeTime = 8f;
                weaponDef.powerCost = 2;
                weaponDef.projectileCount = 2;

                weapons.InstallWeapon(weaponDef, i);
            }
        }

        private void Update()
        {
            if (bossShip == null || bossShip.IsDestroyed || isTransitioning)
                return;

            float deltaTime = Time.deltaTime;

            switch (currentPhase)
            {
                case DreadnoughtPhase.Phase1:
                    UpdatePhase1(deltaTime);
                    break;
                case DreadnoughtPhase.Phase2:
                    UpdatePhase2(deltaTime);
                    break;
                case DreadnoughtPhase.Phase3:
                    UpdatePhase3(deltaTime);
                    break;
            }

            // Check phase transition
            if (bossShip.CurrentHealth <= 0 && !isTransitioning)
            {
                TransitionToNextPhase();
            }
        }

        // ==================== PHASE 1: ARTILLERY BARRAGE ====================

        private void UpdatePhase1(float deltaTime)
        {
            artilleryTimer += deltaTime;

            if (artilleryTimer >= artilleryInterval)
            {
                artilleryTimer = 0f;
                FireArtilleryBarrage();
            }
        }

        private void FireArtilleryBarrage()
        {
            // Fire artillery at random rooms
            var rooms = playerShip.Rooms;
            if (rooms.Count == 0) return;

            int shotsToFire = 2;
            for (int i = 0; i < shotsToFire; i++)
            {
                var targetRoom = rooms[UnityEngine.Random.Range(0, rooms.Count)];

                // Artillery bypasses shields 50% of the time
                bool bypassShields = UnityEngine.Random.value > 0.5f;

                var damage = new DamageInfo
                {
                    amount = artilleryDamage,
                    type = DamageType.Missile,
                    ignoresShields = bypassShields,
                    causesBreach = UnityEngine.Random.value < 0.2f, // 20% breach
                    causesFire = UnityEngine.Random.value < 0.15f   // 15% fire
                };

                playerShip.TakeDamageToRoom(targetRoom, damage);

                Debug.Log($"[Dreadnought] Artillery strikes {targetRoom.RoomType}!");
            }

            GameEvents.TriggerBossAttack(new BossAttackArgs
            {
                attackName = "Artillery Barrage",
                damage = artilleryDamage * shotsToFire
            });
        }

        // ==================== PHASE 2: DRONE SWARM ====================

        private void UpdatePhase2(float deltaTime)
        {
            droneTimer += deltaTime;

            // Spawn drones periodically
            if (droneTimer >= droneSpawnInterval && activeDrones.Count < maxDrones)
            {
                droneTimer = 0f;
                SpawnDrone();
            }

            // Update existing drones
            for (int i = activeDrones.Count - 1; i >= 0; i--)
            {
                var drone = activeDrones[i];
                if (drone == null || drone.IsDestroyed)
                {
                    activeDrones.RemoveAt(i);
                    continue;
                }

                drone.Tick(deltaTime);
            }

            // Reduced artillery in phase 2
            artilleryTimer += deltaTime;
            if (artilleryTimer >= artilleryInterval * 2f)
            {
                artilleryTimer = 0f;
                FireArtilleryBarrage();
            }
        }

        private void SpawnDrone()
        {
            var droneObj = new GameObject($"Drone_{activeDrones.Count}");
            droneObj.transform.SetParent(transform);

            var drone = droneObj.AddComponent<DroneUnit>();
            drone.Initialize(playerShip, DroneType.Combat, 2);

            activeDrones.Add(drone);

            Debug.Log("[Dreadnought] Combat drone deployed!");

            GameEvents.TriggerBossAttack(new BossAttackArgs
            {
                attackName = "Drone Deployment",
                damage = 0
            });
        }

        // ==================== PHASE 3: DESPERATE ASSAULT ====================

        private void UpdatePhase3(float deltaTime)
        {
            // Overcharge shields periodically
            overchargeTimer += deltaTime;
            if (!shieldsOvercharged && overchargeTimer >= overchargeDuration)
            {
                OverchargeShields();
            }
            else if (shieldsOvercharged && overchargeTimer >= overchargeDuration)
            {
                shieldsOvercharged = false;
                overchargeTimer = 0f;
            }

            // Aggressive artillery
            artilleryTimer += deltaTime;
            if (artilleryTimer >= artilleryInterval * 0.5f)
            {
                artilleryTimer = 0f;
                FireArtilleryBarrage();
            }

            // Periodic boarding attempts
            if (boardingWaves < 3 && bossShip.CurrentHealth < maxPhaseHealth * 0.5f)
            {
                AttemptBoarding();
                boardingWaves++;
            }
        }

        private void OverchargeShields()
        {
            shieldsOvercharged = true;
            overchargeTimer = 0f;

            var shields = bossShip.GetSystem<ShieldSystem>();
            if (shields != null)
            {
                // Max out shields
                shields.ForceSetLayers(shields.MaxLayers);
            }

            Debug.Log("[Dreadnought] SHIELDS OVERCHARGED!");

            GameEvents.TriggerBossAttack(new BossAttackArgs
            {
                attackName = "Shield Overcharge",
                damage = 0
            });
        }

        private void AttemptBoarding()
        {
            // Find a room to board
            var rooms = playerShip.Rooms;
            if (rooms.Count == 0) return;

            var targetRoom = rooms[UnityEngine.Random.Range(0, rooms.Count)];

            // Spawn enemy crew in that room
            var crewManager = playerShip.GetComponent<CrewManager>();
            if (crewManager != null)
            {
                // Spawn 2-3 hostile crew
                int count = UnityEngine.Random.Range(2, 4);
                for (int i = 0; i < count; i++)
                {
                    crewManager.SpawnHostileCrew(targetRoom, CrewRace.Human);
                }
            }

            Debug.Log($"[Dreadnought] BOARDING PARTY to {targetRoom.RoomType}!");

            GameEvents.TriggerBossAttack(new BossAttackArgs
            {
                attackName = "Boarding Party",
                damage = 0
            });
        }

        // ==================== PHASE TRANSITIONS ====================

        private void TransitionToNextPhase()
        {
            isTransitioning = true;

            switch (currentPhase)
            {
                case DreadnoughtPhase.Phase1:
                    StartCoroutine(TransitionSequence(DreadnoughtPhase.Phase2));
                    break;

                case DreadnoughtPhase.Phase2:
                    StartCoroutine(TransitionSequence(DreadnoughtPhase.Phase3));
                    break;

                case DreadnoughtPhase.Phase3:
                    // Boss defeated!
                    StartCoroutine(DefeatSequence());
                    break;
            }
        }

        private IEnumerator TransitionSequence(DreadnoughtPhase nextPhase)
        {
            Debug.Log($"[Dreadnought] Transitioning to {nextPhase}...");

            // Brief invulnerability during transition
            yield return new WaitForSeconds(2f);

            currentPhase = nextPhase;
            phaseHealth = GetPhaseMaxHealth(nextPhase);
            maxPhaseHealth = phaseHealth;

            // Heal ship for new phase
            bossShip.RepairHull(phaseHealth);

            // Setup phase-specific changes
            switch (nextPhase)
            {
                case DreadnoughtPhase.Phase2:
                    SetupPhase2();
                    break;
                case DreadnoughtPhase.Phase3:
                    SetupPhase3();
                    break;
            }

            isTransitioning = false;

            OnPhaseChanged?.Invoke(nextPhase);

            GameEvents.TriggerBossPhaseChanged(new BossPhaseChangedArgs
            {
                bossName = "THE DREADNOUGHT",
                phase = (int)nextPhase + 1,
                maxPhases = 3
            });

            Debug.Log($"[Dreadnought] {nextPhase} BEGINS!");
        }

        private void SetupPhase2()
        {
            // Clear any phase 1 specific state
            artilleryInterval = 6f; // Slower artillery

            // Enable drone bay
            droneTimer = 0f;
        }

        private void SetupPhase3()
        {
            // Desperate mode
            artilleryInterval = 1.5f; // Very fast artillery
            artilleryDamage = 4; // Higher damage

            // Destroy remaining drones
            foreach (var drone in activeDrones)
            {
                if (drone != null)
                    Destroy(drone.gameObject);
            }
            activeDrones.Clear();
        }

        private IEnumerator DefeatSequence()
        {
            Debug.Log("[Dreadnought] THE DREADNOUGHT IS DESTROYED!");

            // Dramatic pause
            yield return new WaitForSeconds(1f);

            // Destroy remaining drones
            foreach (var drone in activeDrones)
            {
                if (drone != null)
                    Destroy(drone.gameObject);
            }
            activeDrones.Clear();

            OnBossDefeated?.Invoke();

            GameEvents.TriggerBossDefeated(new BossDefeatedArgs
            {
                bossName = "THE DREADNOUGHT",
                evolutionPath = EvolutionPath.Predator
            });

            // Trigger victory
            var combatManager = FindObjectOfType<CombatManager>();
            if (combatManager != null)
            {
                combatManager.EndCombat(true);
            }

            // Trigger ending sequence
            GameEvents.TriggerGameEnding(new GameEndingArgs
            {
                endingType = EndingType.PredatorVictory,
                endingTitle = "APEX PREDATOR",
                endingDescription = "The Dreadnought falls. The Hegemony's ultimate weapon lies in ruins. " +
                    "Your ship has become the most feared vessel in the galaxy. " +
                    "None dare oppose you now. The stars belong to the hunter."
            });

            Destroy(gameObject);
        }

        private int GetPhaseMaxHealth(DreadnoughtPhase phase)
        {
            return phase switch
            {
                DreadnoughtPhase.Phase1 => 40,
                DreadnoughtPhase.Phase2 => 50,
                DreadnoughtPhase.Phase3 => 60,
                _ => 50
            };
        }

        public void TakeDamage(int amount)
        {
            if (isTransitioning) return;

            bossShip.TakeDamage(amount);
        }
    }

    public enum DreadnoughtPhase
    {
        Phase1, // Artillery Barrage
        Phase2, // Drone Swarm
        Phase3  // Desperate Assault
    }

    /// <summary>
    /// Simple drone unit for Phase 2.
    /// </summary>
    public class DroneUnit : MonoBehaviour
    {
        private ShipController target;
        private DroneType droneType;
        private int damage;
        private float attackInterval = 4f;
        private float attackTimer = 0f;
        private int health = 2;

        public bool IsDestroyed => health <= 0;

        public void Initialize(ShipController targetShip, DroneType type, int dmg)
        {
            target = targetShip;
            droneType = type;
            damage = dmg;
        }

        public void Tick(float deltaTime)
        {
            if (IsDestroyed) return;

            attackTimer += deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                Attack();
            }
        }

        private void Attack()
        {
            if (target == null || target.Rooms.Count == 0) return;

            var room = target.Rooms[UnityEngine.Random.Range(0, target.Rooms.Count)];

            var damageInfo = new DamageInfo
            {
                amount = damage,
                type = DamageType.Laser,
                ignoresShields = droneType == DroneType.Boarding
            };

            target.TakeDamageToRoom(room, damageInfo);
        }

        public void TakeDamage(int amount)
        {
            health -= amount;
            if (health <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public enum DroneType
    {
        Combat,
        Defense,
        Boarding,
        Repair
    }
}
