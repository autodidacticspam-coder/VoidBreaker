using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Data;
using System.Collections.Generic;

namespace VoidBreaker.Data
{
    /// <summary>
    /// Contains default preset data for ships, weapons, mutations, etc.
    /// Used to generate initial ScriptableObject assets or for testing.
    /// </summary>
    public static class DefaultPresets
    {
        // ==================== STARTER SHIPS ====================

        public static ShipPresetData[] GetStarterShips()
        {
            return new ShipPresetData[]
            {
                new ShipPresetData
                {
                    Id = "voidbreaker_standard",
                    Name = "Voidbreaker - Standard",
                    Class = ShipClass.Cruiser,
                    Description = "A balanced cruiser with moderate firepower and defenses. The standard choice for new pilots.",
                    Hull = 30,
                    Shields = 2,
                    ReactorPower = 8,
                    WeaponSlots = 3,
                    DroneSlots = 2,
                    BaseEvasion = 15,
                    StartingWeapons = new[] { "burst_laser_mk1", "artemis_missile" },
                    StartingAugments = new string[0],
                    StartingCrew = new[] { "human", "human", "human" },
                    IsUnlockedByDefault = true
                },
                new ShipPresetData
                {
                    Id = "phantom_striker",
                    Name = "Phantom Striker",
                    Class = ShipClass.Fighter,
                    Description = "A fast, evasive ship with cloaking capabilities. Favors hit-and-run tactics.",
                    Hull = 20,
                    Shields = 1,
                    ReactorPower = 7,
                    WeaponSlots = 2,
                    DroneSlots = 1,
                    BaseEvasion = 30,
                    StartingWeapons = new[] { "dual_laser" },
                    StartingAugments = new[] { "stealth_weapons" },
                    StartingCrew = new[] { "human", "mantis" },
                    IsUnlockedByDefault = false,
                    UnlockAchievement = "complete_phantom_run"
                },
                new ShipPresetData
                {
                    Id = "herald_diplomat",
                    Name = "Herald Diplomat",
                    Class = ShipClass.Freighter,
                    Description = "A diplomatic vessel with enhanced sensors and communication systems. Better at negotiations.",
                    Hull = 35,
                    Shields = 3,
                    ReactorPower = 10,
                    WeaponSlots = 2,
                    DroneSlots = 3,
                    BaseEvasion = 10,
                    StartingWeapons = new[] { "ion_stunner" },
                    StartingAugments = new[] { "long_range_scanners", "trade_license" },
                    StartingCrew = new[] { "human", "zoltan", "engi" },
                    IsUnlockedByDefault = false,
                    UnlockAchievement = "complete_herald_run"
                },
                new ShipPresetData
                {
                    Id = "predator_warship",
                    Name = "Predator Warship",
                    Class = ShipClass.Battleship,
                    Description = "A heavily armed warship designed for aggressive combat. More weapons, less subtlety.",
                    Hull = 40,
                    Shields = 4,
                    ReactorPower = 12,
                    WeaponSlots = 4,
                    DroneSlots = 2,
                    BaseEvasion = 5,
                    StartingWeapons = new[] { "burst_laser_mk2", "hull_beam", "artemis_missile" },
                    StartingAugments = new[] { "automated_reloader" },
                    StartingCrew = new[] { "human", "human", "mantis", "rock" },
                    IsUnlockedByDefault = false,
                    UnlockAchievement = "complete_predator_run"
                }
            };
        }

        // ==================== WEAPONS ====================

