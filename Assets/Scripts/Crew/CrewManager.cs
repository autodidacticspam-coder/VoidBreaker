using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;

namespace VoidBreaker.Crew
{
    /// <summary>
    /// Manages all crew members on the player ship.
    /// Handles hiring, morale, relationships, and crew-wide operations.
    /// </summary>
    public class CrewManager : MonoBehaviour
    {
        [Header("Crew")]
        [SerializeField] private List<CrewMember> crew = new List<CrewMember>();
        [SerializeField] private int maxCrew = 8;

        [Header("Morale")]
        [SerializeField] private int currentMorale = 50;
        [SerializeField] private int maxMorale = 100;

        [Header("Relationships")]
        [SerializeField] private RelationshipSystem relationships;

        // Public accessors
        public IReadOnlyList<CrewMember> Crew => crew;
        public int CrewCount => crew.Count;
        public int MaxCrew => maxCrew;
        public int CurrentMorale => currentMorale;
        public MoraleLevel MoraleLevel => GetMoraleLevel();
        public RelationshipSystem Relationships => relationships;

        // Events
        public event Action<CrewMember> OnCrewAdded;
        public event Action<CrewMember> OnCrewRemoved;
        public event Action<int> OnMoraleChanged;

        private void Awake()
        {
            relationships = new RelationshipSystem();
        }

        private void Start()
        {
            // Subscribe to events
            GameEvents.OnCombatEnded += HandleCombatEnded;
            GameEvents.OnCrewDied += HandleCrewDied;
        }

        private void OnDestroy()
        {
            GameEvents.OnCombatEnded -= HandleCombatEnded;
            GameEvents.OnCrewDied -= HandleCrewDied;
        }

        // ==================== CREW MANAGEMENT ====================

        /// <summary>
        /// Adds a new crew member to the ship.
        /// </summary>
        public bool AddCrew(CrewMember newCrew)
        {
            if (crew.Count >= maxCrew)
            {
                Debug.Log("[CrewManager] Ship is at max crew capacity");
                return false;
            }

            crew.Add(newCrew);

            // Add to relationship tracking
            relationships.AddCrew(newCrew);

            OnCrewAdded?.Invoke(newCrew);

            GameEvents.TriggerCrewHired(new CrewHiredArgs
            {
                crew = newCrew,
                cost = 0
            });

            Debug.Log($"[CrewManager] {newCrew.CrewName} joined the crew ({crew.Count}/{maxCrew})");
            return true;
        }

        /// <summary>
        /// Hires a new crew member at a shop.
        /// </summary>
        public CrewMember HireCrew(string name, CrewRace race, Room startingRoom, int cost)
        {
            if (crew.Count >= maxCrew)
            {
                Debug.Log("[CrewManager] Cannot hire - at max crew capacity");
                return null;
            }

            var runManager = GameManager.Instance?.CurrentRun;
            if (runManager != null && !runManager.SpendScrap(cost))
            {
                Debug.Log("[CrewManager] Cannot afford this crew member");
                return null;
            }

            var crewObj = new GameObject($"Crew_{name}");
            crewObj.transform.SetParent(transform);

            var newCrew = crewObj.AddComponent<CrewMember>();
            newCrew.Initialize(name, race, startingRoom);

            AddCrew(newCrew);

            // Slight morale boost for new crew
            ChangeMorale(3, "New crew member joined");

            return newCrew;
        }

        /// <summary>
        /// Removes a crew member (death, dismissal, etc.)
        /// </summary>
        public void RemoveCrew(CrewMember crewMember)
        {
            if (!crew.Contains(crewMember)) return;

            crew.Remove(crewMember);
            relationships.RemoveCrew(crewMember);

            OnCrewRemoved?.Invoke(crewMember);

            Debug.Log($"[CrewManager] {crewMember.CrewName} removed from crew ({crew.Count}/{maxCrew})");
        }

        /// <summary>
        /// Increases max crew capacity (from mutations/augments).
        /// </summary>
        public void IncreaseMaxCrew(int amount = 1)
        {
            maxCrew += amount;
            Debug.Log($"[CrewManager] Max crew increased to {maxCrew}");
        }

        // ==================== CREW COMMANDS ====================

