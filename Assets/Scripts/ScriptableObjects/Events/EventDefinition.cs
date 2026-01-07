using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Sector;

namespace VoidBreaker.Data
{
    /// <summary>
    /// ScriptableObject for defining text events.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEvent", menuName = "VoidBreaker/Event Definition")]
    public class EventDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string eventId;
        public string eventTitle;
        [TextArea(5, 15)]
        public string eventDescription;

        [Header("Display")]
        public Sprite eventImage;
        public EventTone tone = EventTone.Neutral;

        [Header("Availability")]
        public float spawnWeight = 1f;
        public int minSector = 0;
        public int maxSector = 10;
        public BeaconType[] validBeacons;
        public SectorType[] validSectors;

        [Header("Requirements")]
        public EventRequirement[] requirements;

        [Header("Choices")]
        public EventChoiceDefinition[] choices;

        [Header("Flags")]
        public bool isQuestEvent = false;
        public string questId;
        public bool isRepeatable = true;
        public string[] prerequisiteEvents;
    }

    [System.Serializable]
    public class EventRequirement
    {
        public EventRequirementType type;
        public string stringValue;
        public int intValue;
    }

    public enum EventRequirementType
    {
        HasCrew,
        HasCrewRace,
        HasSystem,
        HasAugment,
        MinScrap,
        MinFuel,
        EvolutionPath,
        QuestActive,
        QuestComplete
    }

    [System.Serializable]
    public class EventChoiceDefinition
    {
        public string choiceId;
        [TextArea(1, 3)]
        public string choiceText;

        [Header("Requirements")]
        public EventChoiceRequirement[] requirements;

        [Header("Outcomes")]
        public EventOutcomeDefinition[] outcomes;
    }

    [System.Serializable]
    public class EventChoiceRequirement
    {
        public ChoiceRequirementType type;
        public int amount;
        public string stringValue;
        public SystemType systemType;
        public CrewRace crewRace;
    }

    public enum ChoiceRequirementType
    {
        Scrap,
        Fuel,
        Missiles,
        DroneParts,
        Crew,
        CrewRace,
        System,
        Augment,
        MutationPoints
    }

    [System.Serializable]
    public class EventOutcomeDefinition
    {
        public float weight = 100f;
        [TextArea(2, 5)]
        public string resultText;

        [Header("Effects")]
        public EventEffectDefinition[] effects;

        [Header("Chain")]
        public string nextEventId;
        public string startCombatEnemyId;
    }

    [System.Serializable]
    public class EventEffectDefinition
    {
        public EventEffectType type;
        public int amount;
        public string stringValue;
        public CrewRace crewRace;
        public EvolutionPath evolutionPath;
    }

    public enum EventEffectType
    {
        GainScrap,
        LoseScrap,
        GainFuel,
        LoseFuel,
        GainMissiles,
        LoseMissiles,
        GainCrew,
        LoseCrew,
        GainWeapon,
        GainAugment,
        TakeDamage,
        RepairHull,
        SystemDamage,
        SystemRepair,
        CrewDamage,
        GainMutationPoints,
        UnlockMutation,
        EvolutionProgress,
        StartQuest,
        CompleteQuest,
        Nothing
    }

    public enum EventTone
    {
        Neutral,
        Positive,
        Negative,
        Mysterious,
        Threatening,
        Comedic
    }
}