        public static WeaponPresetData[] GetDefaultWeapons()
        {
            return new WeaponPresetData[]
            {
                // Lasers
                new WeaponPresetData
                {
                    Id = "burst_laser_mk1",
                    Name = "Burst Laser Mk I",
                    Type = WeaponType.Laser,
                    Rarity = WeaponRarity.Common,
                    Description = "Fires 2 laser shots. Reliable and power-efficient.",
                    Damage = 1,
                    ShieldPiercing = 0,
                    ProjectileCount = 2,
                    ChargeTime = 10f,
                    PowerCost = 2,
                    BasePrice = 40
                },
                new WeaponPresetData
                {
                    Id = "burst_laser_mk2",
                    Name = "Burst Laser Mk II",
                    Type = WeaponType.Laser,
                    Rarity = WeaponRarity.Uncommon,
                    Description = "Fires 3 laser shots. More firepower for the same power cost.",
                    Damage = 1,
                    ShieldPiercing = 0,
                    ProjectileCount = 3,
                    ChargeTime = 12f,
                    PowerCost = 2,
                    BasePrice = 80
                },
                new WeaponPresetData
                {
                    Id = "dual_laser",
                    Name = "Dual Laser",
                    Type = WeaponType.Laser,
                    Rarity = WeaponRarity.Common,
                    Description = "Fires 2 laser shots. Fast and power-efficient.",
                    Damage = 1,
                    ShieldPiercing = 0,
                    ProjectileCount = 2,
                    ChargeTime = 8f,
                    PowerCost = 1,
                    BasePrice = 30
                },

                // Beams
                new WeaponPresetData
                {
                    Id = "hull_beam",
                    Name = "Hull Beam",
                    Type = WeaponType.Beam,
                    Rarity = WeaponRarity.Uncommon,
                    Description = "A beam that pierces all shields and deals bonus hull damage.",
                    Damage = 2,
                    ShieldPiercing = 99,
                    ProjectileCount = 1,
                    ChargeTime = 14f,
                    PowerCost = 3,
                    BasePrice = 75
                },
                new WeaponPresetData
                {
                    Id = "fire_beam",
                    Name = "Fire Beam",
                    Type = WeaponType.Beam,
                    Rarity = WeaponRarity.Rare,
                    Description = "A beam that sets rooms on fire. Requires shields to be down.",
                    Damage = 1,
                    ShieldPiercing = 0,
                    ProjectileCount = 1,
                    ChargeTime = 16f,
                    PowerCost = 2,
                    BasePrice = 70,
                    CausesFires = true,
                    FireChance = 1f
                },

                // Missiles
                new WeaponPresetData
                {
                    Id = "artemis_missile",
                    Name = "Artemis Missile",
                    Type = WeaponType.Missile,
                    Rarity = WeaponRarity.Common,
                    Description = "A basic missile that bypasses shields. Requires missile ammo.",
                    Damage = 2,
                    ShieldPiercing = 5,
                    ProjectileCount = 1,
                    ChargeTime = 10f,
                    PowerCost = 1,
                    AmmoCost = 1,
                    AmmoType = AmmoType.Missiles,
                    BasePrice = 35
                },
                new WeaponPresetData
                {
                    Id = "breach_missile",
                    Name = "Breach Missile",
                    Type = WeaponType.Missile,
                    Rarity = WeaponRarity.Uncommon,
                    Description = "A missile that creates hull breaches. Bypasses shields.",
                    Damage = 3,
                    ShieldPiercing = 5,
                    ProjectileCount = 1,
                    ChargeTime = 18f,
                    PowerCost = 2,
                    AmmoCost = 1,
                    AmmoType = AmmoType.Missiles,
                    BasePrice = 65,
                    CausesBreaches = true,
                    BreachChance = 0.8f
                },

                // Ion
                new WeaponPresetData
                {
                    Id = "ion_stunner",
                    Name = "Ion Stunner",
                    Type = WeaponType.Ion,
                    Rarity = WeaponRarity.Common,
                    Description = "Deals ion damage that disables systems temporarily.",
                    Damage = 0,
                    IonDamage = 1,
                    ShieldPiercing = 0,
                    ProjectileCount = 1,
                    ChargeTime = 8f,
                    PowerCost = 1,
                    BasePrice = 45
                },
                new WeaponPresetData
                {
                    Id = "ion_blast_mk2",
                    Name = "Ion Blast Mk II",
                    Type = WeaponType.Ion,
                    Rarity = WeaponRarity.Rare,
                    Description = "Fires 3 ion shots. Excellent for disabling enemy systems.",
                    Damage = 0,
                    IonDamage = 1,
                    ShieldPiercing = 0,
                    ProjectileCount = 3,
                    ChargeTime = 14f,
                    PowerCost = 3,
                    BasePrice = 95
                },

                // Bombs
                new WeaponPresetData
                {
                    Id = "small_bomb",
                    Name = "Small Bomb",
                    Type = WeaponType.Bomb,
                    Rarity = WeaponRarity.Common,
                    Description = "Teleports a small explosive to the target room.",
                    Damage = 2,
                    ShieldPiercing = 5,
                    ProjectileCount = 1,
                    ChargeTime = 15f,
                    PowerCost = 1,
                    AmmoCost = 1,
                    AmmoType = AmmoType.Missiles,
                    BasePrice = 40
                },
                new WeaponPresetData
                {
                    Id = "fire_bomb",
                    Name = "Fire Bomb",
                    Type = WeaponType.Bomb,
                    Rarity = WeaponRarity.Uncommon,
                    Description = "Teleports an incendiary bomb that starts fires.",
                    Damage = 1,
                    ShieldPiercing = 5,
                    ProjectileCount = 1,
                    ChargeTime = 13f,
                    PowerCost = 1,
                    AmmoCost = 1,
                    AmmoType = AmmoType.Missiles,
                    BasePrice = 55,
                    CausesFires = true,
                    FireChance = 1f
                },

                // Legendary
                new WeaponPresetData
                {
                    Id = "vulcan_chain",
                    Name = "Vulcan Chain Gun",
                    Type = WeaponType.Laser,
                    Rarity = WeaponRarity.Legendary,
                    Description = "Fires faster with each shot. Reaches devastating fire rates.",
                    Damage = 1,
                    ShieldPiercing = 0,
                    ProjectileCount = 1,
                    ChargeTime = 11f,
                    PowerCost = 4,
                    BasePrice = 150
                },
                new WeaponPresetData
                {
                    Id = "glaive_beam",
                    Name = "Glaive Beam",
                    Type = WeaponType.Beam,
                    Rarity = WeaponRarity.Legendary,
                    Description = "A massive beam that deals 3 damage per room. Slow to charge.",
                    Damage = 3,
                    ShieldPiercing = 0,
                    ProjectileCount = 1,
                    ChargeTime = 25f,
                    PowerCost = 4,
                    BasePrice = 120
                }
            };
        }

