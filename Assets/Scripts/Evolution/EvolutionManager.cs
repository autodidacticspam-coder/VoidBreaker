using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Data;
using VoidBreaker.Ship;

namespace VoidBreaker.Evolution
{
    /// <summary>
    /// Manages ship evolution - the key differentiator of VoidBreaker.
    /// Tracks mutation points across three paths and handles visual/mechanical transformations.
    /// </summary>
    public class EvolutionManager : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private EvolutionState state;
        [SerializeField] private ShipController ship;

        [Header("Mutations")]
        [SerializeField] private MutationTree predatorTree;
        [SerializeField] private MutationTree phantomTree;
        [SerializeField] private MutationTree heraldTree;

        [Header("Visual Evolution")]
        [SerializeField] private ShipVisualEvolver visualEvolver;

        [Header("Active Mutations")]
        [SerializeField] private List<Mutation> activeMutations = new List<Mutation>();

        // Public accessors
        public EvolutionState State => state;
        public int TotalMutationPoints => GameManager.Instance?.CurrentRun?.State.mutationPoints ?? 0;
        public EvolutionPath DominantPath => state?.GetDominantPath() ?? EvolutionPath.None;
        public int PredatorPoints => state?.predatorPoints ?? 0;
        public int PhantomPoints => state?.phantomPoints ?? 0;
        public int HeraldPoints => state?.heraldPoints ?? 0;
        public IReadOnlyList<Mutation> ActiveMutations => activeMutations;

        // Tier 4 unlocks
        public bool HasApexPredator => HasMutation("apex_predator");
        public bool HasPhantomComplete => HasMutation("phantom_complete");
        public bool HasHeraldAscendant => HasMutation("herald_ascendant");

        // Events
        public event Action<EvolutionPath, int> OnPathPointsChanged;
        public event Action<Mutation> OnMutationActivated;
        public event Action<int> OnVisualStageChanged;

        public void Initialize(ShipController shipController, EvolutionState evolutionState)
        {
            ship = shipController;
            state = evolutionState ?? new EvolutionState();

            // Initialize mutation trees
            predatorTree = CreatePredatorTree();
            phantomTree = CreatePhantomTree();
            heraldTree = CreateHeraldTree();

            // Create visual evolver
            var visualObj = new GameObject("ShipVisualEvolver");
            visualObj.transform.SetParent(transform);
            visualEvolver = visualObj.AddComponent<ShipVisualEvolver>();
            visualEvolver.Initialize(ship);

            // Restore active mutations from state
            foreach (var mutationId in state.unlockedMutations)
            {
                var mutation = FindMutation(mutationId);
                if (mutation != null)
                {
                    activeMutations.Add(mutation);
                    ApplyMutationEffects(mutation);
                }
            }

            // Subscribe to events
            GameEvents.OnCombatEnded += HandleCombatEnded;
            GameEvents.OnEventChoiceMade += HandleEventChoice;

            Debug.Log($"[EvolutionManager] Initialized. Points: P:{PredatorPoints} Ph:{PhantomPoints} H:{HeraldPoints}");
        }

        private void OnDestroy()
        {
            GameEvents.OnCombatEnded -= HandleCombatEnded;
            GameEvents.OnEventChoiceMade -= HandleEventChoice;
        }

        // ==================== MUTATION POINT MANAGEMENT ====================

        /// <summary>
        /// Adds points to a specific evolution path.
        /// </summary>
        public void AddPathPoints(EvolutionPath path, int amount, string source = "")
        {
            int oldStage = GetVisualStage(path);

            switch (path)
            {
                case EvolutionPath.Predator:
                    state.predatorPoints += amount;
                    break;
                case EvolutionPath.Phantom:
                    state.phantomPoints += amount;
                    break;
                case EvolutionPath.Herald:
                    state.heraldPoints += amount;
                    break;
            }

            OnPathPointsChanged?.Invoke(path, GetPathPoints(path));

            // Check for visual evolution
            int newStage = GetVisualStage(path);
            if (newStage > oldStage && path == DominantPath)
            {
                TriggerVisualEvolution(path, newStage);
            }

            Debug.Log($"[Evolution] +{amount} {path} points ({source}). Total: {GetPathPoints(path)}");
        }

