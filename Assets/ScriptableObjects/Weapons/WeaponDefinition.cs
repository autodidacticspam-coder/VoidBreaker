using UnityEngine;
using VoidBreaker.Combat;
using VoidBreaker.Core;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining a weapon's stats and behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "VoidBreaker/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName;
        public string weaponId;
        [TextArea(2, 4)]
        public string description;
        public WeaponType weaponType;
        public Rarity rarity = Rarity.Common;

        [Header("Visuals")]
        public Sprite icon;
        public Sprite projectileSprite;
        public Color projectileColor = Color.white;
        public GameObject impactEffectPrefab;

        [Header("Power")]
        public int powerRequired = 1;

        [Header("Timing")]
        public float chargeTime = 10f;
        public float cooldown = 0f;

        [Header("Damage")]
        public int damage = 1;
        public int shieldPiercing = 0;
        public int systemDamageBonus = 0;
        public int hullDamageBonus = 0;

        [Header("Projectiles")]
        public int projectileCount = 1;
        public float spread = 0f;
        public float projectileSpeed = 10f;

        [Header("Ammo")]
        public bool usesAmmo = false;
        public int ammoCost = 1;

        [Header("Special Effects")]
        public StatusEffect[] appliedEffects;
        public float fireChance = 0f;
        public float breachChance = 0f;
        public int ionDamage = 0;
        public float stunDuration = 0f;

        [Header("Beam Specific")]
        public bool isBeam = false;
        public float beamLength = 5f;
        public int beamRoomHits = 3;

        [Header("Value")]
        public int scrapValue = 40;
        public int buyPrice = 60;

        [Header("AI Hints")]
        public float aiPriorityMultiplier = 1f;
        public bool preferTargetingSystems = false;
        public SystemType preferredTarget = SystemType.Shields;
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
