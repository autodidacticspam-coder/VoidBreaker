using System.Collections;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Hacking system - disrupts enemy ship systems temporarily.
    /// </summary>
    public class HackingSystem : ShipSystem
    {
        [Header("Hack State")]
        [SerializeField] private bool isDeployed = false;
        [SerializeField] private bool isHacking = false;
        [SerializeField] private ShipSystem targetSystem;
        [SerializeField] private ShipController targetShip;

        [Header("Timings")]
        [SerializeField] private float deployTime = 3f;
        [SerializeField] private float hackDuration = 10f;
        [SerializeField] private float cooldownTime = 20f;
        [SerializeField] private float currentTimer = 0f;

        [Header("Hack Drone")]
        [SerializeField] private HackDrone activeDrone;

        public bool IsDeployed => isDeployed;
        public bool IsHacking => isHacking;
        public bool CanDeploy => !isDeployed && IsOperational && AllocatedPower > 0;
        public float HackTimeRemaining => isHacking ? hackDuration - currentTimer : 0f;
        public ShipSystem TargetSystem => targetSystem;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.Hacking;
            maxPower = level;

            // Higher level = longer hack duration
            hackDuration = 7f + (level * 3f);
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (isHacking)
            {
                currentTimer += deltaTime;

                if (currentTimer >= hackDuration)
                {
                    EndHack();
                }
            }
        }

        public void SetTarget(ShipController ship)
        {
            targetShip = ship;
        }

        public bool TryDeployHack(ShipSystem target)
        {
            if (!CanDeploy)
            {
                Debug.Log("[HackingSystem] Cannot deploy hack");
                return false;
            }

            if (target == null || target.ParentShip == parentShip)
            {
                Debug.Log("[HackingSystem] Invalid target");
                return false;
            }

            targetSystem = target;
            isDeployed = true;

            // Create hack drone
            var droneObj = new GameObject("HackDrone");
            droneObj.transform.SetParent(transform);
            activeDrone = droneObj.AddComponent<HackDrone>();
            activeDrone.Deploy(parentShip, target, deployTime, OnDroneArrived);

            Debug.Log($"[HackingSystem] Hack drone deployed to {target.SystemType}");

            GameEvents.TriggerHackDeployed(new HackDeployedArgs
            {
                sourceShip = parentShip,
                targetShip = target.ParentShip,
                targetSystem = target.SystemType
            });

            return true;
        }

        private void OnDroneArrived()
        {
            if (!isDeployed) return;

            isHacking = true;
            currentTimer = 0f;

            // Apply hack effect to target
            targetSystem.ApplyHack();

            Debug.Log($"[HackingSystem] HACKING {targetSystem.SystemType}!");

            GameEvents.TriggerHackStarted(new HackStartedArgs
            {
                targetSystem = targetSystem.SystemType,
                duration = hackDuration
            });
        }

        public void ActivateHack()
        {
            if (!isDeployed || isHacking)
            {
                Debug.Log("[HackingSystem] Cannot activate hack");
                return;
            }

            // Force immediate activation
            OnDroneArrived();
        }

        private void EndHack()
        {
            if (targetSystem != null)
            {
                targetSystem.RemoveHack();
            }

            isHacking = false;
            currentTimer = 0f;

            Debug.Log("[HackingSystem] Hack ended");

            GameEvents.TriggerHackEnded(new HackEndedArgs
            {
                targetSystem = targetSystem?.SystemType ?? SystemType.None
            });
        }

        public void RecallDrone()
        {
            if (!isDeployed) return;

            if (isHacking)
            {
                EndHack();
            }

            if (activeDrone != null)
            {
                Destroy(activeDrone.gameObject);
                activeDrone = null;
            }

            isDeployed = false;
            targetSystem = null;

            Debug.Log("[HackingSystem] Drone recalled");
        }

        public override void OnDamaged(int amount)
        {
            base.OnDamaged(amount);

            // Damage can disrupt active hack
            if (isHacking && damageLevel >= currentLevel)
            {
                EndHack();
                Debug.Log("[HackingSystem] Hack disrupted by damage!");
            }
        }
    }

    /// <summary>
    /// Hack drone that travels to target.
    /// </summary>
    public class HackDrone : MonoBehaviour
    {
        private ShipController source;
        private ShipSystem target;
        private float travelTime;
        private float progress = 0f;
        private Vector3 startPos;
        private Vector3 endPos;
        private System.Action onArrival;

        public void Deploy(ShipController sourceShip, ShipSystem targetSystem, float time, System.Action callback)
        {
            source = sourceShip;
            target = targetSystem;
            travelTime = time;
            onArrival = callback;

            startPos = sourceShip.transform.position;
            endPos = targetSystem.transform.position;
            transform.position = startPos;
        }

        private void Update()
        {
            if (target == null) return;

            progress += Time.deltaTime / travelTime;
            transform.position = Vector3.Lerp(startPos, endPos, progress);

            if (progress >= 1f)
            {
                onArrival?.Invoke();
                enabled = false;
            }
        }
    }
}
