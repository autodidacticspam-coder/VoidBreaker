using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;

namespace VoidBreaker.Crew
{
    /// <summary>
    /// Represents a single crew member with stats, skills, and AI behavior.
    /// </summary>
    public class CrewMember : MonoBehaviour, ICrewMember
    {
        [Header("Identity")]
        [SerializeField] private string crewName;
        [SerializeField] private CrewRace race;
        [SerializeField] private bool isPlayerControlled = true;

        [Header("Stats")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private CrewSkills skills = new CrewSkills();

        [Header("State")]
        [SerializeField] private Room currentRoom;
        [SerializeField] private Room targetRoom;
        [SerializeField] private CrewTask currentTask = CrewTask.Idle;
        [SerializeField] private ICrewMember combatTarget;
        [SerializeField] private ShipSystem manningSystem;

        [Header("Movement")]
        [SerializeField] private float movementProgress = 0f;
        [SerializeField] private List<Room> currentPath = new List<Room>();

        // IDamageable implementation
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDestroyed => currentHealth <= 0;

        // ICrewMember implementation
        public string CrewName => crewName;
        public CrewRace Race => race;
        public CrewSkills Skills => skills;
        public Room CurrentRoom => currentRoom;
        public Room TargetRoom => targetRoom;
        public CrewTask CurrentTask => currentTask;
        public bool IsPlayerControlled => isPlayerControlled;
        public bool IsManning => manningSystem != null;

        // Race-based stats
        public float MovementSpeed => GetRaceMovementSpeed();
        public float CombatDamage => GetRaceCombatDamage();
        public float RepairSpeed => GetRaceRepairSpeed();
        public bool IsImmuneToSuffocation => race == CrewRace.Crystalline ||
                                              race == CrewRace.Lanius ||
                                              race == CrewRace.Synthetic;
        public bool IsImmuneToMindControl => race == CrewRace.Synthetic;
        public bool DropsOxygen => race == CrewRace.Lanius;
        public bool ProvidesZoltanPower => race == CrewRace.Zoltan;

        // Events
        public event Action<IDamageable, DamageInfo> OnDamaged;
        public event Action<IDamageable> OnDestroyed;
        public event Action<ICrewMember, Room> OnRoomChanged;
        public event Action<ICrewMember, CrewTask> OnTaskChanged;

        // Experience tracking
        private Dictionary<SkillType, int> experiencePoints = new Dictionary<SkillType, int>();
        private static readonly int[] EXPERIENCE_THRESHOLDS = { 15, 30, 45 }; // For levels 1, 2, 3

        public void Initialize(string name, CrewRace crewRace, Room startingRoom, bool playerControlled = true)
        {
            crewName = name;
            race = crewRace;
            isPlayerControlled = playerControlled;

            // Set race-based max health
            maxHealth = GetRaceMaxHealth();
            currentHealth = maxHealth;

            // Set starting room
            currentRoom = startingRoom;
            startingRoom?.CrewEntered(this);

            // Initialize experience tracking
            foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
            {
                experiencePoints[skill] = 0;
            }

            Debug.Log($"[Crew] {crewName} ({race}) initialized in {startingRoom?.RoomName}");
        }

        private void Update()
        {
            if (IsDestroyed) return;

            float deltaTime = Time.deltaTime;

            // Handle current task
            switch (currentTask)
            {
                case CrewTask.Moving:
                    UpdateMovement(deltaTime);
                    break;

                case CrewTask.Repairing:
                    UpdateRepair(deltaTime);
                    break;

                case CrewTask.Fighting:
                    UpdateCombat(deltaTime);
                    break;

                case CrewTask.Extinguishing:
                    UpdateExtinguish(deltaTime);
                    break;

                case CrewTask.Manning:
                    // Just stay at station
                    break;

                case CrewTask.Idle:
                    // Check for auto-tasks
                    CheckAutoTasks();
                    break;
            }

            // Handle Lanius oxygen drain
            if (DropsOxygen && currentRoom != null)
            {
                currentRoom.DrainOxygen(Mathf.RoundToInt(2f * deltaTime));
            }
        }

        // ==================== MOVEMENT ====================

        public void MoveTo(Room target)
        {
            if (target == currentRoom)
            {
                SetTask(CrewTask.Idle);
                return;
            }

            targetRoom = target;

            // Find path
            currentPath = FindPath(currentRoom, target);
            if (currentPath.Count == 0)
            {
                Debug.LogWarning($"[Crew] {crewName} cannot find path to {target.RoomName}");
                return;
            }

            // Stop manning if we were
            StopManning();

            SetTask(CrewTask.Moving);
            movementProgress = 0f;
        }

        private void UpdateMovement(float deltaTime)
        {
            if (currentPath.Count == 0)
            {
                // Arrived at destination
                SetTask(CrewTask.Idle);
                return;
            }

            // Move towards next room in path
            float moveSpeed = MovementSpeed;
            movementProgress += deltaTime * moveSpeed;

            if (movementProgress >= 1f)
            {
                // Arrived at next room
                movementProgress = 0f;

                Room previousRoom = currentRoom;
                currentRoom = currentPath[0];
                currentPath.RemoveAt(0);

                // Update room references
                previousRoom?.CrewLeft(this);
                currentRoom?.CrewEntered(this);

                OnRoomChanged?.Invoke(this, currentRoom);

                GameEvents.TriggerCrewMoved(new CrewMovedArgs
                {
                    crew = this,
                    fromRoom = previousRoom,
                    toRoom = currentRoom
                });

                // Check if we've arrived
                if (currentRoom == targetRoom)
                {
                    SetTask(CrewTask.Idle);
                }
            }
        }

        private List<Room> FindPath(Room from, Room to)
        {
            // Simple BFS pathfinding
            var visited = new HashSet<Room>();
            var queue = new Queue<List<Room>>();
            queue.Enqueue(new List<Room> { from });

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var current = path[path.Count - 1];

                if (current == to)
                {
                    // Remove starting room from path
                    path.RemoveAt(0);
                    return path;
                }

                if (visited.Contains(current))
                    continue;

                visited.Add(current);

                foreach (var adjacent in current.AdjacentRooms)
                {
                    if (!visited.Contains(adjacent))
                    {
                        var newPath = new List<Room>(path) { adjacent };
                        queue.Enqueue(newPath);
                    }
                }
            }

            return new List<Room>(); // No path found
        }

