using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Reactor system that generates power for the ship.
    /// Unlike other systems, the reactor doesn't consume power - it produces it.
    /// </summary>
    public class ReactorSystem : ShipSystem
    {
        [Header("Reactor")]
        [SerializeField] private int powerOutput;

        /// <summary>
        /// Total power output (level determines max power available to the ship)
        /// </summary>
        public int PowerOutput => Mathf.Max(0, currentLevel - DamageLevel - ionDamage);

        protected override void Awake()
        {
            base.Awake();
            systemName = "Reactor";
            systemType = SystemType.Reactor;
            requiresPower = false; // Reactor doesn't consume power
            maxLevel = 25; // Reactors can have many levels
        }

        public override void Initialize(int startingLevel)
        {
            base.Initialize(startingLevel);
            powerOutput = currentLevel;
        }

        public override void Upgrade()
        {
            base.Upgrade();
            powerOutput = currentLevel;

            // Notify power manager of increased capacity
            GameEvents.TriggerPowerChanged(new PowerChangedArgs
            {
                totalPower = PowerOutput,
                usedPower = 0, // PowerManager will update this
                availablePower = PowerOutput
            });
        }

        public override void TakeDamage(int amount)
        {
            int oldOutput = PowerOutput;
            base.TakeDamage(amount);

            // Reactor damage reduces available power
            if (PowerOutput < oldOutput)
            {
                GameEvents.TriggerPowerChanged(new PowerChangedArgs
                {
                    totalPower = PowerOutput,
                    usedPower = 0,
                    availablePower = PowerOutput
                });
            }
        }

        public override void Repair(int amount)
        {
            int oldOutput = PowerOutput;
            base.Repair(amount);

            // Reactor repair increases available power
            if (PowerOutput > oldOutput)
            {
                GameEvents.TriggerPowerChanged(new PowerChangedArgs
                {
                    totalPower = PowerOutput,
                    usedPower = 0,
                    availablePower = PowerOutput
                });
            }
        }

        public override void TakeIonDamage(int amount)
        {
            int oldOutput = PowerOutput;
            base.TakeIonDamage(amount);

            // Ion damage also reduces power output
            if (PowerOutput < oldOutput)
            {
                GameEvents.TriggerPowerChanged(new PowerChangedArgs
                {
                    totalPower = PowerOutput,
                    usedPower = 0,
                    availablePower = PowerOutput
                });
            }
        }

        public override void Tick(float deltaTime)
        {
            int oldOutput = PowerOutput;
            base.Tick(deltaTime);

            // Ion recovery may restore power
            if (PowerOutput > oldOutput)
            {
                GameEvents.TriggerPowerChanged(new PowerChangedArgs
                {
                    totalPower = PowerOutput,
                    usedPower = 0,
                    availablePower = PowerOutput
                });
            }
        }
    }
}