        /// <summary>
        /// Commands all crew to stations for combat.
        /// </summary>
        public void SendAllToStations(ShipController ship)
        {
            // Priority: Piloting > Engines > Shields > Weapons
            var systems = new[]
            {
                ship.GetSystem(SystemType.Piloting),
                ship.GetSystem(SystemType.Engines),
                ship.GetSystem(SystemType.Shields),
                ship.GetSystem(SystemType.Weapons)
            };

            int crewIndex = 0;
            foreach (var system in systems)
            {
                if (system == null || crewIndex >= crew.Count) continue;

                crew[crewIndex].SetManning(system);
                crewIndex++;
            }
        }

        /// <summary>
        /// Commands crew to repair a specific room.
        /// </summary>
        public void SendToRepair(Room room, int count = 1)
        {
            int sent = 0;
            foreach (var member in crew)
            {
                if (sent >= count) break;
                if (member.CurrentTask == CrewTask.Idle || member.CurrentTask == CrewTask.Moving)
                {
                    member.MoveTo(room);
                    member.AssignTask(CrewTask.Repairing);
                    sent++;
                }
            }
        }

        /// <summary>
        /// Gets idle crew members.
        /// </summary>
        public List<CrewMember> GetIdleCrew()
        {
            var idle = new List<CrewMember>();
            foreach (var member in crew)
            {
                if (member.CurrentTask == CrewTask.Idle)
                    idle.Add(member);
            }
            return idle;
        }

        /// <summary>
        /// Gets crew members in a specific room.
        /// </summary>
        public List<CrewMember> GetCrewInRoom(Room room)
        {
            var inRoom = new List<CrewMember>();
            foreach (var member in crew)
            {
                if (member.CurrentRoom == room)
                    inRoom.Add(member);
            }
            return inRoom;
        }

        // ==================== MORALE ====================

        /// <summary>
        /// Changes morale by the specified amount.
        /// </summary>
        public void ChangeMorale(int amount, string reason = "")
        {
            int oldMorale = currentMorale;
            currentMorale = Mathf.Clamp(currentMorale + amount, 0, maxMorale);

            if (currentMorale != oldMorale)
            {
                OnMoraleChanged?.Invoke(currentMorale);

                GameEvents.TriggerMoraleChanged(new MoraleChangedArgs
                {
                    newMorale = currentMorale,
                    change = amount,
                    reason = reason
                });

                Debug.Log($"[CrewManager] Morale {(amount > 0 ? "+" : "")}{amount}: {reason} (now {currentMorale})");
            }
        }

        private MoraleLevel GetMoraleLevel()
        {
            if (currentMorale <= 20) return MoraleLevel.Desperate;
            if (currentMorale <= 40) return MoraleLevel.Low;
            if (currentMorale <= 60) return MoraleLevel.Normal;
            if (currentMorale <= 80) return MoraleLevel.High;
            return MoraleLevel.Inspired;
        }

        /// <summary>
        /// Gets the stat modifier from current morale.
        /// </summary>
        public float GetMoraleModifier()
        {
            return MoraleLevel switch
            {
                MoraleLevel.Desperate => 0.85f,
                MoraleLevel.Low => 0.9f,
                MoraleLevel.Normal => 1f,
                MoraleLevel.High => 1.05f,
                MoraleLevel.Inspired => 1.1f,
                _ => 1f
            };
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleCombatEnded(CombatEndedArgs args)
        {
            if (args.playerWon)
            {
                // Victory boosts morale
                ChangeMorale(5, "Victory in combat");

                // Crew who fought together build bonds
                var roomsWithCrew = new Dictionary<Room, List<CrewMember>>();
                foreach (var member in crew)
                {
                    if (member.CurrentRoom != null)
                    {
                        if (!roomsWithCrew.ContainsKey(member.CurrentRoom))
                            roomsWithCrew[member.CurrentRoom] = new List<CrewMember>();
                        roomsWithCrew[member.CurrentRoom].Add(member);
                    }
                }

                foreach (var kvp in roomsWithCrew)
                {
                    if (kvp.Value.Count >= 2)
                    {
                        for (int i = 0; i < kvp.Value.Count; i++)
                        {
                            for (int j = i + 1; j < kvp.Value.Count; j++)
                            {
                                relationships.AddRelationshipPoints(kvp.Value[i], kvp.Value[j], 3);
                            }
                        }
                    }
                }
            }
            else
            {
                // Defeat hurts morale (but we survived)
                ChangeMorale(-10, "Narrow escape");
            }
        }

        private void HandleCrewDied(CrewDiedArgs args)
        {
            var deceased = args.crew as CrewMember;
            if (deceased == null || !crew.Contains(deceased)) return;

            // Remove from crew
            RemoveCrew(deceased);

            // Major morale hit
            ChangeMorale(-15, $"{deceased.CrewName} died");

            // Extra morale hit for bonded crew
            foreach (var member in crew)
            {
                var relation = relationships.GetRelationship(deceased, member);
                if (relation >= RelationshipType.Friends)
                {
                    ChangeMorale(-5, $"Lost a friend");
                }
            }
        }

        // ==================== SAVE/LOAD ====================

        public List<CrewSaveData> GetSaveData()
        {
            var data = new List<CrewSaveData>();
            foreach (var member in crew)
            {
                data.Add(member.GetSaveData());
            }
            return data;
        }

        public void LoadFromSave(List<CrewSaveData> data)
        {
            // Clear existing crew
            foreach (var member in crew)
            {
                Destroy(member.gameObject);
            }
            crew.Clear();

            // Recreate from save data
            foreach (var saveData in data)
            {
                var crewObj = new GameObject($"Crew_{saveData.crewName}");
                crewObj.transform.SetParent(transform);

                var member = crewObj.AddComponent<CrewMember>();
                member.LoadFromSave(saveData);

                crew.Add(member);
                relationships.AddCrew(member);
            }
        }
    }

