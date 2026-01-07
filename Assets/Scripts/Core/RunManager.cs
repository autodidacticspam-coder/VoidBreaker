using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Manages the state of a single run.
    /// Created when a run starts, destroyed when it ends.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        [Header("Run State")]
        [SerializeField] private RunState state;

        [Header("Managers")]
        private ShipController playerShip;
        private CrewManager crewManager;
        private SectorManager sectorManager;
        private EvolutionManager evolutionManager;
        private CombatManager combatManager;
        private EventManager eventManager;

        // Public accessors
        public RunState State => state;
        public ShipController PlayerShip => playerShip;
        public CrewManager CrewManager => crewManager;
        public SectorManager SectorManager => sectorManager;
        public EvolutionManager EvolutionManager => evolutionManager;

        public float RunDuration => Time.time - runStartTime;
        private float runStartTime;

        // Pity timer tracking
        private bool hasReceivedWeapon;
        private bool hasReceivedCrew;
        private bool hasReceivedShieldUpgrade;

        public void Initialize(ShipDefinition shipDef, int layoutIndex, MetaProgressionData meta)
        {
            runStartTime = Time.time;

            // Initialize state
            state = new RunState
            {
                shipDefinitionId = shipDef.name,
                layoutIndex = layoutIndex,
                currentSector = 1,
                currentBeacon = 0,

                // Starting resources
                scrap = 30 + (meta.HasGeneMemory("prepared") ? 25 : 0),
                fuel = 16,
                missiles = 8,
                droneParts = 2,
                mutationPoints = meta.HasGeneMemory("evolved") ? 10 : 0,

                // Evolution state
                evolution = new EvolutionState(),

                // Statistics
                statistics = new RunStatistics()
            };

            // Apply gene memory bonuses
            ApplyGeneMemories(meta);

            // Create subsystems
            CreateManagers();

            // Initialize ship from definition
            playerShip.Initialize(shipDef, layoutIndex);

            // Generate first sector
            sectorManager.GenerateSector(1);

            Debug.Log($"[RunManager] Run initialized with {shipDef.shipName}");
        }

        public void LoadFromSave(RunSaveData saveData)
        {
            runStartTime = Time.time - saveData.runDuration;
            state = saveData.state;

            CreateManagers();

            // Restore from save data
            playerShip.LoadFromSave(saveData.shipState);
            crewManager.LoadFromSave(saveData.crewStates);
            sectorManager.LoadFromSave(saveData.sectorState);
            evolutionManager.LoadFromSave(state.evolution);

            Debug.Log("[RunManager] Run loaded from save");
        }

        private void CreateManagers()
        {
            // Create ship
            var shipObject = new GameObject("PlayerShip");
            shipObject.transform.SetParent(transform);
            playerShip = shipObject.AddComponent<ShipController>();

            // Create crew manager
            var crewObject = new GameObject("CrewManager");
            crewObject.transform.SetParent(transform);
            crewManager = crewObject.AddComponent<CrewManager>();

            // Create sector manager
            var sectorObject = new GameObject("SectorManager");
            sectorObject.transform.SetParent(transform);
            sectorManager = sectorObject.AddComponent<SectorManager>();

            // Create evolution manager
            var evoObject = new GameObject("EvolutionManager");
            evoObject.transform.SetParent(transform);
            evolutionManager = evoObject.AddComponent<EvolutionManager>();

            // Create combat manager
            var combatObject = new GameObject("CombatManager");
            combatObject.transform.SetParent(transform);
            combatManager = combatObject.AddComponent<CombatManager>();

            // Create event manager
            var eventObject = new GameObject("EventManager");
            eventObject.transform.SetParent(transform);
            eventManager = eventObject.AddComponent<EventManager>();

            // Wire up dependencies
            combatManager.Initialize(playerShip);
            eventManager.Initialize(this);
            evolutionManager.Initialize(playerShip, state.evolution);
        }

        private void ApplyGeneMemories(MetaProgressionData meta)
        {
            // Apply unlocked gene memory bonuses
            if (meta.HasGeneMemory("veteran_crew"))
            {
                // Will spawn with one trained crew
                state.flags.Add("bonus_trained_crew");
            }

            if (meta.HasGeneMemory("weapons_expert"))
            {
                state.flags.Add("bonus_random_weapon");
            }

            if (meta.HasGeneMemory("survivor"))
            {
                state.bonusHull = 5;
            }
        }

        private void Start()
        {
            // Subscribe to events
            GameEvents.OnCombatEnded += HandleCombatEnded;
            GameEvents.OnBeaconReached += HandleBeaconReached;
            GameEvents.OnResourcesChanged += HandleResourcesChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnCombatEnded -= HandleCombatEnded;
            GameEvents.OnBeaconReached -= HandleBeaconReached;
            GameEvents.OnResourcesChanged -= HandleResourcesChanged;
        }

        // ==================== RESOURCE MANAGEMENT ====================

        public bool SpendScrap(int amount)
        {
            if (state.scrap < amount) return false;

            state.scrap -= amount;
            GameEvents.TriggerResourcesChanged(new ResourcesChangedArgs
            {
                resource = ResourceType.Scrap,
                amount = -amount,
                newTotal = state.scrap
            });
            return true;
        }

        public void AddScrap(int amount)
        {
            state.scrap += amount;
            state.statistics.totalScrapEarned += amount;

            GameEvents.TriggerResourcesChanged(new ResourcesChangedArgs
            {
                resource = ResourceType.Scrap,
                amount = amount,
                newTotal = state.scrap
            });
        }

        public bool SpendFuel(int amount = 1)
        {
            if (state.fuel < amount) return false;

            state.fuel -= amount;
            GameEvents.TriggerResourcesChanged(new ResourcesChangedArgs
            {
                resource = ResourceType.Fuel,
                amount = -amount,
                newTotal = state.fuel
            });
            return true;
        }

        public void AddFuel(int amount)
        {
            state.fuel += amount;
            GameEvents.TriggerResourcesChanged(new ResourcesChangedArgs
            {
                resource = ResourceType.Fuel,
                amount = amount,
                newTotal = state.fuel
            });
        }

        public bool SpendMissiles(int amount)
        {
            if (state.missiles < amount) return false;

            state.missiles -= amount;
            GameEvents.TriggerResourcesChanged(new ResourcesChangedArgs
            {
                resource = ResourceType.Missiles,
                amount = -amount,
                newTotal = state.missiles
            });
            return true;
        }

        public void AddMissiles(int amount)
        {
            state.missiles += amount;
            GameEvents.TriggerResourcesChanged(new ResourcesChangedArgs
            {
                resource = ResourceType.Missiles,
                amount = amount,
                newTotal = state.missiles
            });
        }

        public void AddMutationPoints(int amount, string source = "")
        {
            state.mutationPoints += amount;
            state.statistics.totalMutationPoints += amount;

            GameEvents.TriggerMutationPointsChanged(new MutationPointsChangedArgs
            {
                currentPoints = state.mutationPoints,
                change = amount,
                source = source
            });
        }

        public bool SpendMutationPoints(int amount)
        {
            if (state.mutationPoints < amount) return false;

            state.mutationPoints -= amount;
            GameEvents.TriggerMutationPointsChanged(new MutationPointsChangedArgs
            {
                currentPoints = state.mutationPoints,
                change = -amount,
                source = "mutation_purchase"
            });
            return true;
        }

        // ==================== PITY TIMER SYSTEM ====================

        public DraftOption[] GetPityAdjustedDraft(DraftOption[] originalOptions)
        {
            var options = new List<DraftOption>(originalOptions);

            // Sector 2+: Guarantee weapon if none received
            if (state.currentSector >= 2 && !hasReceivedWeapon)
            {
                if (!options.Exists(o => o.type == DraftOptionType.Weapon))
                {
                    // Replace worst option with weapon
                    options[0] = GenerateWeaponDraft();
                }
            }

            // Sector 2+: Guarantee crew if only 1
            if (state.currentSector >= 2 && !hasReceivedCrew && crewManager.CrewCount <= 2)
            {
                if (!options.Exists(o => o.type == DraftOptionType.Crew))
                {
                    options[1] = GenerateCrewDraft();
                }
            }

            // Sector 3+: Guarantee shield if not upgraded
            if (state.currentSector >= 3 && !hasReceivedShieldUpgrade)
            {
                if (playerShip.GetSystem(SystemType.Shields).CurrentLevel < 4)
                {
                    // Add shield booster augment option
                }
            }

            return options.ToArray();
        }

        private DraftOption GenerateWeaponDraft()
        {
            // Generate appropriate weapon for current sector
            return new DraftOption
            {
                type = DraftOptionType.Weapon,
                displayName = "Burst Laser II",
                description = "A reliable 3-shot laser weapon"
                // weaponDef would be assigned
            };
        }

        private DraftOption GenerateCrewDraft()
        {
            return new DraftOption
            {
                type = DraftOptionType.Crew,
                displayName = "Human Refugee",
                description = "A survivor eager to join your crew"
            };
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleCombatEnded(CombatEndedArgs args)
        {
            if (args.playerWon)
            {
                state.statistics.enemiesDefeated++;
                AddScrap(args.scrapReward);
                AddMutationPoints(args.mutationPointsReward, "combat");
            }
        }

        private void HandleBeaconReached(BeaconReachedArgs args)
        {
            state.currentBeacon = args.beacon.Index;
            state.statistics.beaconsVisited++;
        }

        private void HandleResourcesChanged(ResourcesChangedArgs args)
        {
            // Track for pity timer
            if (args.resource == ResourceType.Scrap)
            {
                // Check if weapon/crew was purchased
            }
        }

        // ==================== SECTOR TRANSITIONS ====================

        public void AdvanceToNextSector()
        {
            state.currentSector++;

            if (state.currentSector > 8)
            {
                // Reached final sector - trigger ending based on evolution path
                TriggerFinalEncounter();
                return;
            }

            sectorManager.GenerateSector(state.currentSector);

            GameEvents.TriggerSectorChanged(new SectorChangedArgs
            {
                newSector = state.currentSector,
                sectorType = sectorManager.CurrentSectorType
            });

            Debug.Log($"[RunManager] Advanced to sector {state.currentSector}");
        }

        private void TriggerFinalEncounter()
        {
            var dominantPath = evolutionManager.GetDominantPath();

            Debug.Log($"[RunManager] Final encounter triggered. Dominant path: {dominantPath}");

            // The final boss depends on evolution path
            switch (dominantPath)
            {
                case EvolutionPath.Predator:
                    // Dreadnought battle
                    combatManager.StartFinalBattle(FinalBossType.Dreadnought);
                    break;

                case EvolutionPath.Phantom:
                    // Station infiltration
                    eventManager.TriggerFinalEvent(FinalBossType.Station);
                    break;

                case EvolutionPath.Herald:
                    // Council negotiation
                    eventManager.TriggerFinalEvent(FinalBossType.Council);
                    break;

                default:
                    // Default to combat
                    combatManager.StartFinalBattle(FinalBossType.Dreadnought);
                    break;
            }
        }

        // ==================== SAVE/LOAD ====================

        public RunSaveData GetSaveData()
        {
            return new RunSaveData
            {
                state = state,
                runDuration = RunDuration,
                shipState = playerShip.GetSaveData(),
                crewStates = crewManager.GetSaveData(),
                sectorState = sectorManager.GetSaveData()
            };
        }

        public RunStatistics GetRunStatistics()
        {
            return state.statistics;
        }
    }

    [Serializable]
    public class RunState
    {
        public string shipDefinitionId;
        public int layoutIndex;

        // Sector progress
        public int currentSector;
        public int currentBeacon;

        // Resources
        public int scrap;
        public int fuel;
        public int missiles;
        public int droneParts;
        public int mutationPoints;

        // Evolution
        public EvolutionState evolution;

        // Flags for events/pity timer
        public HashSet<string> flags = new HashSet<string>();
        public HashSet<string> completedEvents = new HashSet<string>();

        // Bonuses from gene memories
        public int bonusHull;

        // Run statistics
        public RunStatistics statistics;
    }

    [Serializable]
    public class EvolutionState
    {
        public int predatorPoints;
        public int phantomPoints;
        public int heraldPoints;

        public List<string> unlockedMutations = new List<string>();

        public int TotalPoints => predatorPoints + phantomPoints + heraldPoints;

        public EvolutionPath GetDominantPath()
        {
            if (predatorPoints >= phantomPoints && predatorPoints >= heraldPoints)
                return EvolutionPath.Predator;
            if (phantomPoints >= predatorPoints && phantomPoints >= heraldPoints)
                return EvolutionPath.Phantom;
            return EvolutionPath.Herald;
        }

        public int GetVisualStage(EvolutionPath path)
        {
            int points = path switch
            {
                EvolutionPath.Predator => predatorPoints,
                EvolutionPath.Phantom => phantomPoints,
                EvolutionPath.Herald => heraldPoints,
                _ => 0
            };

            if (points >= 185) return 4;
            if (points >= 85) return 3;
            if (points >= 35) return 2;
            if (points >= 10) return 1;
            return 0;
        }
    }

    [Serializable]
    public class RunStatistics
    {
        public int enemiesDefeated;
        public int beaconsVisited;
        public int crewLost;
        public int totalScrapEarned;
        public int totalMutationPoints;
        public int damageDealt;
        public int damageTaken;
        public int shotsEvaded;
    }

    [Serializable]
    public class RunSaveData
    {
        public RunState state;
        public float runDuration;
        public ShipSaveData shipState;
        public List<CrewSaveData> crewStates;
        public SectorSaveData sectorState;
    }

    public enum EvolutionPath
    {
        None,
        Predator,
        Phantom,
        Herald
    }

    public enum FinalBossType
    {
        Dreadnought,
        Station,
        Council
    }
}
