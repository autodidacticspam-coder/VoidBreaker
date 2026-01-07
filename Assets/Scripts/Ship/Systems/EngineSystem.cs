using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Engine system - provides evasion and FTL charge speed.
    /// </summary>
    public class EngineSystem : ShipSystem
    {
        [Header("FTL State")]
        [SerializeField] private float ftlChargeProgress = 0f;
        [SerializeField] private bool isChargingFTL = false;

        [Header("FTL Settings")]
        [SerializeField] private float baseFTLChargeTime = 20f;

        public float FTLChargeProgress => ftlChargeProgress;
        public bool IsChargingFTL => isChargingFTL;
        public bool IsFTLReady => ftlChargeProgress >= 1f;

        /// <summary>
        /// Evasion bonus from engines (5% per power level)
        /// </summary>
        public int EvasionBonus => EffectivePower * 5;

        protected override void Awake()
        {
            base.Awake();
            systemName = "Engines";
            systemType = SystemType.Engines;
            maxLevel = 8;
        }

        public override void Initialize(int startingLevel)
        {
            base.Initialize(startingLevel);
            ftlChargeProgress = 0f;
            isChargingFTL = false;
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            // Charge FTL if active
            if (isChargingFTL && IsOperational)
            {
                float chargeRate = GetFTLChargeRate();
                ftlChargeProgress += deltaTime / chargeRate;

                ftlChargeProgress = Mathf.Min(1f, ftlChargeProgress);

                if (IsFTLReady)
                {
                    Debug.Log("[Engines] FTL ready!");
                }
            }
        }

        /// <summary>
        /// Starts charging the FTL drive.
        /// </summary>
        public void StartFTLCharge()
        {
            if (isChargingFTL) return;

            isChargingFTL = true;
            ftlChargeProgress = 0f;
            Debug.Log("[Engines] FTL charging started");
        }

        /// <summary>
        /// Cancels FTL charging (e.g., when entering combat).
        /// </summary>
        public void CancelFTLCharge()
        {
            isChargingFTL = false;
            // Note: Progress is preserved, not reset
            Debug.Log("[Engines] FTL charging paused");
        }

        /// <summary>
        /// Resets FTL charge completely (after jump).
        /// </summary>
        public void ResetFTLCharge()
        {
            isChargingFTL = false;
            ftlChargeProgress = 0f;
        }

        /// <summary>
        /// Executes FTL jump if ready.
        /// </summary>
        public bool TryFTLJump()
        {
            if (!IsFTLReady)
            {
                Debug.Log("[Engines] FTL not ready!");
                return false;
            }

            ResetFTLCharge();
            return true;
        }

        private float GetFTLChargeRate()
        {
            // Base time divided by engine efficiency
            float time = baseFTLChargeTime;

            // Power level affects charge speed
            float powerBonus = 1f + (EffectivePower * 0.05f); // 5% faster per power

            // Manning bonus
            if (IsManned)
            {
                int skillLevel = ManningCrew.Skills.GetSkill(SkillType.Engines);
                powerBonus += skillLevel * 0.1f; // 10% faster per skill level
            }

            return time / powerBonus;
        }

        /// <summary>
        /// Gets the skill bonus from manning crew.
        /// </summary>
        public int GetManningEvasionBonus()
        {
            if (!IsManned) return 0;
            return ManningCrew.Skills.GetSkill(SkillType.Engines) * 5;
        }
    }
}
