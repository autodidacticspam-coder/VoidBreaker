using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Cloaking system - provides temporary invisibility and evasion bonus.
    /// </summary>
    public class CloakSystem : ShipSystem
    {
        [Header("Cloak State")]
        [SerializeField] private bool isCloaked = false;
        [SerializeField] private float cloakDuration = 5f;
        [SerializeField] private float cloakTimer = 0f;
        [SerializeField] private float cooldownDuration = 20f;
        [SerializeField] private float cooldownTimer = 0f;
        [SerializeField] private bool isOnCooldown = false;

        [Header("Bonuses")]
        [SerializeField] private int evasionBonus = 60;

        public bool IsCloaked => isCloaked;
        public bool CanCloak => !isCloaked && !isOnCooldown && IsOperational && AllocatedPower > 0;
        public float CloakTimeRemaining => isCloaked ? cloakDuration - cloakTimer : 0f;
        public float CooldownRemaining => isOnCooldown ? cooldownDuration - cooldownTimer : 0f;
        public int EvasionBonus => isCloaked ? evasionBonus : 0;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.Cloak;
            maxPower = level;

            // Duration scales with level
            cloakDuration = 3f + (level * 2f);
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (isCloaked)
            {
                cloakTimer += deltaTime;

                // Check if cloak expires
                if (cloakTimer >= cloakDuration)
                {
                    Decloak();
                }
            }
            else if (isOnCooldown)
            {
                cooldownTimer += deltaTime;

                if (cooldownTimer >= cooldownDuration)
                {
                    isOnCooldown = false;
                    cooldownTimer = 0f;
                    Debug.Log("[CloakSystem] Cloak ready");
                }
            }
        }

        public bool TryActivateCloak()
        {
            if (!CanCloak)
            {
                Debug.Log("[CloakSystem] Cannot activate cloak");
                return false;
            }

            isCloaked = true;
            cloakTimer = 0f;

            // Adjust duration based on power
            float powerMultiplier = (float)AllocatedPower / maxPower;
            cloakDuration = (3f + (currentLevel * 2f)) * powerMultiplier;

            Debug.Log($"[CloakSystem] CLOAKED for {cloakDuration:F1}s");

            GameEvents.TriggerCloakActivated(new CloakActivatedArgs
            {
                ship = parentShip,
                duration = cloakDuration
            });

            return true;
        }

        public void Decloak()
        {
            if (!isCloaked) return;

            isCloaked = false;
            cloakTimer = 0f;
            isOnCooldown = true;
            cooldownTimer = 0f;

            Debug.Log("[CloakSystem] Decloaked - cooldown started");

            GameEvents.TriggerCloakDeactivated(new CloakDeactivatedArgs
            {
                ship = parentShip
            });
        }

        public void ForceDeCloak()
        {
            // Used when taking damage while cloaked
            if (isCloaked)
            {
                Decloak();
                Debug.Log("[CloakSystem] Cloak disrupted by damage!");
            }
        }

        public override void OnDamaged(int amount)
        {
            base.OnDamaged(amount);

            // Damage can disrupt cloak
            if (isCloaked && damageLevel >= currentLevel)
            {
                ForceDeCloak();
            }
        }
    }
}
