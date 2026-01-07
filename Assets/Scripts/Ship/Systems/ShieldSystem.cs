using System;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Shield system - absorbs incoming damage in layers.
    /// Each 2 power = 1 shield layer.
    /// </summary>
    public class ShieldSystem : ShipSystem
    {
        [Header("Shield State")]
        [SerializeField] private int currentLayers = 0;
        [SerializeField] private float rechargeProgress = 0f;
        [SerializeField] private float rechargeDelay = 0f;

        [Header("Shield Settings")]
        [SerializeField] private float baseRechargeTime = 2f;
        [SerializeField] private float rechargeDelayAfterHit = 0.5f;

        public int CurrentLayers => currentLayers;
        public int MaxLayers => EffectivePower / 2;
        public float RechargeProgress => rechargeProgress;
        public bool IsRecharging => currentLayers < MaxLayers && rechargeDelay <= 0;

        // Events
        public event Action<int> OnLayersChanged;
        public event Action OnShieldHit;

        protected override void Awake()
        {
            base.Awake();
            systemName = "Shields";
            systemType = SystemType.Shields;
            maxLevel = 8;
        }

        public override void Initialize(int startingLevel)
        {
            base.Initialize(startingLevel);
            currentLayers = 0;
            rechargeProgress = 0f;
            rechargeDelay = 0f;
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            // Handle recharge delay
            if (rechargeDelay > 0)
            {
                rechargeDelay -= deltaTime;
                return;
            }

            // Recharge shields
            if (currentLayers < MaxLayers && IsOperational)
            {
                float rechargeRate = GetRechargeRate();
                rechargeProgress += deltaTime / rechargeRate;

                if (rechargeProgress >= 1f)
                {
                    rechargeProgress = 0f;
                    currentLayers++;
                    OnLayersChanged?.Invoke(currentLayers);

                    GameEvents.TriggerShieldChanged(new ShieldChangedArgs
                    {
                        currentLayers = currentLayers,
                        maxLayers = MaxLayers,
                        rechargeProgress = rechargeProgress
                    });
                }
            }

            // If power reduced and we have too many layers, they don't disappear immediately
            // but won't recharge beyond the new max
        }

        /// <summary>
        /// Absorbs a hit, reducing shield layers by 1.
        /// Returns true if the hit was absorbed.
        /// </summary>
        public bool AbsorbHit()
        {
            if (currentLayers <= 0) return false;

            currentLayers--;
            rechargeProgress = 0f;
            rechargeDelay = rechargeDelayAfterHit;

            OnShieldHit?.Invoke();
            OnLayersChanged?.Invoke(currentLayers);

            // Grant shield skill experience to manning crew
            ManningCrew?.GainExperience(SkillType.Shields, 1);

            GameEvents.TriggerShieldChanged(new ShieldChangedArgs
            {
                currentLayers = currentLayers,
                maxLayers = MaxLayers,
                rechargeProgress = rechargeProgress
            });

            Debug.Log($"[Shields] Hit absorbed! Layers: {currentLayers}/{MaxLayers}");
            return true;
        }

        /// <summary>
        /// Calculates damage that passes through shields for beam weapons.
        /// Beams deal reduced damage based on remaining shields.
        /// </summary>
        public int CalculateBeamDamage(int baseDamage)
        {
            return Mathf.Max(0, baseDamage - currentLayers);
        }

        private float GetRechargeRate()
        {
            float rate = baseRechargeTime;

            // Manning bonus speeds up recharge
            if (IsManned)
            {
                int skillLevel = ManningCrew.Skills.GetSkill(SkillType.Shields);
                rate *= (1f - (skillLevel * 0.1f)); // 10% faster per skill level
            }

            return rate;
        }

        /// <summary>
        /// Instantly sets shields to a specific layer count (for hacking/events).
        /// </summary>
        public void SetLayers(int layers)
        {
            currentLayers = Mathf.Clamp(layers, 0, MaxLayers);
            OnLayersChanged?.Invoke(currentLayers);
        }

        /// <summary>
        /// Removes all shield layers (for hacking/EMP).
        /// </summary>
        public void DropShields()
        {
            currentLayers = 0;
            rechargeProgress = 0f;
            rechargeDelay = 2f; // Longer delay when fully dropped
            OnLayersChanged?.Invoke(currentLayers);
        }
    }
}