        public int GetPathPoints(EvolutionPath path)
        {
            return path switch
            {
                EvolutionPath.Predator => state.predatorPoints,
                EvolutionPath.Phantom => state.phantomPoints,
                EvolutionPath.Herald => state.heraldPoints,
                _ => 0
            };
        }

        public int GetVisualStage(EvolutionPath path)
        {
            return state.GetVisualStage(path);
        }

        public EvolutionPath GetDominantPath()
        {
            return state.GetDominantPath();
        }

        // ==================== MUTATION UNLOCKING ====================

        /// <summary>
        /// Attempts to unlock a mutation.
        /// Returns true if successful.
        /// </summary>
        public bool UnlockMutation(Mutation mutation)
        {
            if (mutation == null) return false;

            // Check if already unlocked
            if (HasMutation(mutation.MutationId))
            {
                Debug.Log($"[Evolution] Mutation {mutation.MutationName} already unlocked");
                return false;
            }

            // Check prerequisites
            if (!ArePrerequisitesMet(mutation))
            {
                Debug.Log($"[Evolution] Prerequisites not met for {mutation.MutationName}");
                return false;
            }

            // Check cost
            var runManager = GameManager.Instance?.CurrentRun;
            if (runManager == null || !runManager.SpendMutationPoints(mutation.Cost))
            {
                Debug.Log($"[Evolution] Cannot afford {mutation.MutationName} (costs {mutation.Cost})");
                return false;
            }

            // Unlock the mutation
            state.unlockedMutations.Add(mutation.MutationId);
            activeMutations.Add(mutation);

            // Add points to the path
            AddPathPoints(mutation.Path, mutation.Cost, mutation.MutationName);

            // Apply effects
            ApplyMutationEffects(mutation);

            OnMutationActivated?.Invoke(mutation);

            GameEvents.TriggerMutationUnlocked(new MutationUnlockedArgs
            {
                mutation = mutation,
                path = mutation.Path,
                tier = mutation.Tier
            });

            Debug.Log($"[Evolution] Unlocked mutation: {mutation.MutationName} (Tier {mutation.Tier})");

            return true;
        }

        public bool HasMutation(string mutationId)
        {
            return state.unlockedMutations.Contains(mutationId);
        }

        public bool ArePrerequisitesMet(Mutation mutation)
        {
            if (mutation.Prerequisites == null || mutation.Prerequisites.Length == 0)
                return true;

            foreach (var prereq in mutation.Prerequisites)
            {
                if (!HasMutation(prereq))
                    return false;
            }

            return true;
        }

        public bool CanUnlock(Mutation mutation)
        {
            if (HasMutation(mutation.MutationId)) return false;
            if (!ArePrerequisitesMet(mutation)) return false;
            if (TotalMutationPoints < mutation.Cost) return false;

            return true;
        }

        /// <summary>
        /// Gets all mutations available to unlock.
        /// </summary>
        public List<Mutation> GetAvailableMutations()
        {
            var available = new List<Mutation>();

            CheckTreeForAvailable(predatorTree, available);
            CheckTreeForAvailable(phantomTree, available);
            CheckTreeForAvailable(heraldTree, available);

            return available;
        }

        private void CheckTreeForAvailable(MutationTree tree, List<Mutation> available)
        {
            foreach (var mutation in tree.AllMutations)
            {
                if (CanUnlock(mutation))
                {
                    available.Add(mutation);
                }
            }
        }

        // ==================== MUTATION EFFECTS ====================

        private void ApplyMutationEffects(Mutation mutation)
        {
            if (ship == null) return;

            foreach (var effect in mutation.Effects)
            {
                ApplyEffect(effect);
            }
        }

