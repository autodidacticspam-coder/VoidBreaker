using UnityEngine;
using VoidBreaker.Ship;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Interface for crew members
    /// </summary>
    public interface ICrewMember : IDamageable
    {
        string CrewName { get; }
        CrewRace Race { get; }
        CrewSkills Skills { get; }
        Room CurrentRoom { get; }
        Room TargetRoom { get; }
        CrewTask CurrentTask { get; }
        bool IsPlayerControlled { get; }
        bool IsManning { get; }
        float MovementSpeed { get; }
        float CombatDamage { get; }
        float RepairSpeed { get; }

        void MoveTo(Room targetRoom);
        void AssignTask(CrewTask task);
        void SetManning(ISystem system);
        void StopManning();
        void Attack(ICrewMember target);
        void StopAttacking();
        void GainExperience(SkillType skill, int amount);

        event System.Action<ICrewMember, Room> OnRoomChanged;
        event System.Action<ICrewMember, CrewTask> OnTaskChanged;
    }

    public enum CrewRace
    {
        Human,
        Vex,        // Mantis-like (combat focused)
        Crystalline,
        Zoltan,
        Engi,
        Slug,
        Lanius,
        Synthetic
    }

    public enum CrewTask
    {
        Idle,
        Moving,
        Manning,
        Repairing,
        Fighting,
        Extinguishing,
        Healing
    }

    public enum SkillType
    {
        Piloting,
        Engines,
        Shields,
        Weapons,
        Repair,
        Combat
    }

    [System.Serializable]
    public class CrewSkills
    {
        public int piloting;
        public int engines;
        public int shields;
        public int weapons;
        public int repair;
        public int combat;

        public const int MAX_SKILL_LEVEL = 3;

        public int GetSkill(SkillType type)
        {
            return type switch
            {
                SkillType.Piloting => piloting,
                SkillType.Engines => engines,
                SkillType.Shields => shields,
                SkillType.Weapons => weapons,
                SkillType.Repair => repair,
                SkillType.Combat => combat,
                _ => 0
            };
        }

        public void AddExperience(SkillType type, int amount)
        {
            // Experience thresholds: 15, 30, 45 for levels 1, 2, 3
            // This is simplified - actual implementation would track XP
            switch (type)
            {
                case SkillType.Piloting: piloting = Mathf.Min(piloting + 1, MAX_SKILL_LEVEL); break;
                case SkillType.Engines: engines = Mathf.Min(engines + 1, MAX_SKILL_LEVEL); break;
                case SkillType.Shields: shields = Mathf.Min(shields + 1, MAX_SKILL_LEVEL); break;
                case SkillType.Weapons: weapons = Mathf.Min(weapons + 1, MAX_SKILL_LEVEL); break;
                case SkillType.Repair: repair = Mathf.Min(repair + 1, MAX_SKILL_LEVEL); break;
                case SkillType.Combat: combat = Mathf.Min(combat + 1, MAX_SKILL_LEVEL); break;
            }
        }
    }
}
