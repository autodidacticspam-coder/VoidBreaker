using System;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Data;

namespace VoidBreaker.Combat
{
    /// <summary>
    /// Base class for all weapons.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Weapon Definition")]
        [SerializeField] protected WeaponDefinition definition;

        [Header("State")]
        [SerializeField] protected bool isPowered = false;
        [SerializeField] protected float chargeProgress = 0f;
        [SerializeField] protected Room currentTarget;

        [Header("Ammo")]
        [SerializeField] protected bool usesAmmo = false;
        [SerializeField] protected int ammoCost = 1;

        // Public accessors
        public string WeaponName => definition?.weaponName ?? "Unknown Weapon";
        public WeaponType WeaponType => definition?.weaponType ?? WeaponType.Laser;
        public int PowerRequired => definition?.powerRequired ?? 1;
        public float ChargeTime => definition?.chargeTime ?? 10f;
        public int Damage => definition?.damage ?? 1;
        public int ProjectileCount => definition?.projectileCount ?? 1;
        public bool UsesAmmo => definition?.usesAmmo ?? false;
        public int AmmoCost => definition?.ammoCost ?? 1;

        public bool IsPowered => isPowered;
        public float ChargeProgress => chargeProgress;
        public bool IsCharged => chargeProgress >= 1f;
        public bool HasTarget => currentTarget != null;
        public Room CurrentTarget => currentTarget;

        // Events
        public event Action<Weapon> OnCharged;
        public event Action<Weapon> OnFired;

        // Reference to weapon control for charge bonus
        private WeaponControlSystem weaponControl;

        public void Initialize()
        {
            isPowered = false;
            chargeProgress = 0f;
            currentTarget = null;

            // Find weapon control system
            var ship = GetComponentInParent<ShipController>();
            if (ship != null)
            {
                weaponControl = ship.GetSystem<WeaponControlSystem>();
            }
        }

        public void SetDefinition(WeaponDefinition def)
        {
            definition = def;
        }

        // ==================== POWER ====================

        public void Power()
        {
            if (isPowered) return;

            isPowered = true;
            // Don't reset charge - preserve it
            Debug.Log($"[Weapon] {WeaponName} powered");
        }

        public void Depower()
        {
            if (!isPowered) return;

            isPowered = false;
            // Charge decays when depowered
            Debug.Log($"[Weapon] {WeaponName} depowered");
        }

        // ==================== TARGETING ====================

        public void SetTarget(Room target)
        {
            currentTarget = target;
        }

        public void ClearTarget()
        {
            currentTarget = null;
        }

        // ==================== CHARGING ====================

        public virtual void Tick(float deltaTime)
        {
            if (!isPowered)
            {
                // Charge decays when not powered
                chargeProgress = Mathf.Max(0f, chargeProgress - deltaTime * 0.5f);
                return;
            }

            if (chargeProgress < 1f)
            {
                float chargeRate = GetChargeRate();
                chargeProgress += deltaTime / chargeRate;

                if (chargeProgress >= 1f)
                {
                    chargeProgress = 1f;
                    OnCharged?.Invoke(this);
                    Debug.Log($"[Weapon] {WeaponName} charged!");
                }
            }
        }

        protected float GetChargeRate()
        {
            float baseTime = ChargeTime;

            // Apply manning bonus from weapon control
            if (weaponControl != null)
            {
                baseTime /= weaponControl.GetChargeSpeedMultiplier();
            }

            return baseTime;
        }

        // ==================== FIRING ====================

        public bool CanFire()
        {
            if (!isPowered) return false;
            if (!IsCharged) return false;
            if (!HasTarget) return false;

            // Check ammo if required
            if (UsesAmmo)
            {
                var runManager = GameManager.Instance?.CurrentRun;
                if (runManager != null && runManager.State.missiles < AmmoCost)
                    return false;
            }

            return true;
        }

        public virtual void Fire()
        {
            if (!CanFire()) return;

            // Consume ammo
            if (UsesAmmo)
            {
                GameManager.Instance?.CurrentRun?.SpendMissiles(AmmoCost);
            }

            // Reset charge
            chargeProgress = 0f;

            // Create projectiles
            for (int i = 0; i < ProjectileCount; i++)
            {
                FireProjectile(i);
            }

            OnFired?.Invoke(this);
            Debug.Log($"[Weapon] {WeaponName} fired at {currentTarget.RoomName}!");
        }

        protected virtual void FireProjectile(int index)
        {
            // This would create the actual projectile in a full implementation
            // For now, we'll calculate hit/miss directly

            var targetShip = currentTarget.GetComponentInParent<ShipController>();
            if (targetShip == null) return;

            // Roll for evasion
            bool hit = !targetShip.TryEvade();

            if (hit)
            {
                ApplyDamage(currentTarget, targetShip);
            }
            else
            {
                GameEvents.TriggerProjectileMissed(new ProjectileMissedArgs
                {
                    targetShip = targetShip,
                    evasionRoll = targetShip.CalculateEvasion()
                });
            }
        }

        protected virtual void ApplyDamage(Room targetRoom, ShipController targetShip)
        {
            DamageInfo damage = CreateDamageInfo();

            targetShip.TakeDamageToRoom(targetRoom, damage);

            GameEvents.TriggerProjectileHit(new ProjectileHitArgs
            {
                targetRoom = targetRoom,
                damage = damage
            });
        }

        protected virtual DamageInfo CreateDamageInfo()
        {
            return new DamageInfo
            {
                amount = Damage,
                type = WeaponTypeToDamageType(),
                ignoresShields = WeaponType == WeaponType.Missile || WeaponType == WeaponType.Bomb,
                causesBreach = definition?.appliedEffects != null &&
                               Array.Exists(definition.appliedEffects, e => e == StatusEffect.Breach),
                causesFire = definition?.appliedEffects != null &&
                             Array.Exists(definition.appliedEffects, e => e == StatusEffect.Fire),
                ionDamage = WeaponType == WeaponType.Ion ? Damage : 0
            };
        }

        private DamageType WeaponTypeToDamageType()
        {
            return WeaponType switch
            {
                WeaponType.Laser => DamageType.Laser,
                WeaponType.Missile => DamageType.Missile,
                WeaponType.Beam => DamageType.Beam,
                WeaponType.Ion => DamageType.Ion,
                WeaponType.Bomb => DamageType.Missile,
                WeaponType.Flak => DamageType.Laser,
                _ => DamageType.Normal
            };
        }

        // ==================== DISPLAY ====================

        public string GetStatusString()
        {
            if (!isPowered)
                return "Offline";

            if (IsCharged)
                return HasTarget ? "Ready" : "No Target";

            return $"Charging {Mathf.RoundToInt(chargeProgress * 100)}%";
        }
    }

    public enum WeaponType
    {
        Laser,
        Missile,
        Beam,
        Ion,
        Bomb,
        Flak
    }

    public enum StatusEffect
    {
        None,
        Fire,
        Breach,
        Stun,
        Lockdown
    }
}
