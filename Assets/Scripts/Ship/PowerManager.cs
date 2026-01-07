using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Manages power distribution across all ship systems.
    /// </summary>
    public class PowerManager : MonoBehaviour
    {
        [Header("Reactor")]
        [SerializeField] private int maxReactorPower = 8;
        [SerializeField] private int currentReactorLevel = 2;

        [Header("Power State")]
        [SerializeField] private int usedPower = 0;
        [SerializeField] private int zoltanPower = 0;
        [SerializeField] private int backupBatteryPower = 0;
        [SerializeField] private bool backupBatteryActive = false;

        [Header("Systems")]
        [SerializeField] private List<ShipSystem> registeredSystems = new List<ShipSystem>();

        // Public accessors
        public int TotalAvailablePower => currentReactorLevel + zoltanPower + (backupBatteryActive ? backupBatteryPower : 0);
        public int UsedPower => usedPower;
        public int FreePower => TotalAvailablePower - usedPower;
        public int ReactorLevel => currentReactorLevel;
        public int MaxReactorLevel => maxReactorPower;

        // Events
        public event Action OnPowerDistributionChanged;

        public void Initialize(int startingReactor)
        {
            currentReactorLevel = Mathf.Clamp(startingReactor, 1, maxReactorPower);
            usedPower = 0;
            zoltanPower = 0;
            backupBatteryPower = 0;

            Debug.Log($"[PowerManager] Initialized with {currentReactorLevel} reactor power");
        }

        public void RegisterSystem(ShipSystem system)
        {
            if (!registeredSystems.Contains(system))
            {
                registeredSystems.Add(system);
            }
        }

        public void UnregisterSystem(ShipSystem system)
        {
            registeredSystems.Remove(system);
            RecalculateUsedPower();
        }

        // ==================== POWER ALLOCATION ====================

        /// <summary>
        /// Attempts to allocate power to a system.
        /// Returns true if successful.
        /// </summary>
        public bool AllocatePower(ShipSystem system, int amount)
        {
            if (!registeredSystems.Contains(system))
            {
                Debug.LogWarning($"[PowerManager] System {system.SystemName} not registered");
                return false;
            }

            // Check if we have enough free power
            int additionalNeeded = amount - system.PowerAllocated;
            if (additionalNeeded > FreePower)
            {
                Debug.Log($"[PowerManager] Not enough power. Need {additionalNeeded}, have {FreePower}");
                return false;
            }

            // Check if system can accept this much power
            int maxSystemPower = system.CurrentLevel - system.DamageLevel;
            if (amount > maxSystemPower)
            {
                amount = maxSystemPower;
            }

            system.PowerAllocated = amount;
            RecalculateUsedPower();
            NotifyPowerChanged();

            return true;
        }

        /// <summary>
        /// Adds one power to a system if available.
        /// </summary>
        public bool IncreasePower(ShipSystem system)
        {
            return AllocatePower(system, system.PowerAllocated + 1);
        }

        /// <summary>
        /// Removes one power from a system.
        /// </summary>
        public bool DecreasePower(ShipSystem system)
        {
            if (system.PowerAllocated <= 0) return false;

            system.PowerAllocated = system.PowerAllocated - 1;
            RecalculateUsedPower();
            NotifyPowerChanged();

            return true;
        }

        /// <summary>
        /// Removes all power from a system.
        /// </summary>
        public void RemoveAllPower(ShipSystem system)
        {
            system.PowerAllocated = 0;
            RecalculateUsedPower();
            NotifyPowerChanged();
        }

        /// <summary>
        /// Automatically distributes power based on priority.
        /// </summary>
        public void AutoDistribute()
        {
            // Reset all power
            foreach (var system in registeredSystems)
            {
                system.PowerAllocated = 0;
            }
            usedPower = 0;

            // Priority order: Oxygen > Shields > Engines > Piloting > Weapons > Others
            var priorityOrder = new[]
            {
                SystemType.Oxygen,
                SystemType.Shields,
                SystemType.Engines,
                SystemType.Piloting,
                SystemType.Weapons,
                SystemType.Medbay,
                SystemType.Sensors,
                SystemType.Doors
            };

            foreach (var priority in priorityOrder)
            {
                var system = GetSystem(priority);
                if (system != null && system.RequiresPower)
                {
                    int maxPower = system.CurrentLevel - system.DamageLevel;
                    int toAllocate = Mathf.Min(maxPower, FreePower);
                    system.PowerAllocated = toAllocate;
                    RecalculateUsedPower();
                }
            }

            NotifyPowerChanged();
            Debug.Log($"[PowerManager] Auto-distributed. Used: {usedPower}/{TotalAvailablePower}");
        }

        // ==================== REACTOR UPGRADES ====================

        public bool CanUpgradeReactor()
        {
            return currentReactorLevel < maxReactorPower;
        }

        public void UpgradeReactor()
        {
            if (!CanUpgradeReactor())
            {
                Debug.Log("[PowerManager] Reactor at max level");
                return;
            }

            currentReactorLevel++;
            NotifyPowerChanged();
            Debug.Log($"[PowerManager] Reactor upgraded to level {currentReactorLevel}");
        }

        // ==================== ZOLTAN POWER ====================

        public void AddZoltanPower(int amount)
        {
            zoltanPower += amount;
            NotifyPowerChanged();
        }

        public void RemoveZoltanPower(int amount)
        {
            zoltanPower = Mathf.Max(0, zoltanPower - amount);

            // If we now have negative free power, remove from lowest priority systems
            while (usedPower > TotalAvailablePower)
            {
                var lowestPriority = GetLowestPriorityPoweredSystem();
                if (lowestPriority != null)
                {
                    DecreasePower(lowestPriority);
                }
                else
                {
                    break;
                }
            }

            NotifyPowerChanged();
        }

        // ==================== BACKUP BATTERY ====================

        public void SetBackupBatteryCapacity(int capacity)
        {
            backupBatteryPower = capacity;
        }

        public void ActivateBackupBattery()
        {
            if (backupBatteryPower <= 0 || backupBatteryActive) return;

            backupBatteryActive = true;
            NotifyPowerChanged();
            Debug.Log($"[PowerManager] Backup battery activated (+{backupBatteryPower} power)");
        }

        public void DeactivateBackupBattery()
        {
            if (!backupBatteryActive) return;

            backupBatteryActive = false;

            // Remove excess power from systems
            while (usedPower > TotalAvailablePower)
            {
                var lowestPriority = GetLowestPriorityPoweredSystem();
                if (lowestPriority != null)
                {
                    DecreasePower(lowestPriority);
                }
                else
                {
                    break;
                }
            }

            NotifyPowerChanged();
        }

        // ==================== HELPERS ====================

        private void RecalculateUsedPower()
        {
            usedPower = 0;
            foreach (var system in registeredSystems)
            {
                usedPower += system.PowerAllocated;
            }
        }

        private void NotifyPowerChanged()
        {
            OnPowerDistributionChanged?.Invoke();

            GameEvents.TriggerPowerChanged(new PowerChangedArgs
            {
                totalPower = TotalAvailablePower,
                usedPower = usedPower,
                availablePower = FreePower
            });
        }

        private ShipSystem GetSystem(SystemType type)
        {
            foreach (var system in registeredSystems)
            {
                if (system.SystemType == type)
                    return system;
            }
            return null;
        }

        private ShipSystem GetLowestPriorityPoweredSystem()
        {
            // Return the lowest priority system that has power allocated
            // Priority is reversed here - we want to remove from least important first
            var reversePriority = new[]
            {
                SystemType.Doors,
                SystemType.Sensors,
                SystemType.Medbay,
                SystemType.Weapons,
                SystemType.Piloting,
                SystemType.Engines,
                SystemType.Shields,
                SystemType.Oxygen
            };

            foreach (var type in reversePriority)
            {
                var system = GetSystem(type);
                if (system != null && system.PowerAllocated > 0)
                {
                    return system;
                }
            }

            return null;
        }

        // ==================== DISPLAY HELPERS ====================

        public string GetPowerDisplayString()
        {
            string display = $"{usedPower}/{TotalAvailablePower}";

            if (zoltanPower > 0)
                display += $" (+{zoltanPower} Zoltan)";

            if (backupBatteryActive)
                display += $" (+{backupBatteryPower} Battery)";

            return display;
        }
    }
}
