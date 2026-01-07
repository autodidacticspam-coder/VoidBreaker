using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Combat;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Weapon control system - manages weapon slots and power distribution to weapons.
    /// </summary>
    public class WeaponControlSystem : ShipSystem
    {
        [Header("Weapon Slots")]
        [SerializeField] private int maxWeaponSlots = 4;
        [SerializeField] private List<Weapon> weapons = new List<Weapon>();

        [Header("Autofire")]
        [SerializeField] private bool autofireEnabled = false;

        public int MaxWeaponSlots => maxWeaponSlots;
        public IReadOnlyList<Weapon> Weapons => weapons;
        public bool AutofireEnabled => autofireEnabled;

        // Events
        public event Action<Weapon> OnWeaponAdded;
        public event Action<Weapon> OnWeaponRemoved;
        public event Action<Weapon> OnWeaponFired;

        protected override void Awake()
        {
            base.Awake();
            systemName = "Weapons";
            systemType = SystemType.Weapons;
            maxLevel = 8;
        }

        public override void Initialize(int startingLevel)
        {
            base.Initialize(startingLevel);
            weapons.Clear();
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            // Update all weapons
            foreach (var weapon in weapons)
            {
                weapon.Tick(deltaTime);

                // Handle autofire
                if (autofireEnabled && weapon.IsCharged && weapon.HasTarget)
                {
                    TryFireWeapon(weapon);
                }
            }
        }

        // ==================== WEAPON MANAGEMENT ====================

        /// <summary>
        /// Adds a weapon to an available slot.
        /// </summary>
        public bool AddWeapon(Weapon weapon)
        {
            if (weapons.Count >= maxWeaponSlots)
            {
                Debug.Log("[WeaponControl] No available weapon slots");
                return false;
            }

            weapons.Add(weapon);
            weapon.Initialize();
            OnWeaponAdded?.Invoke(weapon);

            Debug.Log($"[WeaponControl] Added weapon: {weapon.WeaponName}");
            return true;
        }

        /// <summary>
        /// Removes a weapon from its slot.
        /// </summary>
        public bool RemoveWeapon(Weapon weapon)
        {
            if (!weapons.Contains(weapon))
                return false;

            weapon.Depower();
            weapons.Remove(weapon);
            OnWeaponRemoved?.Invoke(weapon);

            Debug.Log($"[WeaponControl] Removed weapon: {weapon.WeaponName}");
            return true;
        }

        /// <summary>
        /// Swaps weapon positions.
        /// </summary>
        public void SwapWeaponSlots(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= weapons.Count) return;
            if (indexB < 0 || indexB >= weapons.Count) return;

            var temp = weapons[indexA];
            weapons[indexA] = weapons[indexB];
            weapons[indexB] = temp;
        }

        /// <summary>
        /// Increases max weapon slots (from mutations/augments).
        /// </summary>
        public void AddWeaponSlot()
        {
            maxWeaponSlots++;
            Debug.Log($"[WeaponControl] Max slots increased to {maxWeaponSlots}");
        }

        // ==================== POWER MANAGEMENT ====================

        /// <summary>
        /// Powers up a weapon if enough power is available.
        /// </summary>
        public bool PowerWeapon(Weapon weapon)
        {
            if (!weapons.Contains(weapon)) return false;

            int powerNeeded = weapon.PowerRequired;
            int powerAvailable = EffectivePower - GetUsedWeaponPower();

            if (powerAvailable < powerNeeded)
            {
                Debug.Log($"[WeaponControl] Not enough power for {weapon.WeaponName}");
                return false;
            }

            weapon.Power();
            return true;
        }

        /// <summary>
        /// Depowers a weapon.
        /// </summary>
        public void DepowerWeapon(Weapon weapon)
        {
            if (!weapons.Contains(weapon)) return;
            weapon.Depower();
        }

        /// <summary>
        /// Toggles weapon power state.
        /// </summary>
        public void ToggleWeaponPower(Weapon weapon)
        {
            if (weapon.IsPowered)
                DepowerWeapon(weapon);
            else
                PowerWeapon(weapon);
        }

        /// <summary>
        /// Gets total power used by all powered weapons.
        /// </summary>
        public int GetUsedWeaponPower()
        {
            int used = 0;
            foreach (var weapon in weapons)
            {
                if (weapon.IsPowered)
                    used += weapon.PowerRequired;
            }
            return used;
        }

        /// <summary>
        /// Gets available power for weapons.
        /// </summary>
        public int GetAvailableWeaponPower()
        {
            return EffectivePower - GetUsedWeaponPower();
        }

        // ==================== FIRING ====================

        /// <summary>
        /// Attempts to fire a weapon at its current target.
        /// </summary>
        public bool TryFireWeapon(Weapon weapon)
        {
            if (!weapons.Contains(weapon)) return false;
            if (!weapon.CanFire()) return false;

            weapon.Fire();
            OnWeaponFired?.Invoke(weapon);

            // Grant weapon skill experience to manning crew
            ManningCrew?.GainExperience(SkillType.Weapons, 1);

            GameEvents.TriggerWeaponFired(new WeaponFiredArgs
            {
                weapon = weapon,
                target = weapon.CurrentTarget
            });

            return true;
        }

        /// <summary>
        /// Fires all charged weapons at their targets.
        /// </summary>
        public void FireAllReady()
        {
            foreach (var weapon in weapons)
            {
                if (weapon.CanFire())
                {
                    TryFireWeapon(weapon);
                }
            }
        }

        /// <summary>
        /// Sets target for all weapons.
        /// </summary>
        public void SetAllTargets(Room target)
        {
            foreach (var weapon in weapons)
            {
                weapon.SetTarget(target);
            }
        }

        // ==================== AUTOFIRE ====================

        public void EnableAutofire()
        {
            autofireEnabled = true;
        }

        public void DisableAutofire()
        {
            autofireEnabled = false;
        }

        public void ToggleAutofire()
        {
            autofireEnabled = !autofireEnabled;
        }

        // ==================== CHARGE BONUS ====================

        /// <summary>
        /// Returns the charge speed multiplier from manning bonus.
        /// </summary>
        public float GetChargeSpeedMultiplier()
        {
            if (!IsManned) return 1f;

            int skillLevel = ManningCrew.Skills.GetSkill(SkillType.Weapons);
            return 1f + (skillLevel * 0.1f); // 10% faster per skill level
        }
    }
}
