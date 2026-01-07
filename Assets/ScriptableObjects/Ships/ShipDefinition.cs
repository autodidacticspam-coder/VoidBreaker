using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining a ship's base configuration.
    /// Each ship type (Vagrant, Fang, Whisper, etc.) has one of these.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShip", menuName = "VoidBreaker/Ship Definition")]
    public class ShipDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string shipName;
        public string shipId;
        [TextArea(2, 4)]
        public string description;
        public ShipClass shipClass;

        [Header("Visuals")]
        public Sprite baseSprite;
        public Sprite[] evolutionSpritesPredator;
        public Sprite[] evolutionSpritesPhantom;
        public Sprite[] evolutionSpritesHerald;
        public Color hullColor = Color.white;

        [Header("Base Stats")]
        public int baseHull = 30;
        public int baseReactor = 8;
        public int baseEvasion = 0;

        [Header("Layout")]
        public RoomLayout roomLayout;
        public int weaponSlots = 4;
        public int droneSlots = 2;
        public int augmentSlots = 3;

        [Header("Starting Loadout")]
        public SystemLoadout startingSystems;
        public WeaponDefinition[] startingWeapons;
        public RaceDefinition[] startingCrew;
        public AugmentDefinition[] startingAugments;

        [Header("Starting Resources")]
        public int startingScrap = 30;
        public int startingFuel = 16;
        public int startingMissiles = 8;
        public int startingDroneParts = 2;

        [Header("Evolution")]
        public EvolutionPath preferredPath = EvolutionPath.None;
        public float preferredPathBonus = 1.2f; // 20% more MP for preferred path

        [Header("Unlock")]
        public bool startsUnlocked = false;
        public string unlockCondition;
        public string unlockDescription;
    }

    public enum ShipClass
    {
        Standard,
        Combat,
        Stealth,
        Diplomatic,
        Drone,
        Experimental
    }

    [System.Serializable]
    public class SystemLoadout
    {
        public SystemConfig[] systems;
    }

    [System.Serializable]
    public class SystemConfig
    {
        public SystemType type;
        public int startingLevel;
        public bool isPoweredByDefault;
    }
}
