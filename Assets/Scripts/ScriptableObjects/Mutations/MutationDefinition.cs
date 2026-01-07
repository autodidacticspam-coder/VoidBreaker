using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining a mutation in the evolution tree.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMutation", menuName = "VoidBreaker/Mutation Definition")]
    public class MutationDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string mutationId;
        public string mutationName;
        [TextArea(2, 4)]
        public string description;

        [Header("Evolution Path")]
        public EvolutionPath path;
        public int tier; // 1-4
        public int cost;

        [Header("Prerequisites")]
        public string[] prerequisiteMutationIds;
        public int requiredPathLevel;

        [Header("Visuals")]
        public Sprite icon;
        public Sprite shipVisualModifier; // Overlay for ship appearance
        public Color glowColor = Color.white;

        [Header("Effects")]
        public MutationEffectDefinition[] effects;

        [Header("Special")]
        public bool isFinalMutation = false;
        public string unlocksEndingId;
    }

    [System.Serializable]
    public class MutationEffectDefinition
    {
        public MutationEffectType type;
        public float value;
        public bool isPercent;
        public string stringValue;
    }

    public enum MutationEffectType
    {
        // Combat (Predator)
        BonusDamage,
        CritChance,
        CritDamage,
        BypassShields,
        LifeSteal,
        BonusProjectiles,

        // Stealth (Phantom)
        BonusEvasion,
        CloakDuration,
        CloakCooldown,
        SensorEvasion,
        SilentRunning,
        AmbushDamage,

        // Diplomacy (Herald)
        ShopDiscount,
        ExtraRewards,
        CrewMorale,
        EventBonuses,
        ScrapBonus,
        ReputationGain,

        // Defense
        BonusShields,
        ShieldRecharge,
        BonusHull,
        DamageReduction,

        // Crew
        BonusCrew,
        CrewCombat,
        CrewRepair,
        CrewHealing,

        // Special
        UnlockSystem,
        UnlockWeapon,
        UnlockEnding,
        SpecialAbility
    }
}