    public enum MoraleLevel
    {
        Desperate,
        Low,
        Normal,
        High,
        Inspired
    }

    /// <summary>
    /// Tracks relationships between crew members.
    /// </summary>
    [Serializable]
    public class RelationshipSystem
    {
        private Dictionary<(ICrewMember, ICrewMember), int> relationshipPoints =
            new Dictionary<(ICrewMember, ICrewMember), int>();

        public void AddCrew(ICrewMember crew)
        {
            // Initialize relationships with existing crew as strangers
        }

        public void RemoveCrew(ICrewMember crew)
        {
            // Remove all relationships involving this crew member
            var toRemove = new List<(ICrewMember, ICrewMember)>();
            foreach (var key in relationshipPoints.Keys)
            {
                if (key.Item1 == crew || key.Item2 == crew)
                    toRemove.Add(key);
            }
            foreach (var key in toRemove)
            {
                relationshipPoints.Remove(key);
            }
        }

        public void AddRelationshipPoints(ICrewMember a, ICrewMember b, int points)
        {
            var key = GetKey(a, b);

            if (!relationshipPoints.ContainsKey(key))
                relationshipPoints[key] = 0;

            int oldPoints = relationshipPoints[key];
            relationshipPoints[key] = Mathf.Clamp(relationshipPoints[key] + points, -100, 100);

            // Check for relationship level change
            var oldType = PointsToRelationType(oldPoints);
            var newType = PointsToRelationType(relationshipPoints[key]);

            if (oldType != newType)
            {
                GameEvents.TriggerRelationshipChanged(new RelationshipChangedArgs
                {
                    crewA = a,
                    crewB = b,
                    newRelationshipLevel = relationshipPoints[key],
                    type = newType
                });
            }
        }

        public int GetRelationshipPoints(ICrewMember a, ICrewMember b)
        {
            var key = GetKey(a, b);
            return relationshipPoints.TryGetValue(key, out int points) ? points : 0;
        }

        public RelationshipType GetRelationship(ICrewMember a, ICrewMember b)
        {
            int points = GetRelationshipPoints(a, b);
            return PointsToRelationType(points);
        }

        private (ICrewMember, ICrewMember) GetKey(ICrewMember a, ICrewMember b)
        {
            // Ensure consistent key ordering
            return a.GetHashCode() < b.GetHashCode() ? (a, b) : (b, a);
        }

        private RelationshipType PointsToRelationType(int points)
        {
            if (points < -50) return RelationshipType.Rivals;
            if (points <= 20) return RelationshipType.Strangers;
            if (points <= 50) return RelationshipType.Acquaintances;
            if (points <= 80) return RelationshipType.Friends;
            return RelationshipType.Bonded;
        }
    }

    public enum RelationshipType
    {
        Rivals,
        Strangers,
        Acquaintances,
        Friends,
        Bonded
    }
}
