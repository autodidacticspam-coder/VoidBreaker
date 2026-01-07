using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Evolution;
using VoidBreaker.Sector;

namespace VoidBreaker.Events
{
    /// <summary>
    /// Manages the draft/reward selection system.
    /// After encounters, players choose 1 of 3 rewards.
    /// </summary>
    public class RewardDrafter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int optionsPerDraft = 3;

        [Header("Weights")]
        [SerializeField] private float scrapWeight = 0.3f;
        [SerializeField] private float weaponWeight = 0.15f;
        [SerializeField] private float augmentWeight = 0.1f;
        [SerializeField] private float crewWeight = 0.1f;
        [SerializeField] private float resourceWeight = 0.2f;
        [SerializeField] private float mutationPointWeight = 0.15f;

        // Current draft state
        private RewardOption[] currentOptions;
        private RewardContext currentContext;
        private bool isDraftActive = false;

        // Events
        public event Action<RewardOption[]> OnDraftPresented;
        public event Action<RewardOption> OnDraftChosen;

        /// <summary>
        /// Generates draft options based on context.
        /// </summary>
        public RewardOption[] GenerateDraftOptions(RewardContext context)
        {
            currentContext = context;
            var options = new List<RewardOption>();

            // Adjust weights based on context
            var adjustedWeights = AdjustWeights(context);

            // Generate options
            for (int i = 0; i < optionsPerDraft; i++)
            {
                var option = GenerateSingleOption(adjustedWeights, options);
                if (option != null)
                {
                    options.Add(option);
                }
            }

            currentOptions = options.ToArray();
            isDraftActive = true;

            OnDraftPresented?.Invoke(currentOptions);

            Debug.Log($"[RewardDrafter] Generated {currentOptions.Length} reward options");

            return currentOptions;
        }

        /// <summary>
        /// Player selects an option from the draft.
        /// </summary>
        public void SelectOption(int index)
        {
            if (!isDraftActive || index < 0 || index >= currentOptions.Length)
            {
                Debug.LogWarning("[RewardDrafter] Invalid draft selection");
                return;
            }

            var chosen = currentOptions[index];
            ApplyReward(chosen);

            isDraftActive = false;

            OnDraftChosen?.Invoke(chosen);

            Debug.Log($"[RewardDrafter] Player chose: {chosen.displayName}");
        }

        /// <summary>
        /// Skips the draft (takes nothing).
        /// </summary>
        public void SkipDraft()
        {
            isDraftActive = false;
            Debug.Log("[RewardDrafter] Player skipped draft");
        }

        // ==================== WEIGHT ADJUSTMENT ====================

        private Dictionary<RewardOptionType, float> AdjustWeights(RewardContext context)
        {
            var weights = new Dictionary<RewardOptionType, float>
            {
                { RewardOptionType.Scrap, scrapWeight },
                { RewardOptionType.Weapon, weaponWeight },
                { RewardOptionType.Augment, augmentWeight },
                { RewardOptionType.Crew, crewWeight },
                { RewardOptionType.Resources, resourceWeight },
                { RewardOptionType.MutationPoints, mutationPointWeight }
            };

            var runManager = GameManager.Instance?.CurrentRun;
            if (runManager == null) return weights;

            // Adjust based on player needs
            var state = runManager.State;

            // Low fuel? Increase resource weight
            if (state.fuel < 5)
            {
                weights[RewardOptionType.Resources] *= 1.5f;
            }

            // Low scrap? Increase scrap weight
            if (state.scrap < 20)
            {
                weights[RewardOptionType.Scrap] *= 1.3f;
            }

            // Few weapons? Increase weapon weight
            var ship = runManager.PlayerShip;
            if (ship != null)
            {
                var weaponControl = ship.GetSystem<Ship.WeaponControlSystem>();
                if (weaponControl != null && weaponControl.Weapons.Count < 2)
                {
                    weights[RewardOptionType.Weapon] *= 2f;
                }
            }

            // Small crew? Increase crew weight
            if (runManager.CrewManager != null && runManager.CrewManager.CrewCount < 3)
            {
                weights[RewardOptionType.Crew] *= 1.5f;
            }

            // Evolution path bonus
            var evolution = runManager.EvolutionManager;
            if (evolution != null)
            {
                var dominant = evolution.DominantPath;
                if (dominant == EvolutionPath.Predator)
                {
                    weights[RewardOptionType.Weapon] *= 1.2f;
                }
                else if (dominant == EvolutionPath.Herald)
                {
                    weights[RewardOptionType.Crew] *= 1.2f;
                }
            }

            // Sector difficulty affects quality
            if (context.sectorNumber >= 5)
            {
                weights[RewardOptionType.Weapon] *= 1.3f;
                weights[RewardOptionType.Augment] *= 1.3f;
            }

            return weights;
        }

