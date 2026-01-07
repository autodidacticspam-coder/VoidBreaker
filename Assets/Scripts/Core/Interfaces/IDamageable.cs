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
}
