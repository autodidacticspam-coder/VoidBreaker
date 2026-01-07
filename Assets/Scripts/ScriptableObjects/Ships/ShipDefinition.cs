using System;
using UnityEngine;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject defining a ship's base properties.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShip", menuName = "VoidBreaker/Ship Definition")]
    public class ShipDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string shipId;
        public string shipName;
        [TextArea] public string description;
        public Sprite shipSprite;

        [Header("Hull")]
        public int maxHull = 30;
        public int startingHull = 30;

        [Header("Systems")]
        public int reactorPower = 8;
        public SystemLoadout[] systemLayouts;

        [Header("Weapons")]
        public int weaponSlots = 4;
        public WeaponDefinition[] startingWeapons;

        [Header("Crew")]
        public int crewCapacity = 8;
        public int startingCrew = 3;
        public RaceDefinition[] startingCrewRaces;

        [Header("Resources")]
        public int startingScrap = 30;
        public int startingFuel = 16;
        public int startingMissiles = 8;
        public int startingDroneParts = 2;

        [Header("Unlocking")]
        public bool unlockedByDefault = false;
        public string unlockCondition;
    }

    [Serializable]
    public class SystemLoadout
    {
        public ShipSystemType systemType;
        public int startingLevel;
        public int maxLevel;
        public bool isOptional;
    }

    public enum ShipSystemType
    {
        Shields,
        Weapons,
        Engines,
        Piloting,
        Sensors,
        Doors,
        Medbay,
        Oxygen,
        Teleporter,
        Cloaking,
        MindControl,
        Hacking,
        Artillery,
        Battery,
        CloneBay
    }

    public enum ShipClass
    {
        Frigate,
        Cruiser,
        Destroyer,
        Battleship,
        Carrier,
        Scout
    }
}