        // ==================== MUTATIONS ====================

        public static MutationPresetData[] GetDefaultMutations()
        {
            return new MutationPresetData[]
            {
                // PREDATOR PATH - Tier 1
                new MutationPresetData
                {
                    Id = "pred_t1_damage",
                    Name = "Enhanced Weaponry",
                    Path = EvolutionPath.Predator,
                    Tier = 1,
                    Cost = 1,
                    Description = "Weapons deal 10% more damage.",
                    Effects = new[] { (MutationEffectType.BonusDamage, 0.1f) }
                },
                new MutationPresetData
                {
                    Id = "pred_t1_crit",
                    Name = "Targeting Systems",
                    Path = EvolutionPath.Predator,
                    Tier = 1,
                    Cost = 1,
                    Description = "5% chance to deal double damage.",
                    Effects = new[] { (MutationEffectType.CritChance, 0.05f) }
                },
                // PREDATOR PATH - Tier 2
                new MutationPresetData
                {
                    Id = "pred_t2_pierce",
                    Name = "Shield Breaker",
                    Path = EvolutionPath.Predator,
                    Tier = 2,
                    Cost = 2,
                    Description = "Weapons ignore 1 shield layer.",
                    Prerequisites = new[] { "pred_t1_damage" },
                    Effects = new[] { (MutationEffectType.BypassShields, 1f) }
                },
                new MutationPresetData
                {
                    Id = "pred_t2_lifesteal",
                    Name = "Vampiric Systems",
                    Path = EvolutionPath.Predator,
                    Tier = 2,
                    Cost = 2,
                    Description = "Heal 10% of damage dealt to enemy hull.",
                    Prerequisites = new[] { "pred_t1_crit" },
                    Effects = new[] { (MutationEffectType.LifeSteal, 0.1f) }
                },
                // PREDATOR PATH - Tier 3
                new MutationPresetData
                {
                    Id = "pred_t3_barrage",
                    Name = "Weapon Barrage",
                    Path = EvolutionPath.Predator,
                    Tier = 3,
                    Cost = 3,
                    Description = "+1 projectile for all weapons.",
                    Prerequisites = new[] { "pred_t2_pierce", "pred_t2_lifesteal" },
                    Effects = new[] { (MutationEffectType.BonusProjectiles, 1f) }
                },
                // PREDATOR PATH - Final
                new MutationPresetData
                {
                    Id = "pred_final",
                    Name = "Apex Predator",
                    Path = EvolutionPath.Predator,
                    Tier = 4,
                    Cost = 5,
                    Description = "Unlock the Dreadnought boss. +25% damage, +15% crit chance.",
                    Prerequisites = new[] { "pred_t3_barrage" },
                    IsFinal = true,
                    UnlocksEnding = "predator_ending",
                    Effects = new[] { (MutationEffectType.BonusDamage, 0.25f), (MutationEffectType.CritChance, 0.15f) }
                },

                // PHANTOM PATH - Tier 1
                new MutationPresetData
                {
                    Id = "phan_t1_evasion",
                    Name = "Evasive Maneuvers",
                    Path = EvolutionPath.Phantom,
                    Tier = 1,
                    Cost = 1,
                    Description = "+10% evasion.",
                    Effects = new[] { (MutationEffectType.BonusEvasion, 0.1f) }
                },
                new MutationPresetData
                {
                    Id = "phan_t1_cloak",
                    Name = "Improved Cloak",
                    Path = EvolutionPath.Phantom,
                    Tier = 1,
                    Cost = 1,
                    Description = "Cloak lasts 2 seconds longer.",
                    Effects = new[] { (MutationEffectType.CloakDuration, 2f) }
                },
                // PHANTOM PATH - Tier 2
                new MutationPresetData
                {
                    Id = "phan_t2_sensor",
                    Name = "Sensor Scrambler",
                    Path = EvolutionPath.Phantom,
                    Tier = 2,
                    Cost = 2,
                    Description = "Enemies have -20% accuracy against you.",
                    Prerequisites = new[] { "phan_t1_evasion" },
                    Effects = new[] { (MutationEffectType.SensorEvasion, 0.2f) }
                },
                new MutationPresetData
                {
                    Id = "phan_t2_silent",
                    Name = "Silent Running",
                    Path = EvolutionPath.Phantom,
                    Tier = 2,
                    Cost = 2,
                    Description = "Weapons can fire while cloaked without breaking cloak.",
                    Prerequisites = new[] { "phan_t1_cloak" },
                    Effects = new[] { (MutationEffectType.SilentRunning, 1f) }
                },
                // PHANTOM PATH - Tier 3
                new MutationPresetData
                {
                    Id = "phan_t3_ambush",
                    Name = "Ambush Specialist",
                    Path = EvolutionPath.Phantom,
                    Tier = 3,
                    Cost = 3,
                    Description = "First attack after uncloaking deals 50% more damage.",
                    Prerequisites = new[] { "phan_t2_sensor", "phan_t2_silent" },
                    Effects = new[] { (MutationEffectType.AmbushDamage, 0.5f) }
                },
                // PHANTOM PATH - Final
                new MutationPresetData
                {
                    Id = "phan_final",
                    Name = "Ghost Ship",
                    Path = EvolutionPath.Phantom,
                    Tier = 4,
                    Cost = 5,
                    Description = "Unlock the Station Infiltration. Permanent +20% evasion, instant cloak cooldown.",
                    Prerequisites = new[] { "phan_t3_ambush" },
                    IsFinal = true,
                    UnlocksEnding = "phantom_ending",
                    Effects = new[] { (MutationEffectType.BonusEvasion, 0.2f), (MutationEffectType.CloakCooldown, -999f) }
                },

                // HERALD PATH - Tier 1
                new MutationPresetData
                {
                    Id = "her_t1_trade",
                    Name = "Trade Routes",
                    Path = EvolutionPath.Herald,
                    Tier = 1,
                    Cost = 1,
                    Description = "Shop prices reduced by 10%.",
                    Effects = new[] { (MutationEffectType.ShopDiscount, 0.1f) }
                },
                new MutationPresetData
                {
                    Id = "her_t1_morale",
                    Name = "Inspiring Presence",
                    Path = EvolutionPath.Herald,
                    Tier = 1,
                    Cost = 1,
                    Description = "Crew morale +10%.",
                    Effects = new[] { (MutationEffectType.CrewMorale, 0.1f) }
                },
                // HERALD PATH - Tier 2
                new MutationPresetData
                {
                    Id = "her_t2_rewards",
                    Name = "Diplomatic Favors",
                    Path = EvolutionPath.Herald,
                    Tier = 2,
                    Cost = 2,
                    Description = "Event rewards +15%.",
                    Prerequisites = new[] { "her_t1_trade" },
                    Effects = new[] { (MutationEffectType.EventBonuses, 0.15f) }
                },
                new MutationPresetData
                {
                    Id = "her_t2_scrap",
                    Name = "Scavenger Networks",
                    Path = EvolutionPath.Herald,
                    Tier = 2,
                    Cost = 2,
                    Description = "Scrap rewards +20%.",
                    Prerequisites = new[] { "her_t1_morale" },
                    Effects = new[] { (MutationEffectType.ScrapBonus, 0.2f) }
                },
                // HERALD PATH - Tier 3
                new MutationPresetData
                {
                    Id = "her_t3_rep",
                    Name = "Renowned",
                    Path = EvolutionPath.Herald,
                    Tier = 3,
                    Cost = 3,
                    Description = "Reputation gains doubled. More peaceful resolutions.",
                    Prerequisites = new[] { "her_t2_rewards", "her_t2_scrap" },
                    Effects = new[] { (MutationEffectType.ReputationGain, 1f) }
                },
                // HERALD PATH - Final
                new MutationPresetData
                {
                    Id = "her_final",
                    Name = "Universal Ambassador",
                    Path = EvolutionPath.Herald,
                    Tier = 4,
                    Cost = 5,
                    Description = "Unlock the Council Negotiation. All faction reputations maxed.",
                    Prerequisites = new[] { "her_t3_rep" },
                    IsFinal = true,
                    UnlocksEnding = "herald_ending",
                    Effects = new[] { (MutationEffectType.ReputationGain, 999f) }
                }
            };
        }

