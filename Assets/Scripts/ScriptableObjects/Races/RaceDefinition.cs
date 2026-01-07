using System;
using UnityEngine;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining a crew race/species.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRace", menuName = "VoidBreaker/Race Definition")]
    public class RaceDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string raceId;
        public string raceName;
        [TextArea] public string description;
        public Sprite raceIcon;
        public Color raceColor = Color.white;

        [Header("Base Stats")]
        public int baseHealth = 100;
        public float baseMoveSpeed = 1f;
        public float baseRepairSpeed = 1f;
        public float baseCombatDamage = 1f;

        [Header("Resistances")]
        public float suffocationResistance = 0f;
        public float fireResistance = 0f;
        public float psychicResistance = 0f;

        [Header("Special Abilities")]
        public bool canTeleport = false;
        public bool canPassThroughWalls = false;
        public bool canSurviveInVacuum = false;
        public bool healsByDamage = false;
        public bool causesFireOnDeath = false;

        [Header("Skill Bonuses")]
        public float pilotingBonus = 0f;
        public float enginesBonus = 0f;
        public float shieldsBonus = 0f;
        public float weaponsBonus = 0f;
        public float repairBonus = 0f;
        public float combatBonus = 0f;

        [Header("Unlocking")]
        public bool unlockedByDefault = true;
        public string unlockCondition;
    }
}