        // ==================== OPTION GENERATION ====================

        private RewardOption GenerateSingleOption(Dictionary<RewardOptionType, float> weights,
            List<RewardOption> existingOptions)
        {
            // Normalize weights
            float total = 0f;
            foreach (var w in weights.Values) total += w;

            // Roll for type
            float roll = UnityEngine.Random.value * total;
            float cumulative = 0f;
            RewardOptionType selectedType = RewardOptionType.Scrap;

            foreach (var kvp in weights)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                {
                    selectedType = kvp.Key;
                    break;
                }
            }

            // Avoid duplicates
            int attempts = 0;
            while (attempts < 10)
            {
                var option = CreateOption(selectedType);
                if (option != null && !IsDuplicate(option, existingOptions))
                {
                    return option;
                }

                // Try a different type
                roll = UnityEngine.Random.value * total;
                cumulative = 0f;
                foreach (var kvp in weights)
                {
                    cumulative += kvp.Value;
                    if (roll <= cumulative)
                    {
                        selectedType = kvp.Key;
                        break;
                    }
                }

                attempts++;
            }

            // Fallback to scrap
            return CreateScrapOption();
        }

        private bool IsDuplicate(RewardOption option, List<RewardOption> existing)
        {
            foreach (var e in existing)
            {
                if (e.type == option.type && e.displayName == option.displayName)
                    return true;
            }
            return false;
        }

        private RewardOption CreateOption(RewardOptionType type)
        {
            return type switch
            {
                RewardOptionType.Scrap => CreateScrapOption(),
                RewardOptionType.Weapon => CreateWeaponOption(),
                RewardOptionType.Augment => CreateAugmentOption(),
                RewardOptionType.Crew => CreateCrewOption(),
                RewardOptionType.Resources => CreateResourceOption(),
                RewardOptionType.MutationPoints => CreateMutationPointsOption(),
                _ => CreateScrapOption()
            };
        }

        private RewardOption CreateScrapOption()
        {
            int sector = currentContext.sectorNumber;
            int baseScrap = 15 + (sector * 5);
            int variance = UnityEngine.Random.Range(-5, 10);
            int amount = baseScrap + variance;

            return new RewardOption
            {
                type = RewardOptionType.Scrap,
                displayName = $"{amount} Scrap",
                description = "Raw materials for repairs and upgrades",
                scrapAmount = amount
            };
        }

        private RewardOption CreateWeaponOption()
        {
            // In full implementation, would pull from weapon database
            var weapons = new[]
            {
                ("Burst Laser II", "Fast 3-shot laser"),
                ("Heavy Laser", "Slow but powerful single shot"),
                ("Artemis Missile", "Ignores shields, uses ammo"),
                ("Ion Blast II", "Disables enemy systems"),
                ("Flak I", "Shotgun-style shield breaker"),
                ("Pike Beam", "Long-range beam weapon"),
            };

            var (name, desc) = weapons[UnityEngine.Random.Range(0, weapons.Length)];

            return new RewardOption
            {
                type = RewardOptionType.Weapon,
                displayName = name,
                description = desc,
                weaponId = name.ToLower().Replace(" ", "_")
            };
        }

        private RewardOption CreateAugmentOption()
        {
            var augments = new[]
            {
                ("Titanium Hull", "+15% hull integrity"),
                ("Long-Range Scanners", "Reveal more beacons"),
                ("Scrap Recovery Arm", "+10% scrap from battles"),
                ("Automated Re-loader", "Weapons charge 10% faster"),
                ("Shield Charger", "Shields recharge 15% faster"),
            };

            var (name, desc) = augments[UnityEngine.Random.Range(0, augments.Length)];

            return new RewardOption
            {
                type = RewardOptionType.Augment,
                displayName = name,
                description = desc,
                augmentId = name.ToLower().Replace(" ", "_")
            };
        }

        private RewardOption CreateCrewOption()
        {
            var races = new[]
            {
                (CrewRace.Human, "Human", "Learns skills faster"),
                (CrewRace.Vex, "Vex Warrior", "Deals 50% more combat damage"),
                (CrewRace.Engi, "Engi", "Repairs 50% faster"),
                (CrewRace.Zoltan, "Zoltan", "Provides +1 power to manned system"),
                (CrewRace.Slug, "Slug", "Telepathy reveals adjacent rooms"),
            };

            var (race, name, desc) = races[UnityEngine.Random.Range(0, races.Length)];

            return new RewardOption
            {
                type = RewardOptionType.Crew,
                displayName = name,
                description = desc,
                crewRace = race
            };
        }

        private RewardOption CreateResourceOption()
        {
            int type = UnityEngine.Random.Range(0, 3);

            return type switch
            {
                0 => new RewardOption
                {
                    type = RewardOptionType.Resources,
                    displayName = "Fuel Reserves",
                    description = "+5 fuel",
                    fuelAmount = 5
                },
                1 => new RewardOption
                {
                    type = RewardOptionType.Resources,
                    displayName = "Missile Cache",
                    description = "+4 missiles",
                    missileAmount = 4
                },
                _ => new RewardOption
                {
                    type = RewardOptionType.Resources,
                    displayName = "Supply Package",
                    description = "+3 fuel, +2 missiles, +1 drone part",
                    fuelAmount = 3,
                    missileAmount = 2,
                    dronePartAmount = 1
                }
            };
        }

        private RewardOption CreateMutationPointsOption()
        {
            int amount = UnityEngine.Random.Range(5, 12);

            return new RewardOption
            {
                type = RewardOptionType.MutationPoints,
                displayName = $"{amount} Mutation Points",
                description = "Fuel your ship's evolution",
                mutationPointAmount = amount
            };
        }

        // ==================== REWARD APPLICATION ====================

        private void ApplyReward(RewardOption option)
        {
            var runManager = GameManager.Instance?.CurrentRun;
            if (runManager == null) return;

            switch (option.type)
            {
                case RewardOptionType.Scrap:
                    runManager.AddScrap(option.scrapAmount);
                    break;

                case RewardOptionType.Weapon:
                    // Would add weapon to ship
                    Debug.Log($"[Reward] Adding weapon: {option.weaponId}");
                    break;

                case RewardOptionType.Augment:
                    // Would add augment
                    Debug.Log($"[Reward] Adding augment: {option.augmentId}");
                    break;

                case RewardOptionType.Crew:
                    // Would hire crew
                    Debug.Log($"[Reward] Hiring crew: {option.crewRace}");
                    break;

                case RewardOptionType.Resources:
                    if (option.fuelAmount > 0)
                        runManager.AddFuel(option.fuelAmount);
                    if (option.missileAmount > 0)
                        runManager.AddMissiles(option.missileAmount);
                    // Drone parts would be similar
                    break;

                case RewardOptionType.MutationPoints:
                    runManager.AddMutationPoints(option.mutationPointAmount, "draft_reward");
                    break;
            }
        }
    }

    // ==================== DATA STRUCTURES ====================

    [Serializable]
    public class RewardOption
    {
        public RewardOptionType type;
        public string displayName;
        public string description;

        // Type-specific data
        public int scrapAmount;
        public string weaponId;
        public string augmentId;
        public CrewRace crewRace;
        public int fuelAmount;
        public int missileAmount;
        public int dronePartAmount;
        public int mutationPointAmount;
    }

    public enum RewardOptionType
    {
        Scrap,
        Weapon,
        Augment,
        Crew,
        Resources,
        MutationPoints
    }

    [Serializable]
    public struct RewardContext
    {
        public int sectorNumber;
        public BeaconType encounterType;
        public bool wasVictory;
        public int difficultyRating;
    }
}
