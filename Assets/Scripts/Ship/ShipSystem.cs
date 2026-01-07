using System;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Base class for all ship systems (Engines, Shields, Weapons, etc.)
    /// </summary>
    public class ShipSystem : MonoBehaviour, ISystem
    {
        [Header("System Info")]
        [SerializeField] protected string systemName = "System";
        [SerializeField] protected SystemType systemType;

        [Header("Levels")]
        [SerializeField] protected int maxLevel = 8;
        [SerializeField] protected int currentLevel = 1;

        [Header("Power")]
        [SerializeField] protected bool requiresPower = true;
        [SerializeField] protected int powerAllocated = 0;
        [SerializeField] protected int maxPower = 4;
        [SerializeField] protected int requiredPowerToOperate = 1;

        [Header("Ship Reference")]
        [SerializeField] protected ShipController parentShip;

        [Header("Damage")]
        [SerializeField] protected int maxHealth = 3;
        [SerializeField] protected int currentHealth = 3;
        [SerializeField] protected int ionDamage = 0;
        [SerializeField] protected float ionRecoveryTimer = 0f;

        [Header("Manning")]
        [SerializeField] protected bool isManned = false;
        [SerializeField] protected ICrewMember manningCrew;

        // ISystem implementation
        public string SystemName => systemName;
        public SystemType SystemType => systemType;
        public int MaxLevel => maxLevel;
        public int CurrentLevel => currentLevel;
        public int MaxPower => currentLevel;
        public int PowerAllocated
        {
            get => powerAllocated;
            set => SetPower(value);
        }
        public int DamageLevel => maxHealth - currentHealth;
        public bool IsOperational => currentHealth > 0 && (powerAllocated > 0 || !requiresPower);
        public bool IsManned => isManned && manningCrew != null;
        public bool RequiresPower => requiresPower;
        public ICrewMember ManningCrew => manningCrew;
        public ShipController ParentShip => parentShip;

        // Protected aliases for child classes
        protected int AllocatedPower => powerAllocated;
        protected int RequiredPowerToOperate => requiredPowerToOperate;
        protected int damageLevel => DamageLevel;

        // Effective power (reduced by damage and ion)
        public int EffectivePower => Mathf.Max(0, powerAllocated - DamageLevel - ionDamage);

        // Events
        public event Action<ISystem> OnSystemDamaged;
        public event Action<ISystem> OnSystemRepaired;
        public event Action<ISystem> OnSystemDestroyed;
        public event Action<ISystem> OnSystemPowerChanged;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        public virtual void Initialize(int startingLevel)
        {
            currentLevel = Mathf.Clamp(startingLevel, 1, maxLevel);
            currentHealth = maxHealth;
            powerAllocated = 0;
            ionDamage = 0;

            Debug.Log($"[{systemName}] Initialized at level {currentLevel}");
        }

        public virtual void Initialize(ShipController ship, int startingLevel)
        {
            Initialize(startingLevel);
        }

        public virtual void Upgrade()
        {
            if (currentLevel >= maxLevel)
            {
                Debug.Log($"[{systemName}] Already at max level");
                return;
            }

            currentLevel++;
            Debug.Log($"[{systemName}] Upgraded to level {currentLevel}");
        }

        // ==================== POWER ====================

        public virtual void SetPowerLevel(int newPower)
        {
            SetPower(newPower);
        }

        // ISystem interface implementation
        public virtual void OnPowerChanged(int newPower)
        {
            SetPower(newPower);
        }

        protected virtual void SetPower(int power)
        {
            int maxAllowable = Mathf.Min(currentLevel, maxHealth - (maxHealth - currentHealth));
            powerAllocated = Mathf.Clamp(power, 0, maxAllowable);

            OnSystemPowerChanged?.Invoke(this);
        }

        // ==================== DAMAGE ====================

        public virtual void TakeDamage(int amount)
        {
            if (currentHealth <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);

            OnSystemDamaged?.Invoke(this);

            GameEvents.TriggerSystemDamaged(new SystemDamagedArgs
            {
                system = this,
                damageAmount = amount,
                remainingHealth = currentHealth
            });

            if (currentHealth <= 0)
            {
                OnSystemDestroyed?.Invoke(this);
                GameEvents.TriggerSystemDestroyed(new SystemDestroyedArgs
                {
                    system = this,
                    causedHullDamage = true
                });
            }

            Debug.Log($"[{systemName}] Damaged! Health: {currentHealth}/{maxHealth}");
        }

        public virtual void TakeIonDamage(int amount)
        {
            ionDamage = Mathf.Min(ionDamage + amount, currentLevel);
            ionRecoveryTimer = 5f; // 5 seconds per ion level

            Debug.Log($"[{systemName}] Ion damage: {ionDamage}");
        }

        public virtual void Repair(int amount)
        {
            if (currentHealth >= maxHealth) return;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            OnSystemRepaired?.Invoke(this);

            GameEvents.TriggerSystemRepaired(new SystemRepairedArgs
            {
                system = this,
                repairAmount = currentHealth - oldHealth,
                currentHealth = currentHealth
            });

            Debug.Log($"[{systemName}] Repaired! Health: {currentHealth}/{maxHealth}");
        }

        // ==================== CREW MANNING ====================

        public virtual void OnCrewManned(ICrewMember crew)
        {
            manningCrew = crew;
            isManned = true;

            Debug.Log($"[{systemName}] Now manned by {crew.CrewName}");
        }

        public virtual void OnCrewLeft()
        {
            manningCrew = null;
            isManned = false;

            Debug.Log($"[{systemName}] No longer manned");
        }

        public virtual void OnUnmanned()
        {
            OnCrewLeft();
        }

        public virtual void OnDamaged(int amount)
        {
            TakeDamage(amount);
        }

        // ==================== HACKING ====================

        public virtual void ApplyHack(float duration)
        {
            // Override in specific systems that can be hacked
            Debug.Log($"[{systemName}] Hacked for {duration}s");
        }

        public virtual void RemoveHack()
        {
            // Override in specific systems
            Debug.Log($"[{systemName}] Hack removed");
        }

        // ==================== UPDATE ====================

        public virtual void Tick(float deltaTime)
        {
            // Recover from ion damage
            if (ionDamage > 0)
            {
                ionRecoveryTimer -= deltaTime;
                if (ionRecoveryTimer <= 0)
                {
                    ionDamage--;
                    ionRecoveryTimer = 5f;
                }
            }
        }

        // ==================== MANNING BONUS ====================

        /// <summary>
        /// Returns the manning bonus multiplier (1.0 = no bonus)
        /// </summary>
        public virtual float GetManningBonus()
        {
            if (!isManned || manningCrew == null)
                return 1f;

            // Each skill level adds 5% efficiency
            int skillLevel = GetRelevantSkillLevel();
            return 1f + (skillLevel * 0.05f);
        }

        protected virtual int GetRelevantSkillLevel()
        {
            if (manningCrew == null) return 0;

            return systemType switch
            {
                SystemType.Piloting => manningCrew.Skills.GetSkill(SkillType.Piloting),
                SystemType.Engines => manningCrew.Skills.GetSkill(SkillType.Engines),
                SystemType.Shields => manningCrew.Skills.GetSkill(SkillType.Shields),
                SystemType.Weapons => manningCrew.Skills.GetSkill(SkillType.Weapons),
                _ => 0
            };
        }

        // ==================== SAVE/LOAD ====================

        public virtual SystemSaveData GetSaveData()
        {
            return new SystemSaveData
            {
                systemType = systemType,
                currentLevel = currentLevel,
                currentHealth = currentHealth,
                powerAllocated = powerAllocated,
                ionDamage = ionDamage
            };
        }

        public virtual void LoadFromSave(SystemSaveData data)
        {
            currentLevel = data.currentLevel;
            currentHealth = data.currentHealth;
            powerAllocated = data.powerAllocated;
            ionDamage = data.ionDamage;
        }
    }

    [Serializable]
    public class SystemSaveData
    {
        public SystemType systemType;
        public int currentLevel;
        public int currentHealth;
        public int powerAllocated;
        public int ionDamage;
    }
}
