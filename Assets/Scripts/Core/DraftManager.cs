using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Data;
using VoidBreaker.Utilities;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Manages the draft/choice system for rewards.
    /// Instead of pure RNG, players choose 1 of 3 options.
    /// Includes pity timer for rare drops.
    /// </summary>
    public class DraftManager : MonoBehaviour
    {
        private static DraftManager instance;
        public static DraftManager Instance => instance;

        [Header("Draft Settings")]
        [SerializeField] private int optionCount = 3;
        [SerializeField] private float rarityBonusPerPity = 0.05f;
        [SerializeField] private int maxPityCounter = 20;

        [Header("Rarity Weights")]
        [SerializeField] private float commonWeight = 60f;
        [SerializeField] private float uncommonWeight = 30f;
        [SerializeField] private float rareWeight = 9f;
        [SerializeField] private float legendaryWeight = 1f;

        // Pity counters
        private int weaponPityCounter = 0;
        private int augmentPityCounter = 0;
        private int crewPityCounter = 0;

        // Exclusion lists (items already owned or seen this run)
        private HashSet<string> excludedWeapons = new HashSet<string>();
        private HashSet<string> excludedAugments = new HashSet<string>();

        // Events
        public event Action<DraftOptions> OnDraftStarted;
        public event Action<DraftResult> OnDraftCompleted;

        private DraftOptions currentDraft;
        private ContentDatabase contentDatabase;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Start()
        {
            contentDatabase = GameBootstrap.Content;
        }

        // ==================== DRAFT GENERATION ====================

        /// <summary>
        /// Starts a weapon draft after combat victory.
        /// </summary>
        public DraftOptions StartWeaponDraft(int sectorNumber)
        {
            var options = new List<DraftOption>();

            // Get available weapons
            var availableWeapons = GetAvailableWeapons();

            // Generate options with rarity consideration
            for (int i = 0; i < optionCount && availableWeapons.Count > 0; i++)
            {
                var rarity = RollRarity(weaponPityCounter);
                var weapon = SelectWeaponOfRarity(availableWeapons, rarity);

                if (weapon != null)
                {
                    options.Add(new DraftOption
                    {
                        Type = DraftOptionType.Weapon,
                        ItemId = weapon.weaponId,
                        Name = weapon.weaponName,
                        Description = weapon.description,
                        Icon = weapon.icon,
                        Rarity = ConvertRarity(weapon.rarity)
                    });

                    availableWeapons.Remove(weapon);
                }
            }

            // Add scrap option as fallback
            int scrapAmount = CalculateScrapValue(sectorNumber);
            options.Add(new DraftOption
            {
                Type = DraftOptionType.Scrap,
                ItemId = "scrap",
                Name = $"{scrapAmount} Scrap",
                Description = "Take the scrap instead.",
                Rarity = DraftRarity.Common,
                IntValue = scrapAmount
            });

            currentDraft = new DraftOptions
            {
                DraftType = DraftType.CombatReward,
                Options = options
            };

            OnDraftStarted?.Invoke(currentDraft);
            GameEvents.TriggerDraftStart(new DraftStartArgs { Options = currentDraft });

            return currentDraft;
        }

        /// <summary>
        /// Starts an augment draft from shops or special events.
        /// </summary>
        public DraftOptions StartAugmentDraft(int sectorNumber)
        {
            var options = new List<DraftOption>();

            var availableAugments = GetAvailableAugments();

            for (int i = 0; i < optionCount && availableAugments.Count > 0; i++)
            {
                var rarity = RollRarity(augmentPityCounter);
                var augment = SelectAugmentOfRarity(availableAugments, rarity);

                if (augment != null)
                {
                    options.Add(new DraftOption
                    {
                        Type = DraftOptionType.Augment,
                        ItemId = augment.augmentId,
                        Name = augment.augmentName,
                        Description = augment.description,
                        Icon = augment.icon,
                        Rarity = ConvertRarity(augment.rarity)
                    });

                    availableAugments.Remove(augment);
                }
            }

            currentDraft = new DraftOptions
            {
                DraftType = DraftType.AugmentReward,
                Options = options
            };

            OnDraftStarted?.Invoke(currentDraft);
            return currentDraft;
        }

        /// <summary>
        /// Starts a crew recruitment draft.
        /// </summary>
        public DraftOptions StartCrewDraft(int sectorNumber)
        {
            var options = new List<DraftOption>();

            if (contentDatabase == null)
                return null;

            var availableRaces = new List<RaceDefinition>(contentDatabase.crewRaces);
            availableRaces.Shuffle();

            for (int i = 0; i < optionCount && availableRaces.Count > 0; i++)
            {
                var race = availableRaces[0];
                availableRaces.RemoveAt(0);

                // Generate random crew name
                string crewName = GenerateCrewName(race.race);

                options.Add(new DraftOption
                {
                    Type = DraftOptionType.Crew,
                    ItemId = race.raceId,
                    Name = $"{crewName} ({race.raceName})",
                    Description = race.description,
                    Icon = race.icon,
                    Rarity = DraftRarity.Common,
                    StringValue = crewName
                });
            }

            currentDraft = new DraftOptions
            {
                DraftType = DraftType.CrewRecruitment,
                Options = options
            };

            OnDraftStarted?.Invoke(currentDraft);
            return currentDraft;
        }

        /// <summary>
        /// Starts a mutation draft when evolution points are available.
        /// </summary>
        public DraftOptions StartMutationDraft(EvolutionPath path, int tier)
        {
            var options = new List<DraftOption>();

            if (contentDatabase == null)
                return null;

            var pathMutations = contentDatabase.GetMutationsByPath(path);
            var tierMutations = new List<MutationDefinition>();

            foreach (var mutation in pathMutations)
            {
                if (mutation.tier == tier)
                {
                    tierMutations.Add(mutation);
                }
            }

            tierMutations.Shuffle();

            int count = Mathf.Min(optionCount, tierMutations.Count);
            for (int i = 0; i < count; i++)
            {
                var mutation = tierMutations[i];
                options.Add(new DraftOption
                {
                    Type = DraftOptionType.Mutation,
                    ItemId = mutation.mutationId,
                    Name = mutation.mutationName,
                    Description = mutation.description,
                    Icon = mutation.icon,
                    Rarity = mutation.isFinalMutation ? DraftRarity.Legendary : (DraftRarity)(tier - 1),
                    IntValue = mutation.cost
                });
            }

            currentDraft = new DraftOptions
            {
                DraftType = DraftType.Evolution,
                Options = options
            };

            OnDraftStarted?.Invoke(currentDraft);
            return currentDraft;
        }

        // ==================== DRAFT SELECTION ====================

        /// <summary>
        /// Called when player selects a draft option.
        /// </summary>
        public DraftResult SelectOption(int optionIndex)
        {
            if (currentDraft == null || optionIndex < 0 || optionIndex >= currentDraft.Options.Count)
            {
                Debug.LogWarning("Invalid draft selection!");
                return null;
            }

            var selected = currentDraft.Options[optionIndex];
            var result = new DraftResult
            {
                SelectedOption = selected,
                DraftType = currentDraft.DraftType
            };

            // Apply the selection
            ApplyDraftResult(result);

            // Update pity counter
            UpdatePityCounter(selected);

            // Add to exclusion list if applicable
            AddToExclusion(selected);

            OnDraftCompleted?.Invoke(result);
            GameEvents.TriggerDraftComplete(new DraftCompleteArgs { Result = result });

            currentDraft = null;
            return result;
        }

        /// <summary>
        /// Skip the draft without selecting (if allowed).
        /// </summary>
        public void SkipDraft()
        {
            if (currentDraft == null)
                return;

            var result = new DraftResult
            {
                SelectedOption = null,
                DraftType = currentDraft.DraftType,
                WasSkipped = true
            };

            OnDraftCompleted?.Invoke(result);
            currentDraft = null;
        }

        private void ApplyDraftResult(DraftResult result)
        {
            var option = result.SelectedOption;
            var gameManager = GameManager.Instance;

            switch (option.Type)
            {
                case DraftOptionType.Weapon:
                    gameManager?.AddWeapon(option.ItemId);
                    break;

                case DraftOptionType.Augment:
                    gameManager?.AddAugment(option.ItemId);
                    break;

                case DraftOptionType.Crew:
                    gameManager?.AddCrew(option.ItemId, option.StringValue);
                    break;

                case DraftOptionType.Scrap:
                    gameManager?.AddScrap(option.IntValue);
                    break;

                case DraftOptionType.Mutation:
                    gameManager?.UnlockMutation(option.ItemId);
                    break;

                case DraftOptionType.Fuel:
                    gameManager?.AddFuel(option.IntValue);
                    break;
            }
        }

        // ==================== RARITY SYSTEM ====================

        private DraftRarity RollRarity(int pityCounter)
        {
            // Pity bonus increases rare/legendary chances
            float pityBonus = pityCounter * rarityBonusPerPity;

            float adjustedRare = rareWeight + (pityBonus * 2);
            float adjustedLegendary = legendaryWeight + pityBonus;

            float totalWeight = commonWeight + uncommonWeight + adjustedRare + adjustedLegendary;
            float roll = UnityEngine.Random.Range(0f, totalWeight);

            if (roll < adjustedLegendary)
                return DraftRarity.Legendary;
            if (roll < adjustedLegendary + adjustedRare)
                return DraftRarity.Rare;
            if (roll < adjustedLegendary + adjustedRare + uncommonWeight)
                return DraftRarity.Uncommon;

            return DraftRarity.Common;
        }

        private void UpdatePityCounter(DraftOption selected)
        {
            switch (selected.Type)
            {
                case DraftOptionType.Weapon:
                    if (selected.Rarity >= DraftRarity.Rare)
                        weaponPityCounter = 0;
                    else
                        weaponPityCounter = Mathf.Min(weaponPityCounter + 1, maxPityCounter);
                    break;

                case DraftOptionType.Augment:
                    if (selected.Rarity >= DraftRarity.Rare)
                        augmentPityCounter = 0;
                    else
                        augmentPityCounter = Mathf.Min(augmentPityCounter + 1, maxPityCounter);
                    break;
            }
        }

        // ==================== HELPERS ====================

        private List<WeaponDefinition> GetAvailableWeapons()
        {
            var available = new List<WeaponDefinition>();

            if (contentDatabase == null)
                return available;

            foreach (var weapon in contentDatabase.weapons)
            {
                if (!excludedWeapons.Contains(weapon.weaponId))
                {
                    available.Add(weapon);
                }
            }

            return available;
        }

        private List<AugmentDefinition> GetAvailableAugments()
        {
            var available = new List<AugmentDefinition>();

            if (contentDatabase == null)
                return available;

            foreach (var augment in contentDatabase.augments)
            {
                if (!excludedAugments.Contains(augment.augmentId))
                {
                    available.Add(augment);
                }
            }

            return available;
        }

        private WeaponDefinition SelectWeaponOfRarity(List<WeaponDefinition> weapons, DraftRarity targetRarity)
        {
            var matching = weapons.FindAll(w => ConvertRarity(w.rarity) == targetRarity);

            if (matching.Count > 0)
                return matching.GetRandom();

            // Fallback to any weapon
            return weapons.Count > 0 ? weapons.GetRandom() : null;
        }

        private AugmentDefinition SelectAugmentOfRarity(List<AugmentDefinition> augments, DraftRarity targetRarity)
        {
            var matching = augments.FindAll(a => ConvertRarity(a.rarity) == targetRarity);

            if (matching.Count > 0)
                return matching.GetRandom();

            return augments.Count > 0 ? augments.GetRandom() : null;
        }

        private DraftRarity ConvertRarity(WeaponRarity rarity)
        {
            return rarity switch
            {
                WeaponRarity.Common => DraftRarity.Common,
                WeaponRarity.Uncommon => DraftRarity.Uncommon,
                WeaponRarity.Rare => DraftRarity.Rare,
                WeaponRarity.Legendary => DraftRarity.Legendary,
                _ => DraftRarity.Common
            };
        }

        private DraftRarity ConvertRarity(AugmentRarity rarity)
        {
            return rarity switch
            {
                AugmentRarity.Common => DraftRarity.Common,
                AugmentRarity.Uncommon => DraftRarity.Uncommon,
                AugmentRarity.Rare => DraftRarity.Rare,
                AugmentRarity.Legendary => DraftRarity.Legendary,
                _ => DraftRarity.Common
            };
        }

        private void AddToExclusion(DraftOption option)
        {
            switch (option.Type)
            {
                case DraftOptionType.Weapon:
                    excludedWeapons.Add(option.ItemId);
                    break;
                case DraftOptionType.Augment:
                    excludedAugments.Add(option.ItemId);
                    break;
            }
        }

        private int CalculateScrapValue(int sectorNumber)
        {
            int baseScrap = 15 + (sectorNumber * 5);
            return UnityEngine.Random.Range(baseScrap, baseScrap + 10);
        }

        private string GenerateCrewName(CrewRace race)
        {
            // Simple name generation
            string[] humanNames = { "Alex", "Jordan", "Casey", "Morgan", "Riley", "Sam", "Taylor", "Quinn" };
            string[] alienSuffixes = { "'tok", "xen", "vir", "zik", "orn", "eth" };

            if (race == CrewRace.Human)
            {
                return humanNames.GetRandom();
            }
            else
            {
                char firstLetter = (char)UnityEngine.Random.Range('A', 'Z');
                return firstLetter + alienSuffixes.GetRandom();
            }
        }

        /// <summary>
        /// Reset exclusions for a new run.
        /// </summary>
        public void ResetForNewRun()
        {
            excludedWeapons.Clear();
            excludedAugments.Clear();
            weaponPityCounter = 0;
            augmentPityCounter = 0;
            crewPityCounter = 0;
        }
    }

    // ==================== DATA STRUCTURES ====================

    public class DraftOptions
    {
        public DraftType DraftType;
        public List<DraftOption> Options = new List<DraftOption>();
    }

    public class DraftOption
    {
        public DraftOptionType Type;
        public string ItemId;
        public string Name;
        public string Description;
        public Sprite Icon;
        public DraftRarity Rarity;
        public int IntValue;
        public string StringValue;
    }

    public class DraftResult
    {
        public DraftOption SelectedOption;
        public DraftType DraftType;
        public bool WasSkipped;
    }

    public enum DraftType
    {
        CombatReward,
        AugmentReward,
        CrewRecruitment,
        Evolution,
        Special
    }

    public enum DraftOptionType
    {
        Weapon,
        Augment,
        Crew,
        Scrap,
        Fuel,
        Missiles,
        Mutation,
        Special
    }

    public enum DraftRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
}