        private void ApplyEffect(MutationEffect effect)
        {
            switch (effect.type)
            {
                case MutationEffectType.BonusHull:
                    // Increase max hull
                    ship.Heal(effect.value);
                    break;

                case MutationEffectType.BonusEvasion:
                    // Applied via modifier in evasion calculation
                    break;

                case MutationEffectType.BonusDamage:
                    // Applied via modifier in damage calculation
                    break;

                case MutationEffectType.WeaponSlot:
                    ship.GetSystem<WeaponControlSystem>()?.AddWeaponSlot();
                    break;

                case MutationEffectType.CrewSlot:
                    GameManager.Instance?.CurrentRun?.CrewManager?.IncreaseMaxCrew();
                    break;

                case MutationEffectType.CloakDuration:
                    // Modifier applied when cloak activates
                    break;

                case MutationEffectType.UnlockAbility:
                    // Ability is now available
                    break;
            }
        }

        /// <summary>
        /// Gets the total modifier for a stat from all active mutations.
        /// </summary>
        public float GetStatModifier(MutationEffectType statType)
        {
            float modifier = 0f;

            foreach (var mutation in activeMutations)
            {
                foreach (var effect in mutation.Effects)
                {
                    if (effect.type == statType)
                    {
                        modifier += effect.value;
                    }
                }
            }

            return modifier;
        }

