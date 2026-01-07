using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Oxygen system - maintains breathable atmosphere throughout the ship.
    /// </summary>
    public class OxygenSystem : ShipSystem
    {
        [Header("Oxygen Settings")]
        [SerializeField] private float oxygenProduction = 5f; // Units per second per room
        [SerializeField] private float oxygenDrainPerCrew = 1f; // Consumption per crew per second
        [SerializeField] private float breachDrainRate = 10f; // Drain rate when room has breach

        [Header("State")]
        [SerializeField] private float shipOxygenLevel = 100f; // Average across all rooms

        public float ShipOxygenLevel => shipOxygenLevel;
        public bool IsLowOxygen => shipOxygenLevel < 50f;
        public bool IsCriticalOxygen => shipOxygenLevel < 25f;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.Oxygen;
            maxPower = level;

            // Higher levels = more oxygen production
            oxygenProduction = 3f + (level * 2f);
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (parentShip == null) return;

            float totalOxygen = 0f;
            int roomCount = 0;

            foreach (var room in parentShip.Rooms)
            {
                float roomOxygen = room.OxygenLevel;

                // Oxygen production (only if system is powered)
                if (IsOperational && AllocatedPower > 0)
                {
                    float production = oxygenProduction * ((float)AllocatedPower / maxPower);

                    // Manning bonus
                    if (IsManned)
                        production *= 1.2f;

                    roomOxygen += production * deltaTime;
                }

                // Oxygen drain from crew
                roomOxygen -= room.CrewCount * oxygenDrainPerCrew * deltaTime;

                // Oxygen drain from breaches
                if (room.HasBreach)
                {
                    roomOxygen -= breachDrainRate * deltaTime;
                }

                // Oxygen spread to adjacent rooms (equalization)
                float avgAdjacentOxygen = 0f;
                int adjacentCount = 0;
                foreach (var adjacent in room.AdjacentRooms)
                {
                    // Only spread if door is open
                    if (!room.IsDoorLockedTo(adjacent))
                    {
                        avgAdjacentOxygen += adjacent.OxygenLevel;
                        adjacentCount++;
                    }
                }

                if (adjacentCount > 0)
                {
                    avgAdjacentOxygen /= adjacentCount;
                    float spread = (avgAdjacentOxygen - roomOxygen) * 0.1f * deltaTime;
                    roomOxygen += spread;
                }

                // Clamp and set
                roomOxygen = Mathf.Clamp(roomOxygen, 0f, 100f);
                room.SetOxygenLevel(roomOxygen);

                totalOxygen += roomOxygen;
                roomCount++;
            }

            // Calculate ship average
            if (roomCount > 0)
            {
                shipOxygenLevel = totalOxygen / roomCount;
            }

            // Trigger warnings
            if (IsCriticalOxygen)
            {
                GameEvents.TriggerOxygenCritical(new OxygenCriticalArgs
                {
                    ship = parentShip,
                    oxygenLevel = shipOxygenLevel
                });
            }
        }

        public void VentRoom(Room room)
        {
            if (room == null) return;

            // Instantly drain oxygen from room
            room.SetOxygenLevel(0f);

            Debug.Log($"[OxygenSystem] Vented {room.RoomType}");
        }

        public void VentAllRooms()
        {
            foreach (var room in parentShip.Rooms)
            {
                room.SetOxygenLevel(0f);
            }

            Debug.Log("[OxygenSystem] All rooms vented!");
        }

        public override void OnDamaged(int amount)
        {
            base.OnDamaged(amount);

            // Reduced oxygen production when damaged
            // (handled by AllocatedPower reduction from damage)
        }
    }
}
