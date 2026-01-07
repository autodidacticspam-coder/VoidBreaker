using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Data;

namespace VoidBreaker.Evolution
{
    /// <summary>
    /// Represents a single mutation that can be unlocked.
    /// </summary>
    [Serializable]
    public class Mutation
    {
        public string MutationId;
        public string MutationName;
        [TextArea(2, 4)]
        public string Description;

        public EvolutionPath Path;
        public int Tier; // 1-4
        public int Cost; // Mutation points required

        public string[] Prerequisites; // Mutation IDs that must be unlocked first
        public MutationEffect[] Effects;

        // For display
        public Sprite Icon;

        public bool IsCapstone => Tier == 4;

        public string GetTierLabel()
        {
            return Tier switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                _ => Tier.ToString()
            };
        }

        public Color GetPathColor()
        {
            return Path switch
            {
                EvolutionPath.Predator => new Color(0.8f, 0.2f, 0.2f), // Red
                EvolutionPath.Phantom => new Color(0.2f, 0.3f, 0.8f), // Blue
                EvolutionPath.Herald => new Color(0.9f, 0.8f, 0.2f), // Gold
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// A single effect granted by a mutation.
    /// </summary>
    [Serializable]
    public struct MutationEffect
    {
        public MutationEffectType type;
        public float value;
        public bool isPercent;
        public string abilityId; // For UnlockAbility type

        public MutationEffect(MutationEffectType type, float value, bool isPercent = false)
        {
            this.type = type;
            this.value = value;
            this.isPercent = isPercent;
            this.abilityId = null;
        }

        public MutationEffect(MutationEffectType type, string abilityId)
        {
            this.type = type;
            this.value = 0;
            this.isPercent = false;
            this.abilityId = abilityId;
        }

        public string GetDescription()
        {
            string valueStr = isPercent ? $"{value}%" : value.ToString();

            return type switch
            {
                MutationEffectType.BonusHull => $"+{valueStr} Max Hull",
                MutationEffectType.BonusEvasion => $"+{valueStr} Evasion",
                MutationEffectType.BonusDamage => isPercent ? $"+{valueStr} Damage" : $"+{value} Damage per shot",
                MutationEffectType.DamageReduction => $"-{value} Damage taken",
                MutationEffectType.WeaponSlot => $"+{value} Weapon Slot",
                MutationEffectType.CrewSlot => $"+{value} Crew Capacity",
                MutationEffectType.CloakDuration => $"+{valueStr} Cloak Duration",
                MutationEffectType.PhaseChance => $"{valueStr} chance to phase through damage",
                MutationEffectType.CrewSpeed => $"+{valueStr} Crew Speed",
                MutationEffectType.DetectionDelay => $"+{value}s before enemy detection",
                MutationEffectType.BonusDiplomacy => $"+{value} Diplomacy options",
                MutationEffectType.ShopDiscount => $"{valueStr} shop discount",
                MutationEffectType.RelationshipBonus => $"+{valueStr} relationship gain",
                MutationEffectType.UnlockAbility => $"Unlocks: {abilityId}",
                MutationEffectType.UnlockEnding => $"Unlocks ending: {abilityId}",
                MutationEffectType.StartCloaked => "Start combat cloaked",
                MutationEffectType.StartWithAlly => "Start final battle with ally",
                _ => $"{type}: {valueStr}"
            };
        }
    }

    // MutationEffectType enum is defined in VoidBreaker.Data.MutationDefinition

    /// <summary>
    /// Contains all mutations for a single evolution path.
    /// </summary>
    [Serializable]
    public class MutationTree
    {
        public EvolutionPath Path;
        public List<Mutation> Tier1 = new List<Mutation>();
        public List<Mutation> Tier2 = new List<Mutation>();
        public List<Mutation> Tier3 = new List<Mutation>();
        public List<Mutation> Tier4 = new List<Mutation>(); // Capstone

        private Dictionary<string, Mutation> mutationLookup = new Dictionary<string, Mutation>();

        public IEnumerable<Mutation> AllMutations
        {
            get
            {
                foreach (var m in Tier1) yield return m;
                foreach (var m in Tier2) yield return m;
                foreach (var m in Tier3) yield return m;
                foreach (var m in Tier4) yield return m;
            }
        }

        public MutationTree(EvolutionPath path)
        {
            Path = path;
        }

        public void AddMutation(Mutation mutation)
        {
            var tierList = mutation.Tier switch
            {
                1 => Tier1,
                2 => Tier2,
                3 => Tier3,
                4 => Tier4,
                _ => Tier1
            };

            tierList.Add(mutation);
            mutationLookup[mutation.MutationId] = mutation;
        }

        public Mutation GetMutation(string mutationId)
        {
            return mutationLookup.TryGetValue(mutationId, out var mutation) ? mutation : null;
        }

        public List<Mutation> GetTier(int tier)
        {
            return tier switch
            {
                1 => Tier1,
                2 => Tier2,
                3 => Tier3,
                4 => Tier4,
                _ => new List<Mutation>()
            };
        }
    }
}