        // ==================== TASKS ====================

        public void AssignTask(CrewTask task)
        {
            SetTask(task);
        }

        private void SetTask(CrewTask task)
        {
            if (currentTask == task) return;

            currentTask = task;
            OnTaskChanged?.Invoke(this, task);
        }

        public void SetManning(ISystem system)
        {
            if (system == null) return;

            // Move to system's room if not there
            var systemRoom = (system as ShipSystem)?.GetComponentInParent<Room>();
            if (systemRoom != null && currentRoom != systemRoom)
            {
                MoveTo(systemRoom);
                // Will need to man after arriving
                return;
            }

            StopManning(); // Stop manning previous system

            manningSystem = system as ShipSystem;
            manningSystem?.OnCrewManned(this);

            // Zoltan power bonus
            if (ProvidesZoltanPower)
            {
                var ship = GetComponentInParent<ShipController>();
                ship?.Power.AddZoltanPower(1);
            }

            SetTask(CrewTask.Manning);
            Debug.Log($"[Crew] {crewName} now manning {manningSystem?.SystemName}");
        }

        public void StopManning()
        {
            if (manningSystem == null) return;

            // Remove Zoltan power bonus
            if (ProvidesZoltanPower)
            {
                var ship = GetComponentInParent<ShipController>();
                ship?.Power.RemoveZoltanPower(1);
            }

            manningSystem.OnCrewLeft();
            manningSystem = null;

            if (currentTask == CrewTask.Manning)
            {
                SetTask(CrewTask.Idle);
            }
        }

        // ==================== COMBAT ====================

