using System;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining a weapon's properties.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "VoidBreaker/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string weaponId;
        public string weaponName;
        [TextArea] public string description;
        public Sprite weaponSprite;

        [Header("Type")]
        public WeaponType weaponType = WeaponType.Laser;
        public WeaponRarity rarity = WeaponRarity.Common;

        [Header("Stats")]
        public int baseDamage = 1;
        public float chargeTime = 1f;
        public float cooldown = 1f;
        public int powerCost = 1;
        public int projectileCount = 1;

        [Header("Ammo")]
        public AmmoType ammoType = AmmoType.None;
        public int ammoCost = 0;

        [Header("Effects")]
        public bool ignoresShields = false;
        public int shieldPiercing = 0;
        public int ionDamage = 0;
        public int systemDamage = 0;
        public float breachChance = 0f;
        public float fireChance = 0f;
        public float stunChance = 0f;
        public float stunDuration = 0f;

        [Header("Crew Bonus")]
        public bool requiresOperator = false;
        public float operatorBonus = 0.1f;

        [Header("Audio/Visual")]
        public AudioClip fireSound;
        public AudioClip hitSound;
        public GameObject projectilePrefab;
        public GameObject hitEffectPrefab;

        [Header("Unlocking")]
        public bool unlockedByDefault = true;
        public string unlockCondition;
    }

    public enum AmmoType
    {
        None,
        Missiles,
        DroneParts,
        Bombs
    }
}
