using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Piloting system - provides base evasion and enables FTL.
    /// </summary>
    public class PilotingSystem : ShipSystem
    {
        [Header("Evasion")]
        [SerializeField] private int baseEvasion = 5;
        [SerializeField] private int autoEvasion = 0; // When unmanned
        [SerializeField] private int mannedEvasion = 0; // When manned

        [Header("Autopilot")]
        [SerializeField] private bool hasAutopilot = false;
        [SerializeField] private int autopilotEvasion = 10; // Level 2+ gives some unmanned evasion

        public int CurrentEvasion => CalculateEvasion();
        public bool HasAutopilot => hasAutopilot && currentLevel >= 2;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.Piloting;
            maxPower = 1; // Piloting only needs 1 power

            // Level 2+ provides autopilot
            hasAutopilot = level >= 2;
            autopilotEvasion = level >= 2 ? 10 + ((level - 2) * 5) : 0;
        }

        private int CalculateEvasion()
        {
            if (!IsOperational || AllocatedPower == 0)
                return 0;

            int evasion = baseEvasion;

            if (IsManned)
            {
                // Full evasion when manned
                evasion += mannedEvasion;

                // Skill bonus from pilot
                if (ManningCrew != null)
                {
                    int skillBonus = ManningCrew.GetSkillLevel(CrewSkill.Piloting) * 2;
                    evasion += skillBonus;
                }
            }
            else if (hasAutopilot)
            {
                // Reduced evasion with autopilot
                evasion += autopilotEvasion;
            }
            else
            {
                // No evasion without pilot or autopilot
                evasion = 0;
            }

            // Apply damage penalty
            evasion -= damageLevel * 5;

            return Mathf.Max(0, evasion);
        }

        public bool CanFTL()
        {
            // Need pilot or autopilot to jump
            if (!IsOperational) return false;
            if (AllocatedPower == 0) return false;

            return IsManned || hasAutopilot;
        }

        public void SetMannedEvasion(int evasion)
        {
            mannedEvasion = evasion;
        }

        public override void OnManned(CrewMember crew)
        {
            base.OnManned(crew);
            Debug.Log($"[PilotingSystem] {crew.CrewName} at the helm - Evasion: {CurrentEvasion}%");
        }

        public override void OnUnmanned()
        {
            base.OnUnmanned();

            if (hasAutopilot)
            {
                Debug.Log($"[PilotingSystem] Autopilot engaged - Evasion: {CurrentEvasion}%");
            }
            else
            {
                Debug.Log("[PilotingSystem] No pilot - Evasion: 0%");
            }
        }
    }
}