        public void Attack(ICrewMember target)
        {
            combatTarget = target;
            SetTask(CrewTask.Fighting);
        }

        public void StopAttacking()
        {
            combatTarget = null;
            if (currentTask == CrewTask.Fighting)
            {
                SetTask(CrewTask.Idle);
            }
        }

        private float combatCooldown = 0f;
        private const float COMBAT_INTERVAL = 1f;

        private void UpdateCombat(float deltaTime)
        {
            if (combatTarget == null || combatTarget.IsDestroyed)
            {
                // Look for another target
                combatTarget = currentRoom?.GetHostileCrew(isPlayerControlled);
                if (combatTarget == null)
                {
                    SetTask(CrewTask.Idle);
                    return;
                }
            }

            // Make sure we're in the same room
            if (combatTarget.CurrentRoom != currentRoom)
            {
                SetTask(CrewTask.Idle);
                return;
            }

            combatCooldown -= deltaTime;
            if (combatCooldown <= 0)
            {
                combatCooldown = COMBAT_INTERVAL;
                DealCombatDamage();
            }
        }

        private void DealCombatDamage()
        {
            float baseDamage = CombatDamage;

            // Skill bonus
            int combatSkill = skills.GetSkill(SkillType.Combat);
            float skillBonus = 1f + (combatSkill * 0.1f);

            int damage = Mathf.RoundToInt(baseDamage * skillBonus);

            combatTarget.TakeDamage(new DamageInfo(damage, DamageType.Combat));

            // Gain combat experience
            GainExperience(SkillType.Combat, 1);
        }

        // ==================== REPAIR ====================

        private void UpdateRepair(float deltaTime)
        {
            if (currentRoom == null)
            {
                SetTask(CrewTask.Idle);
                return;
            }

            // Repair system damage
            if (currentRoom.System != null && currentRoom.System.DamageLevel > 0)
            {
                float repairRate = RepairSpeed * deltaTime;
                // This would accumulate to repair one bar
                currentRoom.System.Repair(Mathf.RoundToInt(repairRate));
                GainExperience(SkillType.Repair, 1);
            }
            // Repair breaches
            else if (currentRoom.HasBreach)
            {
                float repairRate = RepairSpeed * deltaTime;
                // This would accumulate to repair breach
                currentRoom.RepairBreach(Mathf.RoundToInt(repairRate));
                GainExperience(SkillType.Repair, 1);
            }
            else
            {
                SetTask(CrewTask.Idle);
            }
        }

        // ==================== EXTINGUISH ====================

        private void UpdateExtinguish(float deltaTime)
        {
            if (currentRoom == null || !currentRoom.HasFire)
            {
                SetTask(CrewTask.Idle);
                return;
            }

            float extinguishRate = RepairSpeed * deltaTime;
            currentRoom.ExtinguishFire(Mathf.RoundToInt(extinguishRate));
        }

        // ==================== AUTO TASKS ====================

        private void CheckAutoTasks()
        {
            if (currentRoom == null) return;

            // Priority: Combat > Fire > Breach > Repair System

            // Check for enemies
            var hostile = currentRoom.GetHostileCrew(isPlayerControlled);
            if (hostile != null)
            {
                Attack(hostile);
                return;
            }

            // Check for fire
            if (currentRoom.HasFire && !IsImmuneToSuffocation)
            {
                SetTask(CrewTask.Extinguishing);
                return;
            }

            // Check for breach
            if (currentRoom.HasBreach)
            {
                SetTask(CrewTask.Repairing);
                return;
            }

            // Check for system damage
            if (currentRoom.System != null && currentRoom.System.DamageLevel > 0)
            {
                SetTask(CrewTask.Repairing);
                return;
            }
        }

        // ==================== DAMAGE ====================

        public void TakeDamage(DamageInfo damage)
        {
            if (IsDestroyed) return;

            // Immunity checks
            if (damage.type == DamageType.Suffocation && IsImmuneToSuffocation)
                return;

            currentHealth = Mathf.Max(0, currentHealth - damage.amount);

            OnDamaged?.Invoke(this, damage);

            if (currentHealth <= 0)
            {
                Die(damage.type);
            }
        }

