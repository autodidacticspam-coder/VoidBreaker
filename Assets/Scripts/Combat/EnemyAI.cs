using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;

namespace VoidBreaker.Combat
{
    /// <summary>
    /// Controls enemy ship behavior during combat.
    /// Manages power distribution, weapon targeting, and tactical decisions.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ShipController controlledShip;
        [SerializeField] private ShipController targetShip;

        [Header("Behavior")]
        [SerializeField] private AIPersonality personality = AIPersonality.Balanced;
        [SerializeField] private int difficultyRating = 1;

        [Header("Timing")]
        [SerializeField] private float decisionInterval = 2f;
        [SerializeField] private float lastDecisionTime = 0f;

        [Header("Targeting")]
        [SerializeField] private Room currentTargetRoom;
        [SerializeField] private TargetPriority targetPriority = TargetPriority.Shields;

        public int DifficultyRating => difficultyRating;

        public void Initialize(ShipController enemy, ShipController player)
        {
            controlledShip = enemy;
            targetShip = player;

            // Set personality based on ship type or random
            personality = (AIPersonality)Random.Range(0, 4);

            // Determine target priority based on personality
            targetPriority = personality switch
            {
                AIPersonality.Aggressive => TargetPriority.Weapons,
                AIPersonality.Defensive => TargetPriority.Shields,
                AIPersonality.Tactical => TargetPriority.Engines,
                AIPersonality.Chaotic => TargetPriority.Random,
                _ => TargetPriority.Shields
            };

            // Initial power distribution
            DistributePower();

            // Initial targeting
            SelectTarget();

            Debug.Log($"[EnemyAI] Initialized with {personality} personality, targeting {targetPriority}");
        }

        public void Tick(float deltaTime)
        {
            if (controlledShip == null || controlledShip.IsDestroyed)
                return;

            lastDecisionTime += deltaTime;

            // Make decisions periodically
            if (lastDecisionTime >= decisionInterval)
            {
                lastDecisionTime = 0f;
                MakeDecisions();
            }

            // Always try to fire ready weapons
            TryFireWeapons();

            // Manage crew (simplified)
            ManageCrew();
        }

        // ==================== DECISION MAKING ====================

        private void MakeDecisions()
        {
            // Reassess situation
            AssessThreatLevel();

            // Update power distribution if needed
            if (ShouldRedistributePower())
            {
                DistributePower();
            }

            // Update targeting
            SelectTarget();

            // Consider using special abilities
            ConsiderSpecialAbilities();
        }

        private void AssessThreatLevel()
        {
            // Check our hull status
            float hullPercent = (float)controlledShip.CurrentHealth / controlledShip.MaxHealth;

            // If low health, become more defensive or desperate
            if (hullPercent < 0.3f)
            {
                personality = AIPersonality.Defensive;
            }

            // Check if player shields are down
            var playerShields = targetShip.GetSystem<ShieldSystem>();
            if (playerShields != null && playerShields.CurrentLayers == 0)
            {
                // Focus fire!
                personality = AIPersonality.Aggressive;
            }
        }

        private bool ShouldRedistributePower()
        {
            // Check if any critical systems are damaged
            var shields = controlledShip.GetSystem<ShieldSystem>();
            var weapons = controlledShip.GetSystem<WeaponControlSystem>();

            if (shields != null && shields.DamageLevel > 0)
                return true;

            if (weapons != null && weapons.DamageLevel > 0)
                return true;

            return false;
        }

        // ==================== POWER MANAGEMENT ====================

        private void DistributePower()
        {
            var power = controlledShip.Power;
            if (power == null) return;

            // Strategy based on personality
            switch (personality)
            {
                case AIPersonality.Aggressive:
                    // Max weapons, then shields
                    PrioritizeSystems(SystemType.Weapons, SystemType.Shields, SystemType.Engines);
                    break;

                case AIPersonality.Defensive:
                    // Max shields, then engines
                    PrioritizeSystems(SystemType.Shields, SystemType.Engines, SystemType.Weapons);
                    break;

                case AIPersonality.Tactical:
                    // Balance all systems
                    power.AutoDistribute();
                    break;

                case AIPersonality.Chaotic:
                    // Random distribution
                    RandomizePower();
                    break;

                default:
                    power.AutoDistribute();
                    break;
            }
        }

        private void PrioritizeSystems(params SystemType[] priority)
        {
            var power = controlledShip.Power;

            // First, remove all power
            foreach (var system in controlledShip.Systems)
            {
                power.RemoveAllPower(system);
            }

            // Then allocate by priority
            foreach (var type in priority)
            {
                var system = controlledShip.GetSystem(type);
                if (system != null && system.RequiresPower)
                {
                    int maxPower = system.CurrentLevel - system.DamageLevel;
                    int toAllocate = Mathf.Min(maxPower, power.FreePower);
                    power.AllocatePower(system, toAllocate);
                }
            }
        }

        private void RandomizePower()
        {
            var power = controlledShip.Power;

            foreach (var system in controlledShip.Systems)
            {
                if (system.RequiresPower && power.FreePower > 0)
                {
                    int amount = Random.Range(0, Mathf.Min(system.CurrentLevel, power.FreePower + 1));
                    power.AllocatePower(system, amount);
                }
            }
        }