        /// <summary>
        /// Checks if a specific ability is unlocked.
        /// </summary>
        public bool HasAbility(string abilityId)
        {
            foreach (var mutation in activeMutations)
            {
                foreach (var effect in mutation.Effects)
                {
                    if (effect.type == MutationEffectType.UnlockAbility &&
                        effect.abilityId == abilityId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // ==================== VISUAL EVOLUTION ====================

        private void TriggerVisualEvolution(EvolutionPath path, int newStage)
        {
            visualEvolver?.EvolveToStage(path, newStage);

            OnVisualStageChanged?.Invoke(newStage);

            GameEvents.TriggerShipEvolved(new ShipEvolvedArgs
            {
                newVisualStage = newStage,
                dominantPath = path
            });

            Debug.Log($"[Evolution] Ship evolved to stage {newStage} on {path} path!");
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleCombatEnded(CombatEndedArgs args)
        {
            if (!args.playerWon) return;

            // Grant mutation points based on combat performance
            // Base: 2-5 points
            int points = UnityEngine.Random.Range(2, 6);

            // Bonus for aggressive play
            // (would track kills, damage dealt, etc.)

            GameManager.Instance?.CurrentRun?.AddMutationPoints(points, "Combat victory");
        }

        private void HandleEventChoice(EventChoiceMadeArgs args)
        {
            // Some choices grant path-specific points
            // This would be configured in event definitions
        }

        // ==================== MUTATION TREE CREATION ====================

        private MutationTree CreatePredatorTree()
        {
            var tree = new MutationTree(EvolutionPath.Predator);

            // Tier 1
            tree.AddMutation(new Mutation
            {
                MutationId = "reinforced_hull",
                MutationName = "Reinforced Hull",
                Description = "Your ship grows thick, armored plating.",
                Path = EvolutionPath.Predator,
                Tier = 1,
                Cost = 10,
                Effects = new[] { new MutationEffect(MutationEffectType.BonusHull, 15) }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "weapon_ports",
                MutationName = "Weapon Ports",
                Description = "Biological weapon mounts emerge from the hull.",
                Path = EvolutionPath.Predator,
                Tier = 1,
                Cost = 10,
                Effects = new[] { new MutationEffect(MutationEffectType.WeaponSlot, 1) }
            });

            // Tier 2
            tree.AddMutation(new Mutation
            {
                MutationId = "bio_cannons",
                MutationName = "Bio-Cannons",
                Description = "Weapons fuse with the ship, dealing increased damage.",
                Path = EvolutionPath.Predator,
                Tier = 2,
                Cost = 25,
                Prerequisites = new[] { "reinforced_hull" },
                Effects = new[] { new MutationEffect(MutationEffectType.BonusDamage, 1) }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "armored_core",
                MutationName = "Armored Core",
                Description = "Critical systems are shielded by organic armor.",
                Path = EvolutionPath.Predator,
                Tier = 2,
                Cost = 25,
                Prerequisites = new[] { "weapon_ports" },
                Effects = new[] { new MutationEffect(MutationEffectType.DamageReduction, 1) }
            });

            // Tier 3
            tree.AddMutation(new Mutation
            {
                MutationId = "predators_maw",
                MutationName = "Predator's Maw",
                Description = "Your ship can ram enemy vessels, dealing damage and enabling boarding.",
                Path = EvolutionPath.Predator,
                Tier = 3,
                Cost = 50,
                Prerequisites = new[] { "bio_cannons", "armored_core" },
                Effects = new[] { new MutationEffect(MutationEffectType.UnlockAbility, "ram_attack") }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "blood_frenzy",
                MutationName = "Blood Frenzy",
                Description = "Destroying systems grants a temporary damage boost.",
                Path = EvolutionPath.Predator,
                Tier = 3,
                Cost = 50,
                Prerequisites = new[] { "bio_cannons" },
                Effects = new[] { new MutationEffect(MutationEffectType.UnlockAbility, "blood_frenzy") }
            });

            // Tier 4
            tree.AddMutation(new Mutation
            {
                MutationId = "apex_predator",
                MutationName = "APEX PREDATOR",
                Description = "You have become the ultimate hunter. Face the Dreadnought.",
                Path = EvolutionPath.Predator,
                Tier = 4,
                Cost = 100,
                Prerequisites = new[] { "predators_maw", "blood_frenzy" },
                Effects = new[]
                {
                    new MutationEffect(MutationEffectType.BonusDamage, 25, isPercent: true),
                    new MutationEffect(MutationEffectType.UnlockEnding, "dreadnought")
                }
            });

            return tree;
        }

        private MutationTree CreatePhantomTree()
        {
            var tree = new MutationTree(EvolutionPath.Phantom);

            // Tier 1
            tree.AddMutation(new Mutation
            {
                MutationId = "silent_running",
                MutationName = "Silent Running",
                Description = "Your ship becomes harder to detect.",
                Path = EvolutionPath.Phantom,
                Tier = 1,
                Cost = 10,
                Effects = new[] { new MutationEffect(MutationEffectType.BonusEvasion, 10) }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "sensor_dampening",
                MutationName = "Sensor Dampening",
                Description = "Enemies take longer to detect your presence.",
                Path = EvolutionPath.Phantom,
                Tier = 1,
                Cost = 10,
                Effects = new[] { new MutationEffect(MutationEffectType.DetectionDelay, 2) }
            });

            // Tier 2
            tree.AddMutation(new Mutation
            {
                MutationId = "phase_plating",
                MutationName = "Phase Plating",
                Description = "20% chance to phase through incoming damage.",
                Path = EvolutionPath.Phantom,
                Tier = 2,
                Cost = 25,
                Prerequisites = new[] { "silent_running" },
                Effects = new[] { new MutationEffect(MutationEffectType.PhaseChance, 20) }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "ghost_crew",
                MutationName = "Ghost Crew",
                Description = "Crew move silently and faster.",
                Path = EvolutionPath.Phantom,
                Tier = 2,
                Cost = 25,
                Prerequisites = new[] { "sensor_dampening" },
                Effects = new[] { new MutationEffect(MutationEffectType.CrewSpeed, 25, isPercent: true) }
            });

            // Tier 3
            tree.AddMutation(new Mutation
            {
                MutationId = "void_cloak",
                MutationName = "Void Cloak",
                Description = "Cloak duration increased by 50%.",
                Path = EvolutionPath.Phantom,
                Tier = 3,
                Cost = 50,
                Prerequisites = new[] { "phase_plating" },
                Effects = new[] { new MutationEffect(MutationEffectType.CloakDuration, 50, isPercent: true) }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "system_vampire",
                MutationName = "System Vampire",
                Description = "Hacking drains enemy power to your ship.",
                Path = EvolutionPath.Phantom,
                Tier = 3,
                Cost = 50,
                Prerequisites = new[] { "ghost_crew" },
                Effects = new[] { new MutationEffect(MutationEffectType.UnlockAbility, "system_vampire") }
            });

            // Tier 4
            tree.AddMutation(new Mutation
            {
                MutationId = "phantom_complete",
                MutationName = "PHANTOM COMPLETE",
                Description = "You have become one with the void. Infiltrate the Station.",
                Path = EvolutionPath.Phantom,
                Tier = 4,
                Cost = 100,
                Prerequisites = new[] { "void_cloak", "system_vampire" },
                Effects = new[]
                {
                    new MutationEffect(MutationEffectType.StartCloaked, 1),
                    new MutationEffect(MutationEffectType.UnlockEnding, "station")
                }
            });

            return tree;
        }

        private MutationTree CreateHeraldTree()
        {
            var tree = new MutationTree(EvolutionPath.Herald);

            // Tier 1
            tree.AddMutation(new Mutation
            {
                MutationId = "broadcast_array",
                MutationName = "Broadcast Array",
                Description = "+1 diplomacy option in events.",
                Path = EvolutionPath.Herald,
                Tier = 1,
                Cost = 10,
                Effects = new[] { new MutationEffect(MutationEffectType.BonusDiplomacy, 1) }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "welcoming_bays",
                MutationName = "Welcoming Bays",
                Description = "+1 maximum crew capacity.",
                Path = EvolutionPath.Herald,
                Tier = 1,
                Cost = 10,
                Effects = new[] { new MutationEffect(MutationEffectType.CrewSlot, 1) }
            });

            // Tier 2
            tree.AddMutation(new Mutation
            {
                MutationId = "empathic_field",
                MutationName = "Empathic Field",
                Description = "Crew relationships improve faster.",
                Path = EvolutionPath.Herald,
                Tier = 2,
                Cost = 25,
                Prerequisites = new[] { "broadcast_array" },
                Effects = new[] { new MutationEffect(MutationEffectType.RelationshipBonus, 50, isPercent: true) }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "trade_beacon",
                MutationName = "Trade Beacon",
                Description = "Better prices at shops.",
                Path = EvolutionPath.Herald,
                Tier = 2,
                Cost = 25,
                Prerequisites = new[] { "welcoming_bays" },
                Effects = new[] { new MutationEffect(MutationEffectType.ShopDiscount, 15, isPercent: true) }
            });

            // Tier 3
            tree.AddMutation(new Mutation
            {
                MutationId = "sanctuary",
                MutationName = "Sanctuary",
                Description = "Some hostile ships will not attack unprovoked.",
                Path = EvolutionPath.Herald,
                Tier = 3,
                Cost = 50,
                Prerequisites = new[] { "empathic_field" },
                Effects = new[] { new MutationEffect(MutationEffectType.UnlockAbility, "sanctuary") }
            });

            tree.AddMutation(new Mutation
            {
                MutationId = "united_crew",
                MutationName = "United Crew",
                Description = "Crew bonuses stack with each other.",
                Path = EvolutionPath.Herald,
                Tier = 3,
                Cost = 50,
                Prerequisites = new[] { "trade_beacon" },
                Effects = new[] { new MutationEffect(MutationEffectType.UnlockAbility, "united_crew") }
            });

            // Tier 4
            tree.AddMutation(new Mutation
            {
                MutationId = "herald_ascendant",
                MutationName = "HERALD ASCENDANT",
                Description = "Your voice carries across the stars. Face the Council.",
                Path = EvolutionPath.Herald,
                Tier = 4,
                Cost = 100,
                Prerequisites = new[] { "sanctuary", "united_crew" },
                Effects = new[]
                {
                    new MutationEffect(MutationEffectType.StartWithAlly, 1),
                    new MutationEffect(MutationEffectType.UnlockEnding, "council")
                }
            });

            return tree;
        }

        private Mutation FindMutation(string mutationId)
        {
            var mutation = predatorTree?.GetMutation(mutationId);
            if (mutation != null) return mutation;

            mutation = phantomTree?.GetMutation(mutationId);
            if (mutation != null) return mutation;

            return heraldTree?.GetMutation(mutationId);
        }

        // ==================== SAVE/LOAD ====================

        public void LoadFromSave(EvolutionState savedState)
        {
            state = savedState;

            // Reapply all mutations
            activeMutations.Clear();
            foreach (var mutationId in state.unlockedMutations)
            {
                var mutation = FindMutation(mutationId);
                if (mutation != null)
                {
                    activeMutations.Add(mutation);
                    ApplyMutationEffects(mutation);
                }
            }

            // Update visuals
            var dominant = DominantPath;
            int stage = GetVisualStage(dominant);
            visualEvolver?.EvolveToStage(dominant, stage);
        }
    }
}
