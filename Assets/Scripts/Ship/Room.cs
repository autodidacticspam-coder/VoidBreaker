using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Represents a room on a ship. Rooms can contain systems, crew, fires, and breaches.
    /// </summary>
    public class Room : MonoBehaviour, ITargetable
    {
        [Header("Room Info")]
        [SerializeField] private string roomName;
        [SerializeField] private SystemType roomType;
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private Vector2Int size = new Vector2Int(2, 2);

        [Header("State")]
        [SerializeField] private int oxygenLevel = 100;
        [SerializeField] private int fireLevel = 0;
        [SerializeField] private int breachLevel = 0;
        [SerializeField] private bool doorsOpen = false;

        [Header("References")]
        [SerializeField] private ShipController parentShip;
        [SerializeField] private ShipSystem assignedSystem;
        [SerializeField] private List<Room> adjacentRooms = new List<Room>();
        [SerializeField] private List<ICrewMember> crewInRoom = new List<ICrewMember>();

        // ITargetable implementation
        public Vector2 Position => transform.position;
        public TargetType TargetType => TargetType.Room;
        public bool CanBeTargeted => true;
        public string TargetName => roomName;

        // Public accessors
        public string RoomName => roomName;
        public SystemType RoomType => roomType;
        public Vector2Int GridPosition => gridPosition;
        public Vector2Int Size => size;
        public int OxygenLevel => oxygenLevel;
        public int FireLevel => fireLevel;
        public int BreachLevel => breachLevel;
        public bool HasFire => fireLevel > 0;
        public bool HasBreach => breachLevel > 0;
        public bool IsVacuum => oxygenLevel <= 0;
        public ShipSystem System => assignedSystem;
        public IReadOnlyList<Room> AdjacentRooms => adjacentRooms;
        public IReadOnlyList<ICrewMember> Crew => crewInRoom;
        public int CrewCount => crewInRoom.Count;

        // Compatibility method aliases
        public IReadOnlyList<ICrewMember> GetCrewInRoom() => crewInRoom;
        public void SetOxygenLevel(int level) => SetOxygen(level);

        // Events
        public event Action<Room> OnOxygenChanged;
        public event Action<Room> OnFireChanged;
        public event Action<Room> OnBreachChanged;

        // Constants
        private const float OXYGEN_DRAIN_RATE = 5f; // Per second when breached
        private const float OXYGEN_FILL_RATE = 10f; // Per second when connected to O2
        private const float FIRE_SPREAD_CHANCE = 0.1f; // Per second
        private const float FIRE_DAMAGE_INTERVAL = 1f;
        private const int FIRE_DAMAGE_TO_SYSTEM = 1;
        private const int FIRE_DAMAGE_TO_CREW = 10;

        private float fireDamageTimer = 0f;

        public void Initialize(RoomDefinition def, ShipController ship)
        {
            roomName = def.roomName;
            roomType = def.roomType;
            gridPosition = def.gridPosition;
            size = def.size;
            parentShip = ship;

            oxygenLevel = 100;
            fireLevel = 0;
            breachLevel = 0;

            Debug.Log($"[Room] {roomName} initialized at {gridPosition}");
        }

        public void AssignSystem(ShipSystem system)
        {
            assignedSystem = system;
            roomType = system.SystemType;
        }

        public void FindAdjacentRooms(List<Room> allRooms)
        {
            adjacentRooms.Clear();

            foreach (var room in allRooms)
            {
                if (room == this) continue;

                // Check if rooms share an edge
                if (IsAdjacent(room))
                {
                    adjacentRooms.Add(room);
                }
            }
        }

        private bool IsAdjacent(Room other)
        {
            // Check if rooms share any edge
            // This is simplified - real implementation would check grid positions
            float distance = Vector2.Distance(transform.position, other.transform.position);
            return distance < 3f; // Arbitrary threshold
        }

        // ==================== OXYGEN ====================

        public void DrainOxygen(int amount)
        {
            oxygenLevel = Mathf.Max(0, oxygenLevel - amount);
            OnOxygenChanged?.Invoke(this);
        }

        public void FillOxygen(int amount)
        {
            oxygenLevel = Mathf.Min(100, oxygenLevel + amount);
            OnOxygenChanged?.Invoke(this);
        }

        public void SetOxygen(int level)
        {
            oxygenLevel = Mathf.Clamp(level, 0, 100);
            OnOxygenChanged?.Invoke(this);
        }

        // ==================== FIRE ====================

        public void StartFire()
        {
            if (fireLevel >= 3) return;

            fireLevel = Mathf.Min(3, fireLevel + 1);
            OnFireChanged?.Invoke(this);

            GameEvents.TriggerRoomFire(new RoomFireArgs
            {
                room = this,
                fireLevel = fireLevel
            });

            Debug.Log($"[Room] {roomName} fire started! Level: {fireLevel}");
        }

        public void ExtinguishFire(int amount = 1)
        {
            if (fireLevel <= 0) return;

            fireLevel = Mathf.Max(0, fireLevel - amount);
            OnFireChanged?.Invoke(this);

            Debug.Log($"[Room] {roomName} fire reduced to {fireLevel}");
        }

        // ==================== BREACH ====================

        public void CreateBreach()
        {
            if (breachLevel >= 2) return;

            breachLevel = Mathf.Min(2, breachLevel + 1);
            OnBreachChanged?.Invoke(this);

            GameEvents.TriggerRoomBreach(new RoomBreachArgs
            {
                room = this,
                breachLevel = breachLevel
            });

            Debug.Log($"[Room] {roomName} breached! Level: {breachLevel}");
        }

        public void RepairBreach(int amount = 1)
        {
            if (breachLevel <= 0) return;

            breachLevel = Mathf.Max(0, breachLevel - amount);
            OnBreachChanged?.Invoke(this);

            Debug.Log($"[Room] {roomName} breach repaired. Level: {breachLevel}");
        }

        // ==================== CREW ====================

        public void CrewEntered(ICrewMember crew)
        {
            if (!crewInRoom.Contains(crew))
            {
                crewInRoom.Add(crew);
            }
        }

        public void CrewLeft(ICrewMember crew)
        {
            crewInRoom.Remove(crew);

            // If crew was manning the system, update it
            if (assignedSystem != null && assignedSystem.ManningCrew == crew)
            {
                assignedSystem.OnCrewLeft();
            }
        }

        public bool HasHostileCrew(bool isPlayerRoom)
        {
            foreach (var crew in crewInRoom)
            {
                if (crew.IsPlayerControlled != isPlayerRoom)
                    return true;
            }
            return false;
        }

        public ICrewMember GetHostileCrew(bool isPlayerRoom)
        {
            foreach (var crew in crewInRoom)
            {
                if (crew.IsPlayerControlled != isPlayerRoom)
                    return crew;
            }
            return null;
        }

        // ==================== DOORS ====================

        public void OpenDoors()
        {
            doorsOpen = true;
        }

        public void CloseDoors()
        {
            doorsOpen = false;
        }

        public void ToggleDoors()
        {
            doorsOpen = !doorsOpen;
        }

        public bool CanVentTo(Room other)
        {
            if (!adjacentRooms.Contains(other)) return false;
            return doorsOpen || other.doorsOpen;
        }

        // ==================== UPDATE ====================

        public void Tick(float deltaTime)
        {
            // Handle breach oxygen drain
            if (breachLevel > 0)
            {
                float drain = OXYGEN_DRAIN_RATE * breachLevel * deltaTime;
                DrainOxygen(Mathf.RoundToInt(drain));
            }

            // Handle fire
            if (fireLevel > 0)
            {
                // Fire consumes oxygen
                float oxygenConsumption = fireLevel * 2f * deltaTime;
                DrainOxygen(Mathf.RoundToInt(oxygenConsumption));

                // Fire goes out in vacuum
                if (oxygenLevel <= 0)
                {
                    fireLevel = 0;
                    OnFireChanged?.Invoke(this);
                }
                else
                {
                    // Fire damages system and crew periodically
                    fireDamageTimer += deltaTime;
                    if (fireDamageTimer >= FIRE_DAMAGE_INTERVAL)
                    {
                        fireDamageTimer = 0f;
                        ApplyFireDamage();
                    }

                    // Fire can spread
                    if (UnityEngine.Random.value < FIRE_SPREAD_CHANCE * deltaTime)
                    {
                        SpreadFireToAdjacent();
                    }
                }
            }

            // Damage crew in vacuum
            if (oxygenLevel <= 0)
            {
                foreach (var crew in crewInRoom)
                {
                    // Some races are immune to vacuum
                    if (crew.Race == CrewRace.Crystalline ||
                        crew.Race == CrewRace.Lanius ||
                        crew.Race == CrewRace.Synthetic)
                        continue;

                    crew.TakeDamage(new DamageInfo(5, DamageType.Suffocation));
                }
            }
        }

        private void ApplyFireDamage()
        {
            // Damage system
            if (assignedSystem != null)
            {
                assignedSystem.TakeDamage(FIRE_DAMAGE_TO_SYSTEM);
            }

            // Damage crew
            foreach (var crew in crewInRoom)
            {
                crew.TakeDamage(new DamageInfo(FIRE_DAMAGE_TO_CREW, DamageType.Fire));
            }
        }

        private void SpreadFireToAdjacent()
        {
            if (adjacentRooms.Count == 0) return;

            // Pick random adjacent room
            var target = adjacentRooms[UnityEngine.Random.Range(0, adjacentRooms.Count)];

            // Fire can spread through open doors or damaged ones
            if (doorsOpen || target.doorsOpen)
            {
                if (target.oxygenLevel > 20 && target.fireLevel < fireLevel)
                {
                    target.StartFire();
                }
            }
        }

        // ==================== SAVE/LOAD ====================

        public RoomSaveData GetSaveData()
        {
            return new RoomSaveData
            {
                roomType = roomType,
                oxygenLevel = oxygenLevel,
                fireLevel = fireLevel,
                breachLevel = breachLevel,
                doorsOpen = doorsOpen
            };
        }

        public void LoadFromSave(RoomSaveData data)
        {
            oxygenLevel = data.oxygenLevel;
            fireLevel = data.fireLevel;
            breachLevel = data.breachLevel;
            doorsOpen = data.doorsOpen;
        }
    }

    [Serializable]
    public class RoomDefinition
    {
        public string roomName;
        public SystemType roomType;
        public Vector2Int gridPosition;
        public Vector2Int size;
        public Vector3 position;
    }

    [Serializable]
    public class RoomLayout
    {
        public RoomDefinition[] rooms;
    }

    [Serializable]
    public class RoomSaveData
    {
        public SystemType roomType;
        public int oxygenLevel;
        public int fireLevel;
        public int breachLevel;
        public bool doorsOpen;
    }
}
