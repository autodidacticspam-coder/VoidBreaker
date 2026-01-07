using System;
using UnityEngine;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Common types used across the VoidBreaker codebase.
    /// </summary>

    // ==================== DAMAGE SYSTEM ====================

    /// <summary>
    /// Types of damage that can be dealt to ships, systems, and crew.
    /// </summary>
    public enum DamageType
    {
        Normal,
        Fire,
        Ion,
        Breach,
        Suffocation,
        Combat,
        Laser,
        Missile,
        Beam,
        Bomb,
        Boarding
    }

    /// <summary>
    /// Information about a damage instance.
    /// </summary>
    [Serializable]
    public struct DamageInfo
    {
        public int amount;
        public DamageType type;
        public bool ignoresShields;
        public bool causesBreach;
        public bool causesFire;
        public int ionDamage;
        public int systemDamage;
        public float stunDuration;

        public DamageInfo(int amount, DamageType type = DamageType.Normal)
        {
            this.amount = amount;
            this.type = type;
            this.ignoresShields = false;
            this.causesBreach = false;
            this.causesFire = false;
            this.ionDamage = 0;
            this.systemDamage = 0;
            this.stunDuration = 0f;
        }

        public static DamageInfo Laser(int amount) => new DamageInfo(amount, DamageType.Laser);
        public static DamageInfo Missile(int amount) => new DamageInfo(amount, DamageType.Missile) { ignoresShields = true };
        public static DamageInfo Ion(int ionDamage) => new DamageInfo(0, DamageType.Ion) { ionDamage = ionDamage };
        public static DamageInfo Beam(int amount) => new DamageInfo(amount, DamageType.Beam);
        public static DamageInfo Fire(int amount) => new DamageInfo(amount, DamageType.Fire);
    }

    // ==================== TARGET SYSTEM ====================

    /// <summary>
    /// Types of targets that can be selected in combat.
    /// </summary>
    public enum TargetType
    {
        Ship,
        Room,
        System,
        Crew,
        Projectile,
        Drone
    }

    // ==================== EVENT OUTCOME TYPES ====================

    /// <summary>
    /// Types of outcomes from events.
    /// </summary>
    public enum OutcomeType
    {
        Neutral,
        Positive,
        Negative,
        Mixed,
        Combat,
        Escape,
        Trade
    }

    /// <summary>
    /// Bond levels between crew members.
    /// </summary>
    public enum BondLevel
    {
        Stranger,
        Acquaintance,
        Friend,
        CloseFriend,
        Rival,
        Enemy
    }

    /// <summary>
    /// Reason for game ending.
    /// </summary>
    public enum GameEndReason
    {
        Victory,
        Defeat,
        Abandoned,
        Error
    }

    // ==================== WEAPON TYPES ====================

    /// <summary>
    /// Types of weapons available.
    /// </summary>
    public enum WeaponType
    {
        Laser,
        Missile,
        Beam,
        Ion,
        Bomb,
        Flak,
        Crystal
    }

    /// <summary>
    /// Status effects that can be applied by weapons.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public StatusEffectType type;
        public float duration;
        public float intensity;
    }

    public enum StatusEffectType
    {
        None,
        Fire,
        Breach,
        IonDamage,
        Stunned,
        Hacked,
        MindControlled
    }

    // ==================== RARITY ====================

    /// <summary>
    /// Rarity levels for items (weapons, augments).
    /// </summary>
    public enum WeaponRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    public enum AugmentRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
}