        // ==================== TARGETING ====================

        private void SelectTarget()
        {
            if (targetShip == null) return;

            Room selectedRoom = null;

            switch (targetPriority)
            {
                case TargetPriority.Shields:
                    selectedRoom = FindRoomWithSystem(SystemType.Shields);
                    break;

                case TargetPriority.Weapons:
                    selectedRoom = FindRoomWithSystem(SystemType.Weapons);
                    break;

                case TargetPriority.Engines:
                    selectedRoom = FindRoomWithSystem(SystemType.Engines);
                    break;

                case TargetPriority.Piloting:
                    selectedRoom = FindRoomWithSystem(SystemType.Piloting);
                    break;

                case TargetPriority.Oxygen:
                    selectedRoom = FindRoomWithSystem(SystemType.Oxygen);
                    break;

                case TargetPriority.Medbay:
                    selectedRoom = FindRoomWithSystem(SystemType.Medbay);
                    break;

                case TargetPriority.Random:
                    selectedRoom = GetRandomRoom();
                    break;

                case TargetPriority.MostCrew:
                    selectedRoom = FindRoomWithMostCrew();
                    break;

                case TargetPriority.Damaged:
                    selectedRoom = FindMostDamagedRoom();
                    break;
            }

            // Fallback to random if target not found
            if (selectedRoom == null)
            {
                selectedRoom = GetRandomRoom();
            }

            currentTargetRoom = selectedRoom;

            // Set target for all weapons
            var weapons = controlledShip.GetSystem<WeaponControlSystem>();
            if (weapons != null && currentTargetRoom != null)
            {
                weapons.SetAllTargets(currentTargetRoom);
            }
        }

        private Room FindRoomWithSystem(SystemType type)
        {
            foreach (var room in targetShip.Rooms)
            {
                if (room.RoomType == type)
                    return room;
            }
            return null;
        }

        private Room GetRandomRoom()
        {
            if (targetShip.Rooms.Count == 0) return null;
            return targetShip.Rooms[Random.Range(0, targetShip.Rooms.Count)];
        }

        private Room FindRoomWithMostCrew()
        {
            Room best = null;
            int maxCrew = 0;

            foreach (var room in targetShip.Rooms)
            {
                if (room.CrewCount > maxCrew)
                {
                    maxCrew = room.CrewCount;
                    best = room;
                }
            }

            return best ?? GetRandomRoom();
        }

        private Room FindMostDamagedRoom()
        {
            Room best = null;
            int maxDamage = 0;

            foreach (var room in targetShip.Rooms)
            {
                if (room.System != null && room.System.DamageLevel > maxDamage)
                {
                    maxDamage = room.System.DamageLevel;
                    best = room;
                }
            }

            return best ?? GetRandomRoom();
        }

        // ==================== WEAPONS ====================

        private void TryFireWeapons()
        {
            var weapons = controlledShip.GetSystem<WeaponControlSystem>();
            if (weapons == null) return;

            foreach (var weapon in weapons.Weapons)
            {
                if (weapon.CanFire())
                {
                    weapons.TryFireWeapon(weapon);
                }
            }
        }

        /// <summary>
        /// Forces all weapons to fire immediately (for ambush).
        /// </summary>
        public void ForceFireAll()
        {
            var weapons = controlledShip.GetSystem<WeaponControlSystem>();
            if (weapons == null) return;

            SelectTarget();
            weapons.FireAllReady();
        }

        // ==================== CREW MANAGEMENT ====================

        private void ManageCrew()
        {
            // Simple crew management - send to repair damaged systems
            // In full implementation, would be much more sophisticated

            foreach (var room in controlledShip.Rooms)
            {
                // If room has fire or breach and no crew, send someone
                if ((room.HasFire || room.HasBreach) && room.CrewCount == 0)
                {
                    // Find idle crew and send them
                    // (Simplified - full implementation would use CrewManager)
                }

                // If room has damaged system and no crew, send someone
                if (room.System != null && room.System.DamageLevel > 0 && room.CrewCount == 0)
                {
                    // Send repair crew
                }
            }
        }

        // ==================== SPECIAL ABILITIES ====================

        private void ConsiderSpecialAbilities()
        {
            // Use cloak if available and taking heavy damage
            var cloak = controlledShip.GetSystem(SystemType.Cloak);
            if (cloak != null && cloak.IsOperational && !controlledShip.IsCloaked)
            {
                float hullPercent = (float)controlledShip.CurrentHealth / controlledShip.MaxHealth;
                if (hullPercent < 0.5f)
                {
                    controlledShip.ActivateCloak();
                }
            }

            // Use teleporter if available (for boarding)
            // Use hacking if available
            // etc.
        }
    }

    public enum AIPersonality
    {
        Balanced,
        Aggressive,
        Defensive,
        Tactical,
        Chaotic
    }

    public enum TargetPriority
    {
        Shields,
        Weapons,
        Engines,
        Piloting,
        Oxygen,
        Medbay,
        Random,
        MostCrew,
        Damaged
    }
}
