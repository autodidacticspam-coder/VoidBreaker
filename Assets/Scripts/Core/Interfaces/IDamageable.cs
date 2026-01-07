using UnityEngine;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Interface for anything that can take damage (ships, systems, crew)
    /// </summary>
    public interface IDamageable
    {
        int MaxHealth { get; }
        int CurrentHealth { get; }
        bool IsDestroyed { get; }

        void TakeDamage(DamageInfo damage);
        void Heal(int amount);

        event System.Action<IDamageable, DamageInfo> OnDamaged;
        event System.Action<IDamageable> OnDestroyed;
    }

    [System.Serializable]
    public struct DamageInfo
    {
        public int amount;
        public DamageType type;
        public bool ignoresShields;
        public bool causesBreach;
        public bool causesFire;
        public int ionDamage;
        public ITargetable source;

        public DamageInfo(int amount, DamageType type = DamageType.Normal)
        {
            this.amount = amount;
            this.type = type;
            this.ignoresShields = false;
            this.causesBreach = false;
            this.causesFire = false;
            this.ionDamage = 0;
            this.source = null;
        }

        public static DamageInfo Laser(int amount) => new DamageInfo(amount, DamageType.Laser);

        public static DamageInfo Missile(int amount) => new DamageInfo(amount, DamageType.Missile)
        {
            ignoresShields = true
        };

        public static DamageInfo Beam(int amount) => new DamageInfo(amount, DamageType.Beam);

        public static DamageInfo Ion(int ionAmount) => new DamageInfo(0, DamageType.Ion)
        {
            ionDamage = ionAmount
        };

        public static DamageInfo Fire() => new DamageInfo(1, DamageType.Fire)
        {
            causesFire = true
        };

        public static DamageInfo Breach() => new DamageInfo(0, DamageType.Breach)
        {
            causesBreach = true
        };
    }

    public enum DamageType
    {
        Normal,
        Laser,
        Missile,
        Beam,
        Ion,
        Fire,
        Breach,
        Suffocation,
        Combat // Crew-to-crew combat
    }
}
