using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Data;
using VoidBreaker.Crew;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Main controller for a ship (player or enemy).
    /// Manages hull, systems, rooms, and overall ship state.
    /// </summary>
    public class ShipController : MonoBehaviour, IDamageable, ITargetable
    {
        [Header("Ship Definition")]
        [SerializeField] private ShipDefinition definition;
        [SerializeField] private int layoutIndex;

        [Header("Hull")]
        [SerializeField] private int maxHull;
        [SerializeField] private int currentHull;

        [Header("Systems")]
        [SerializeField] private List<ShipSystem> systems = new List<ShipSystem>();
        [SerializeField] private PowerManager powerManager;

        [Header("Rooms")]
        [SerializeField] private List<Room> rooms = new List<Room>();

        [Header("State")]
        [SerializeField] private bool isPlayerShip;
        [SerializeField] private bool isCloaked;
        [SerializeField] private float cloakTimer;

        // IDamageable implementation
        public int MaxHealth => maxHull;
        public int CurrentHealth => currentHull;
        public bool IsDestroyed => currentHull <= 0;

        // ITargetable implementation
        public Vector2 Position => transform.position;
        public TargetType TargetType => TargetType.Ship;
        public bool CanBeTargeted => !isCloaked && !IsDestroyed;
        public string TargetName => definition?.shipName ?? "Unknown Ship";

        // Public accessors
        public ShipDefinition Definition => definition;
        public bool IsPlayerShip => isPlayerShip;
        public bool IsCloaked => isCloaked;
        public IReadOnlyList<ShipSystem> Systems => systems;
        public IReadOnlyList<Room> Rooms => rooms;
        public PowerManager Power => powerManager;

        // Events
        public event Action<IDamageable, DamageInfo> OnDamaged;
        public event Action<IDamageable> OnDestroyed;
        public event Action OnCloakChanged;

        public void Initialize(ShipDefinition def, int layout)
        {
            definition = def;
            layoutIndex = layout;
            isPlayerShip = true;

            // Set hull
            maxHull = def.baseHull;
            currentHull = maxHull;

            // Create power manager
            var powerObj = new GameObject("PowerManager");
            powerObj.transform.SetParent(transform);
            powerManager = powerObj.AddComponent<PowerManager>();
            powerManager.Initialize(def.baseReactor);

            // Create rooms from layout
            CreateRooms(def.roomLayout);

            // Create systems
            CreateSystems(def.startingSystems);

            Debug.Log($"[ShipController] Initialized {def.shipName} with {maxHull} hull");
        }

        public void InitializeAsEnemy(ShipDefinition def, int difficulty)
        {
            definition = def;
            isPlayerShip = false;

            // Scale stats by difficulty
            float scale = 1f + (difficulty * 0.1f);
            maxHull = Mathf.RoundToInt(def.baseHull * scale);
            currentHull = maxHull;

            // Create power manager
            var powerObj = new GameObject("PowerManager");
            powerObj.transform.SetParent(transform);
            powerManager = powerObj.AddComponent<PowerManager>();
            powerManager.Initialize(Mathf.RoundToInt(def.baseReactor * scale));

            // Create rooms and systems
            CreateRooms(def.roomLayout);
            CreateSystems(def.startingSystems);
        }

        private void CreateRooms(RoomLayout layout)
        {
            if (layout == null || layout.rooms == null) return;

            foreach (var roomDef in layout.rooms)
            {
                var roomObj = new GameObject($"Room_{roomDef.roomType}");
                roomObj.transform.SetParent(transform);
                roomObj.transform.localPosition = roomDef.position;

                var room = roomObj.AddComponent<Room>();
                room.Initialize(roomDef, this);
                rooms.Add(room);
            }

            // Connect adjacent rooms
            foreach (var room in rooms)
            {
                room.FindAdjacentRooms(rooms);
            }
        }

        private void CreateSystems(SystemLoadout loadout)
        {
            if (loadout == null) return;

            foreach (var systemDef in loadout.systems)
            {
                var system = CreateSystem(systemDef.type);
                if (system != null)
                {
                    system.Initialize(systemDef.startingLevel);
                    systems.Add(system);

                    // Assign to room
                    var room = GetRoomForSystem(systemDef.type);
                    if (room != null)
                    {
                        room.AssignSystem(system);
                    }

                    // Register with power manager
                    powerManager.RegisterSystem(system);
                }
            }
        }

        private ShipSystem CreateSystem(SystemType type)
        {
            var systemObj = new GameObject($"System_{type}");
            systemObj.transform.SetParent(transform);

            ShipSystem system = type switch
            {
                SystemType.Reactor => systemObj.AddComponent<ReactorSystem>(),
                SystemType.Engines => systemObj.AddComponent<EngineSystem>(),
                SystemType.Shields => systemObj.AddComponent<ShieldSystem>(),
                SystemType.Weapons => systemObj.AddComponent<WeaponControlSystem>(),
                SystemType.Piloting => systemObj.AddComponent<PilotingSystem>(),
                SystemType.Sensors => systemObj.AddComponent<SensorSystem>(),
                SystemType.Doors => systemObj.AddComponent<DoorSystem>(),
                SystemType.Medbay => systemObj.AddComponent<MedbaySystem>(),
                SystemType.Oxygen => systemObj.AddComponent<OxygenSystem>(),
                SystemType.Cloak => systemObj.AddComponent<CloakSystem>(),
                _ => systemObj.AddComponent<ShipSystem>()
            };

            return system;
        }

        private Room GetRoomForSystem(SystemType type)
        {
            foreach (var room in rooms)
            {
                if (room.RoomType == type)
                    return room;
            }
            return null;
        }

        // ==================== HULL/DAMAGE ====================

        public void TakeDamage(DamageInfo damage)
        {
            if (IsDestroyed) return;

            // Check shields first (unless damage ignores them)
            if (!damage.ignoresShields)
            {
                var shields = GetSystem<ShieldSystem>();
                if (shields != null && shields.CurrentLayers > 0)
                {
                    shields.AbsorbHit();
                    return; // Shield absorbed the hit
                }
            }

            // Apply hull damage
            int hullDamage = damage.amount;
            currentHull = Mathf.Max(0, currentHull - hullDamage);

            OnDamaged?.Invoke(this, damage);

            GameEvents.TriggerHullChanged(new HullChangedArgs
            {
                currentHull = currentHull,
                maxHull = maxHull,
                change = -hullDamage
            });

            if (currentHull <= 0)
            {
                OnDestroyed?.Invoke(this);

                if (!isPlayerShip)
                {
                    // Enemy destroyed - trigger rewards
                }
                else
                {
                    // Player destroyed - game over
                    GameManager.Instance.EndRun(false);
                }
            }
        }

        public void TakeDamageToRoom(Room room, DamageInfo damage)
        {
            // First check shields
            if (!damage.ignoresShields)
            {
                var shields = GetSystem<ShieldSystem>();
                if (shields != null && shields.CurrentLayers > 0)
                {
                    shields.AbsorbHit();
                    return;
                }
            }

            // Damage the room's system if it has one
            if (room.System != null)
            {
                room.System.TakeDamage(damage.amount);
            }

            // Apply hull damage
            TakeDamage(damage);

            // Handle breach/fire
            if (damage.causesBreach)
            {
                room.CreateBreach();
            }
            if (damage.causesFire)
            {
                room.StartFire();
            }
        }

        public void Heal(int amount)
        {
            if (IsDestroyed) return;

            int oldHull = currentHull;
            currentHull = Mathf.Min(maxHull, currentHull + amount);

            GameEvents.TriggerHullChanged(new HullChangedArgs
            {
                currentHull = currentHull,
                maxHull = maxHull,
                change = currentHull - oldHull
            });
        }

        public void RepairHull(int amount)
        {
            Heal(amount);
        }

        // ==================== SYSTEMS ====================

        public ShipSystem GetSystem(SystemType type)
        {
            foreach (var system in systems)
            {
                if (system.SystemType == type)
                    return system;
            }
            return null;
        }

        public T GetSystem<T>() where T : ShipSystem
        {
            foreach (var system in systems)
            {
                if (system is T typed)
                    return typed;
            }
            return null;
        }

        public bool HasSystem(SystemType type)
        {
            return GetSystem(type) != null;
        }

        public void AddSystem(SystemType type, int level = 1)
        {
            if (HasSystem(type))
            {
                Debug.LogWarning($"Ship already has system: {type}");
                return;
            }

            var system = CreateSystem(type);
            system.Initialize(level);
            systems.Add(system);

            var room = GetRoomForSystem(type);
            if (room != null)
            {
                room.AssignSystem(system);
            }

            powerManager.RegisterSystem(system);
        }

        // ==================== EVASION ====================

        public int CalculateEvasion()
        {
            var engines = GetSystem<EngineSystem>();
            var piloting = GetSystem<PilotingSystem>();

            if (engines == null || piloting == null)
                return 0;

            if (!piloting.IsManned)
                return 0; // Can't dodge without a pilot

            int evasion = 0;

            // Base evasion from engines (5% per power level)
            evasion += engines.PowerAllocated * 5;

            // Bonus from piloting skill
            if (piloting.ManningCrew != null)
            {
                evasion += piloting.ManningCrew.Skills.GetSkill(SkillType.Piloting) * 5;
            }

            // Bonus from engine skill
            if (engines.ManningCrew != null)
            {
                evasion += engines.ManningCrew.Skills.GetSkill(SkillType.Engines) * 5;
            }

            // Cloak bonus
            if (isCloaked)
            {
                evasion += 60;
            }

            // Cap at 95%
            return Mathf.Min(95, evasion);
        }

        public bool TryEvade()
        {
            int evasion = CalculateEvasion();
            int roll = UnityEngine.Random.Range(0, 100);

            bool evaded = roll < evasion;

            if (evaded)
            {
                // Grant piloting/engine experience
                var piloting = GetSystem<PilotingSystem>();
                var engines = GetSystem<EngineSystem>();

                piloting?.ManningCrew?.GainExperience(SkillType.Piloting, 1);
                engines?.ManningCrew?.GainExperience(SkillType.Engines, 1);
            }

            return evaded;
        }

        // ==================== CLOAK ====================

        public void ActivateCloak()
        {
            var cloak = GetSystem<CloakSystem>();
            if (cloak == null || !cloak.IsOperational) return;

            isCloaked = true;
            cloakTimer = cloak.CloakDuration;

            OnCloakChanged?.Invoke();
        }

        public void DeactivateCloak()
        {
            isCloaked = false;
            cloakTimer = 0;

            OnCloakChanged?.Invoke();
        }

        // ==================== UPDATE ====================

        private void Update()
        {
            if (IsDestroyed) return;

            // Update cloak timer
            if (isCloaked)
            {
                cloakTimer -= Time.deltaTime;
                if (cloakTimer <= 0)
                {
                    DeactivateCloak();
                }
            }

            // Update all systems
            foreach (var system in systems)
            {
                system.Tick(Time.deltaTime);
            }

            // Update all rooms
            foreach (var room in rooms)
            {
                room.Tick(Time.deltaTime);
            }
        }

        // ==================== SAVE/LOAD ====================

        public ShipSaveData GetSaveData()
        {
            var data = new ShipSaveData
            {
                definitionId = definition.name,
                layoutIndex = layoutIndex,
                currentHull = currentHull,
                maxHull = maxHull,
                systemStates = new List<SystemSaveData>(),
                roomStates = new List<RoomSaveData>()
            };

            foreach (var system in systems)
            {
                data.systemStates.Add(system.GetSaveData());
            }

            foreach (var room in rooms)
            {
                data.roomStates.Add(room.GetSaveData());
            }

            return data;
        }

        public void LoadFromSave(ShipSaveData data)
        {
            currentHull = data.currentHull;
            maxHull = data.maxHull;

            // Restore system states
            for (int i = 0; i < data.systemStates.Count && i < systems.Count; i++)
            {
                systems[i].LoadFromSave(data.systemStates[i]);
            }

            // Restore room states
            for (int i = 0; i < data.roomStates.Count && i < rooms.Count; i++)
            {
                rooms[i].LoadFromSave(data.roomStates[i]);
            }
        }
    }

    [Serializable]
    public class ShipSaveData
    {
        public string definitionId;
        public int layoutIndex;
        public int currentHull;
        public int maxHull;
        public List<SystemSaveData> systemStates;
        public List<RoomSaveData> roomStates;
    }
}
