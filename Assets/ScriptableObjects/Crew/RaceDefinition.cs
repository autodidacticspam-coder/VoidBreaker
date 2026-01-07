using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining a crew race's base stats and abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRace", menuName = "VoidBreaker/Race Definition")]
    public class RaceDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string raceName;
        public CrewRace raceType;
        [TextArea(2, 4)]
        public string description;
        public string abilityDescription;

        [Header("Visuals")]
        public Sprite portrait;
        public Sprite[] walkSprites;
        public Color skinColor = Color.white;

        [Header("Base Stats")]
        public int maxHealth = 100;
        public float movementSpeed = 1f;
        public float combatDamage = 5f;
        public float repairSpeed = 1f;

        [Header("Skill Learning")]
        public float experienceMultiplier = 1f; // Humans get 1.1

        [Header("Immunities")]
        public bool immuneToSuffocation = false;
        public bool immuneToFire = false;
        public bool immuneToMindControl = false;

        [Header("Special Abilities")]
        public bool providesZoltanPower = false;
        public int zoltanPowerAmount = 1;
        public bool drainsOxygen = false;
        public int oxygenDrainRate = 2;
        public bool hasSensorAbility = false; // Slugs
        public int sensorRange = 1;
        public bool canLockdown = false; // Crystals

        [Header("Hiring")]
        public int baseHireCost = 45;
        public float spawnWeight = 1f;
    }
}
