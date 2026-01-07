using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining an augment (ship upgrade).
    /// </summary>
    [CreateAssetMenu(fileName = "NewAugment", menuName = "VoidBreaker/Augment Definition")]
    public class AugmentDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string augmentId;
        public string augmentName;
        [TextArea(2, 4)]
        public string description;
        public AugmentRarity rarity = AugmentRarity.Common;

        [Header("Visuals")]
        public Sprite icon;
        public Color glowColor = Color.white;

        [Header("Effects")]
        public AugmentEffect[] effects;

        [Header("Requirements")]
        public SystemType? requiredSystem;
        public int requiredSystemLevel;

        [Header("Shop")]
        public int basePrice = 50;
        public bool canBePurchased = true;
        public bool isUnique = true; // Can only have one

        [Header("Special Flags")]
        public bool affectsEvolution = false;
        public EvolutionPath? evolutionPathBonus;
        public int evolutionBonus = 0;
    }

    [System.Serializable]
    public class AugmentEffect
    {
        public AugmentEffectType type;
        public float value;
        public bool isPercent;
        public SystemType? targetSystem;
    }

    public enum AugmentEffectType
    {
        // Combat
        WeaponDamageBonus,
        WeaponChargeSpeed,
        ShieldRechargeSpeed,
        EvasionBonus,
        CritChance,

        // Defense
        MaxHull,
        MaxShieldLayers,
        DamageReduction,

        // Crew
        CrewCombatBonus,
        CrewRepairSpeed,
        CrewMoveSpeed,
        CrewHealingRate,

        // Systems
        SystemPowerEfficiency,
        OxygenProduction,
        SensorRange,
        CloakDuration,
        TeleportCapacity,

        // Resources
        ScrapBonus,
        FuelEfficiency,
        MissileCapacity,
        DroneCapacity,

        // Special
        LongRangeScanners,
        AutomaticDoors,
        ReverseFTL,
        CloakOnJump,
        PreIgniter
    }

    // AugmentRarity enum is defined in VoidBreaker.Core.CommonTypes
}
