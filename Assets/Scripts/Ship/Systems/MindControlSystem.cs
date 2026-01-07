using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Crew;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Mind control system - temporarily takes control of enemy crew.
    /// </summary>
    public class MindControlSystem : ShipSystem
    {
        [Header("Mind Control State")]
        [SerializeField] private bool isControlling = false;
        [SerializeField] private CrewMember controlledCrew;
        [SerializeField] private ShipController targetShip;

        [Header("Timings")]
        [SerializeField] private float controlDuration = 10f;
        [SerializeField] private float cooldownTime = 25f;
        [SerializeField] private float currentTimer = 0f;
        [SerializeField] private bool isOnCooldown = false;

        [Header("Effects")]
        [SerializeField] private float damageBonus = 1.5f;
        [SerializeField] private bool targetsSystems = true; // Controlled crew targets systems

        public bool IsControlling => isControlling;
        public bool CanControl => !isControlling && !isOnCooldown && IsOperational && AllocatedPower > 0;
        public float ControlTimeRemaining => isControlling ? controlDuration - currentTimer : 0f;
        public float CooldownRemaining => isOnCooldown ? cooldownTime - currentTimer : 0f;
        public CrewMember ControlledCrew => controlledCrew;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.MindControl;
            maxPower = level;

            // Higher level = longer control
            controlDuration = 8f + (level * 2f);
            damageBonus = 1f + (level * 0.25f);
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (isControlling)
            {
                currentTimer += deltaTime;

                if (currentTimer >= controlDuration)
                {
                    EndControl();
                }

                // Make controlled crew attack their allies
                if (controlledCrew != null && controlledCrew.CurrentHealth > 0)
                {
                    DirectControlledCrew();
                }
            }
            else if (isOnCooldown)
            {
                currentTimer += deltaTime;

                if (currentTimer >= cooldownTime)
                {
                    isOnCooldown = false;
                    currentTimer = 0f;
                    Debug.Log("[MindControlSystem] Mind control ready");
                }
            }
        }

        public void SetTarget(ShipController ship)
        {
            targetShip = ship;
        }

        public bool TryControl(CrewMember target)
        {
            if (!CanControl)
            {
                Debug.Log("[MindControlSystem] Cannot activate mind control");
                return false;
            }

            if (target == null || target.CurrentHealth <= 0)
            {
                Debug.Log("[MindControlSystem] Invalid target");
                return false;
            }

            // Check if target is immune (some races are)
            if (target.IsImmuneToMindControl)
            {
                Debug.Log($"[MindControlSystem] {target.CrewName} is immune to mind control!");
                return false;
            }

            controlledCrew = target;
            isControlling = true;
            currentTimer = 0f;

            // Apply mind control effect
            target.ApplyMindControl(parentShip, damageBonus);

            Debug.Log($"[MindControlSystem] Controlling {target.CrewName}!");

            GameEvents.TriggerMindControlStarted(new MindControlStartedArgs
            {
                controlledCrew = target,
                duration = controlDuration
            });

            return true;
        }

        public bool TryControlRandom()
        {
            if (!CanControl || targetShip == null)
                return false;

            var crewManager = targetShip.GetComponent<CrewManager>();
            if (crewManager == null) return false;

            var allCrew = crewManager.GetAllCrew();
            if (allCrew.Count == 0) return false;

            // Find a valid target (not already controlled, not immune)
            CrewMember target = null;
            int attempts = 0;
            while (target == null && attempts < 10)
            {
                var candidate = allCrew[Random.Range(0, allCrew.Count)];
                if (!candidate.IsImmuneToMindControl && candidate.CurrentHealth > 0)
                {
                    target = candidate;
                }
                attempts++;
            }

            if (target == null) return false;

            return TryControl(target);
        }

        private void DirectControlledCrew()
        {
            if (controlledCrew == null) return;

            // Find nearby enemy crew to attack
            var room = controlledCrew.CurrentRoom;
            if (room == null) return;

            CrewMember nearestEnemy = null;
            float nearestDist = float.MaxValue;

            foreach (var crew in room.GetCrewInRoom())
            {
                if (crew == controlledCrew) continue;
                if (crew.CurrentHealth <= 0) continue;

                float dist = Vector3.Distance(controlledCrew.transform.position, crew.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestEnemy = crew;
                }
            }

            // Also check for systems to sabotage
            if (targetsSystems && room.System != null && nearestEnemy == null)
            {
                controlledCrew.SetTask(CrewTask.Sabotage, room);
            }
            else if (nearestEnemy != null)
            {
                controlledCrew.SetCombatTarget(nearestEnemy);
            }
        }

        private void EndControl()
        {
            if (controlledCrew != null)
            {
                controlledCrew.RemoveMindControl();
            }

            isControlling = false;
            isOnCooldown = true;
            currentTimer = 0f;

            Debug.Log("[MindControlSystem] Mind control ended");

            GameEvents.TriggerMindControlEnded(new MindControlEndedArgs
            {
                controlledCrew = controlledCrew
            });

            controlledCrew = null;
        }

        public override void OnDamaged(int amount)
        {
            base.OnDamaged(amount);

            // Damage can break control
            if (isControlling && damageLevel >= currentLevel)
            {
                EndControl();
                Debug.Log("[MindControlSystem] Mind control disrupted by damage!");
            }
        }
    }
}