        public void Heal(int amount)
        {
            if (IsDestroyed) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

        private void Die(DamageType cause)
        {
            Debug.Log($"[Crew] {crewName} has died! Cause: {cause}");

            // Stop manning
            StopManning();

            // Leave room
            currentRoom?.CrewLeft(this);

            OnDestroyed?.Invoke(this);

            GameEvents.TriggerCrewDied(new CrewDiedArgs
            {
                crew = this,
                causeOfDeath = cause,
                deathRoom = currentRoom
            });

            // Don't destroy immediately - might be cloned
        }

        // ==================== EXPERIENCE ====================

        public void GainExperience(SkillType skill, int amount)
        {
            // Humans learn faster
            if (race == CrewRace.Human)
            {
                amount = Mathf.RoundToInt(amount * 1.1f);
            }

            if (!experiencePoints.ContainsKey(skill))
                experiencePoints[skill] = 0;

            experiencePoints[skill] += amount;

            // Check for level up
            int currentLevel = skills.GetSkill(skill);
            if (currentLevel < CrewSkills.MAX_SKILL_LEVEL)
            {
                int threshold = EXPERIENCE_THRESHOLDS[currentLevel];
                if (experiencePoints[skill] >= threshold)
                {
                    skills.AddExperience(skill, 1);
                    experiencePoints[skill] = 0;

                    GameEvents.TriggerCrewSkillUp(new CrewSkillUpArgs
                    {
                        crew = this,
                        skill = skill,
                        newLevel = skills.GetSkill(skill)
                    });

                    Debug.Log($"[Crew] {crewName} leveled up {skill} to {skills.GetSkill(skill)}!");
                }
            }
        }

        // ==================== RACE STATS ====================

        private int GetRaceMaxHealth()
        {
            return race switch
            {
                CrewRace.Human => 100,
                CrewRace.Vex => 100,
                CrewRace.Crystalline => 125,
                CrewRace.Zoltan => 70,
                CrewRace.Engi => 100,
                CrewRace.Slug => 100,
                CrewRace.Lanius => 100,
                CrewRace.Synthetic => 80,
                _ => 100
            };
        }

        private float GetRaceMovementSpeed()
        {
            return race switch
            {
                CrewRace.Crystalline => 0.5f, // Slow
                CrewRace.Vex => 1.2f, // Fast
                CrewRace.Lanius => 0.8f,
                _ => 1f
            };
        }

        private float GetRaceCombatDamage()
        {
            return race switch
            {
                CrewRace.Vex => 7.5f, // +50% damage (base 5)
                CrewRace.Engi => 2.5f, // -50% damage
                CrewRace.Crystalline => 5f,
                _ => 5f
            };
        }

        private float GetRaceRepairSpeed()
        {
            return race switch
            {
                CrewRace.Engi => 1.5f, // +50% repair
                CrewRace.Vex => 0.5f, // -50% repair
                _ => 1f
            };
        }

        // ==================== SAVE/LOAD ====================

        public CrewSaveData GetSaveData()
        {
            return new CrewSaveData
            {
                crewName = crewName,
                race = race,
                currentHealth = currentHealth,
                maxHealth = maxHealth,
                skills = skills,
                isPlayerControlled = isPlayerControlled,
                currentRoomType = currentRoom?.RoomType ?? SystemType.Piloting
            };
        }

        public void LoadFromSave(CrewSaveData data)
        {
            crewName = data.crewName;
            race = data.race;
            currentHealth = data.currentHealth;
            maxHealth = data.maxHealth;
            skills = data.skills;
            isPlayerControlled = data.isPlayerControlled;
        }
    }

    [Serializable]
    public class CrewSaveData
    {
        public string crewName;
        public CrewRace race;
        public int currentHealth;
        public int maxHealth;
        public CrewSkills skills;
        public bool isPlayerControlled;
        public SystemType currentRoomType;
    }
}
