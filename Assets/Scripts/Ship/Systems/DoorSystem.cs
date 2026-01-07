using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Door control system - manages door states and airlock functionality.
    /// </summary>
    public class DoorSystem : ShipSystem
    {
        [Header("Door State")]
        [SerializeField] private List<Door> allDoors = new List<Door>();
        [SerializeField] private bool doorsLocked = false;
        [SerializeField] private bool airlockMode = false;

        [Header("Level Bonuses")]
        [SerializeField] private float doorStrength = 1f; // HP multiplier for doors
        [SerializeField] private bool canRemoteLock = false; // Level 2+

        public bool DoorsLocked => doorsLocked;
        public bool AirlockMode => airlockMode;
        public float DoorStrength => doorStrength;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.Doors;
            maxPower = 1; // Doors only need 1 power max

            // Level bonuses
            doorStrength = 1f + (level * 0.5f);
            canRemoteLock = level >= 2;

            // Find all doors in ship
            RefreshDoorList();
        }

        public void RefreshDoorList()
        {
            allDoors.Clear();
            allDoors.AddRange(parentShip.GetComponentsInChildren<Door>());

            // Apply door strength
            foreach (var door in allDoors)
            {
                door.SetStrengthMultiplier(doorStrength);
            }
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            // Update door behaviors based on power
            bool hasPower = IsOperational && AllocatedPower > 0;

            foreach (var door in allDoors)
            {
                door.SetPowered(hasPower);
            }
        }

        public void ToggleDoorLock(Door door)
        {
            if (!IsOperational) return;

            door.ToggleLock();
        }

        public void LockAllDoors()
        {
            if (!IsOperational) return;

            foreach (var door in allDoors)
            {
                door.Lock();
            }

            doorsLocked = true;
            Debug.Log("[DoorSystem] All doors locked!");
        }

        public void UnlockAllDoors()
        {
            foreach (var door in allDoors)
            {
                door.Unlock();
            }

            doorsLocked = false;
            Debug.Log("[DoorSystem] All doors unlocked");
        }

        public void OpenAllExteriorDoors()
        {
            if (!IsOperational) return;

            airlockMode = true;

            foreach (var door in allDoors)
            {
                if (door.IsExterior)
                {
                    door.Open();
                }
            }

            Debug.Log("[DoorSystem] AIRLOCK MODE - exterior doors opened!");
        }

        public void CloseAllExteriorDoors()
        {
            foreach (var door in allDoors)
            {
                if (door.IsExterior)
                {
                    door.Close();
                }
            }

            airlockMode = false;
            Debug.Log("[DoorSystem] Exterior doors closed");
        }

        public void SetRoomIsolation(Room room, bool isolated)
        {
            if (!canRemoteLock)
            {
                Debug.Log("[DoorSystem] Remote lock requires level 2+");
                return;
            }

            foreach (var door in allDoors)
            {
                if (door.ConnectsRoom(room))
                {
                    if (isolated)
                        door.Lock();
                    else
                        door.Unlock();
                }
            }

            Debug.Log($"[DoorSystem] Room {room.RoomType} isolation: {isolated}");
        }

        public Door GetDoorBetween(Room roomA, Room roomB)
        {
            foreach (var door in allDoors)
            {
                if (door.ConnectsRooms(roomA, roomB))
                    return door;
            }
            return null;
        }
    }

    /// <summary>
    /// Individual door component.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [SerializeField] private Room roomA;
        [SerializeField] private Room roomB;
        [SerializeField] private bool isOpen = true;
        [SerializeField] private bool isLocked = false;
        [SerializeField] private bool isPowered = true;
        [SerializeField] private bool isExterior = false;

        [SerializeField] private int maxHealth = 3;
        [SerializeField] private int currentHealth = 3;
        [SerializeField] private float strengthMultiplier = 1f;

        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;
        public bool IsExterior => isExterior;
        public bool IsDamaged => currentHealth < maxHealth;
        public bool IsDestroyed => currentHealth <= 0;

        public void Initialize(Room a, Room b, bool exterior = false)
        {
            roomA = a;
            roomB = b;
            isExterior = exterior;
        }

        public void SetStrengthMultiplier(float mult)
        {
            strengthMultiplier = mult;
            maxHealth = Mathf.RoundToInt(3 * strengthMultiplier);
            currentHealth = maxHealth;
        }

        public void SetPowered(bool powered)
        {
            isPowered = powered;

            // Unpowered doors are easier to break
            if (!powered)
            {
                maxHealth = Mathf.RoundToInt(1 * strengthMultiplier);
            }
            else
            {
                maxHealth = Mathf.RoundToInt(3 * strengthMultiplier);
            }
        }

        public void Open()
        {
            if (isLocked && isPowered) return;
            isOpen = true;
        }

        public void Close()
        {
            isOpen = false;
        }

        public void Lock()
        {
            isLocked = true;
            isOpen = false;
        }

        public void Unlock()
        {
            isLocked = false;
        }

        public void ToggleLock()
        {
            if (isLocked)
                Unlock();
            else
                Lock();
        }

        public bool ConnectsRoom(Room room)
        {
            return roomA == room || roomB == room;
        }

        public bool ConnectsRooms(Room a, Room b)
        {
            return (roomA == a && roomB == b) || (roomA == b && roomB == a);
        }

        public Room GetOtherRoom(Room room)
        {
            if (roomA == room) return roomB;
            if (roomB == room) return roomA;
            return null;
        }

        public void TakeDamage(int amount)
        {
            currentHealth -= amount;

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                isOpen = true;
                isLocked = false;
                Debug.Log("[Door] Destroyed!");
            }
        }

        public void Repair(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }
    }
}