        // ==================== AUGMENTS ====================

        public static AugmentPresetData[] GetDefaultAugments()
        {
            return new AugmentPresetData[]
            {
                new AugmentPresetData
                {
                    Id = "stealth_weapons",
                    Name = "Stealth Weapons",
                    Rarity = AugmentRarity.Rare,
                    Description = "Weapons do not deactivate cloaking when fired.",
                    BasePrice = 60
                },
                new AugmentPresetData
                {
                    Id = "long_range_scanners",
                    Name = "Long-Range Scanners",
                    Rarity = AugmentRarity.Uncommon,
                    Description = "See beacon hazards before jumping.",
                    BasePrice = 50
                },
                new AugmentPresetData
                {
                    Id = "trade_license",
                    Name = "Merchant License",
                    Rarity = AugmentRarity.Common,
                    Description = "10% discount at shops.",
                    BasePrice = 45
                },
                new AugmentPresetData
                {
                    Id = "automated_reloader",
                    Name = "Automated Reloader",
                    Rarity = AugmentRarity.Uncommon,
                    Description = "Weapons charge 10% faster.",
                    BasePrice = 55
                },
                new AugmentPresetData
                {
                    Id = "scrap_recovery",
                    Name = "Scrap Recovery Arm",
                    Rarity = AugmentRarity.Common,
                    Description = "Gain 10% more scrap from all sources.",
                    BasePrice = 40
                },
                new AugmentPresetData
                {
                    Id = "defense_scrambler",
                    Name = "Defense Scrambler",
                    Rarity = AugmentRarity.Rare,
                    Description = "Enemy ships have -10% evasion.",
                    BasePrice = 70
                },
                new AugmentPresetData
                {
                    Id = "pre_igniter",
                    Name = "Weapon Pre-Igniter",
                    Rarity = AugmentRarity.Legendary,
                    Description = "Weapons start fully charged at the beginning of combat.",
                    BasePrice = 120
                },
                new AugmentPresetData
                {
                    Id = "cloak_augment",
                    Name = "Cloaking Amplifier",
                    Rarity = AugmentRarity.Rare,
                    Description = "Cloak duration +5 seconds.",
                    BasePrice = 65
                }
            };
        }
    }

