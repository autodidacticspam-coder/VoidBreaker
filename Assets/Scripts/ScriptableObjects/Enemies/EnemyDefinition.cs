using UnityEngine;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining an enemy encounter.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "VoidBreaker/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId;
        public string enemyName;
        [TextArea] public string description;

        [Header("Ship")]
        public ShipDefinition shipDefinition;

        [Header("Difficulty")]
        [Range(1, 10)]
        public int difficulty = 1;
        public int minSector = 1;
        public int maxSector = 8;

        [Header("Behavior")]
        public EnemyAIType aiType = EnemyAIType.Balanced;
        public bool canFlee = false;
        public float fleeHealthThreshold = 0.2f;

        [Header("Rewards")]
        public int baseScrapReward = 20;
        public int mutationPointReward = 5;
        public float weaponDropChance = 0.15f;
        public float augmentDropChance = 0.1f;
        public float crewDropChance = 0.05f;
    }

    public enum EnemyAIType
    {
        Aggressive,     // Focuses on weapons and damage
        Defensive,      // Prioritizes shields and evasion
        Balanced,       // Mix of offense and defense
        Boarding,       // Prioritizes crew combat
        Hit_And_Run     // Uses cloak/teleport tactics
    }
}
