using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Crew;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Teleporter system - allows crew to board enemy ships or return home.
    /// </summary>
    public class TeleporterSystem : ShipSystem
    {
        [Header("Teleporter State")]
        [SerializeField] private bool isCharging = false;
        [SerializeField] private bool isReady = true;
        [SerializeField] private float chargeTime = 10f;
        [SerializeField] private float currentCharge = 0f;

        [Header("Capacity")]
        [SerializeField] private int maxTeleportCrew = 2;
        [SerializeField] private List<CrewMember> crewOnPad = new List<CrewMember>();

        [Header("Linked Ships")]
        [SerializeField] private ShipController targetShip;

        public bool IsReady => isReady && IsOperational && AllocatedPower >= RequiredPowerToOperate;
        public bool IsCharging => isCharging;
        public float ChargeProgress => currentCharge / chargeTime;
        public int MaxCrewCapacity => maxTeleportCrew;
        public int CrewOnPad => crewOnPad.Count;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.Teleporter;
            maxPower = level;

            // Higher levels = faster charge, more capacity
            chargeTime = 15f - (level * 2f);
            maxTeleportCrew = 2 + (level / 2);
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (isCharging && AllocatedPower >= RequiredPowerToOperate)
            {
                // Charge rate affected by manning
                float chargeRate = 1f;
                if (IsManned)
                    chargeRate = 1.25f;

                currentCharge += deltaTime * chargeRate;

                if (currentCharge >= chargeTime)
                {
                    isCharging = false;
                    isReady = true;
                    currentCharge = 0f;
                    Debug.Log("[TeleporterSystem] Teleporter ready!");
                }
            }
        }

        public void SetTargetShip(ShipController target)
        {
            targetShip = target;
        }

        public bool AddCrewToPad(CrewMember crew)
        {
            if (crewOnPad.Count >= maxTeleportCrew)
            {
                Debug.Log("[TeleporterSystem] Teleporter pad full");
                return false;
            }

            if (!crewOnPad.Contains(crew))
            {
                crewOnPad.Add(crew);
                return true;
            }

            return false;
        }

        public void RemoveCrewFromPad(CrewMember crew)
        {
            crewOnPad.Remove(crew);
        }

        public bool TryTeleport(Room targetRoom)
        {
            if (!IsReady)
            {
                Debug.Log("[TeleporterSystem] Teleporter not ready");
                return false;
            }

            if (crewOnPad.Count == 0)
            {
                Debug.Log("[TeleporterSystem] No crew on teleporter pad");
                return false;
            }

            if (targetRoom == null || targetShip == null)
            {
                Debug.Log("[TeleporterSystem] No valid target");
                return false;
            }

            // Teleport all crew on pad
            foreach (var crew in crewOnPad)
            {
                TeleportCrew(crew, targetRoom);
            }

            crewOnPad.Clear();

            // Start cooldown
            isReady = false;
            isCharging = true;
            currentCharge = 0f;

            Debug.Log($"[TeleporterSystem] Teleported {crewOnPad.Count} crew to {targetRoom.RoomType}");

            GameEvents.TriggerCrewTeleported(new CrewTeleportedArgs
            {
                fromShip = parentShip,
                toShip = targetShip,
                targetRoom = targetRoom,
                crewCount = crewOnPad.Count
            });

            return true;
        }

        private void TeleportCrew(CrewMember crew, Room targetRoom)
        {
            // Move crew to target room on enemy ship
            crew.transform.SetParent(targetRoom.transform);
            crew.transform.position = targetRoom.Position;
            crew.SetCurrentRoom(targetRoom);

            // Mark as boarding
            crew.SetBoardingState(true, targetShip);
        }

        public bool TryRecall()
        {
            if (!IsReady)
            {
                Debug.Log("[TeleporterSystem] Teleporter not ready for recall");
                return false;
            }

            // Find crew on enemy ship that belong to us
            var crewManager = parentShip.GetComponent<CrewManager>();
            if (crewManager == null) return false;

            var boardingCrew = crewManager.GetBoardingCrew();
            if (boardingCrew.Count == 0)
            {
                Debug.Log("[TeleporterSystem] No crew to recall");
                return false;
            }

            // Get teleporter room
            Room teleporterRoom = null;
            foreach (var room in parentShip.Rooms)
            {
                if (room.System == this)
                {
                    teleporterRoom = room;
                    break;
                }
            }

            if (teleporterRoom == null) return false;

            // Recall up to max capacity
            int recalled = 0;
            foreach (var crew in boardingCrew)
            {
                if (recalled >= maxTeleportCrew) break;

                crew.transform.SetParent(teleporterRoom.transform);
                crew.transform.position = teleporterRoom.Position;
                crew.SetCurrentRoom(teleporterRoom);
                crew.SetBoardingState(false, null);
                recalled++;
            }

            // Start cooldown
            isReady = false;
            isCharging = true;
            currentCharge = 0f;

            Debug.Log($"[TeleporterSystem] Recalled {recalled} crew");

            return true;
        }

        public override void OnDamaged(int amount)
        {
            base.OnDamaged(amount);

            // Damage interrupts charging
            if (isCharging)
            {
                currentCharge = Mathf.Max(0, currentCharge - 2f);
            }
        }
    }
}