    // ==================== PRESET DATA STRUCTURES ====================

    [System.Serializable]
    public class ShipPresetData
    {
        public string Id;
        public string Name;
        public ShipClass Class;
        public string Description;
        public int Hull;
        public int Shields;
        public int ReactorPower;
        public int WeaponSlots;
        public int DroneSlots;
        public int BaseEvasion;
        public string[] StartingWeapons;
        public string[] StartingAugments;
        public string[] StartingCrew;
        public bool IsUnlockedByDefault;
        public string UnlockAchievement;
    }

    [System.Serializable]
    public class WeaponPresetData
    {
        public string Id;
        public string Name;
        public WeaponType Type;
        public WeaponRarity Rarity;
        public string Description;
        public int Damage;
        public int IonDamage;
        public int ShieldPiercing;
        public int ProjectileCount;
        public float ChargeTime;
        public int PowerCost;
        public int AmmoCost;
        public AmmoType AmmoType;
        public int BasePrice;
        public bool CausesBreaches;
        public float BreachChance;
        public bool CausesFires;
        public float FireChance;
    }

    [System.Serializable]
    public class MutationPresetData
    {
        public string Id;
        public string Name;
        public EvolutionPath Path;
        public int Tier;
        public int Cost;
        public string Description;
        public string[] Prerequisites;
        public (MutationEffectType type, float value)[] Effects;
        public bool IsFinal;
        public string UnlocksEnding;
    }

    [System.Serializable]
    public class AugmentPresetData
    {
        public string Id;
        public string Name;
        public AugmentRarity Rarity;
        public string Description;
        public int BasePrice;
    }
}
