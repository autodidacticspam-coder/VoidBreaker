using UnityEngine;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Interface for all ship systems (Engines, Shields, Weapons, etc.)
    /// </summary>
    public interface ISystem
    {
        string SystemName { get; }
        SystemType SystemType { get; }
        int MaxLevel { get; }
        int CurrentLevel { get; }
        int MaxPower { get; }
        int PowerAllocated { get; set; }
        int DamageLevel { get; }
        bool IsOperational { get; }
        bool IsManned { get; }
        bool RequiresPower { get; }

        void Initialize(int startingLevel);
        void Upgrade();
        void OnPowerChanged(int newPower);
        void TakeDamage(int amount);
        void Repair(int amount);
        void OnCrewManned(ICrewMember crew);
        void OnCrewLeft();
        void Tick(float deltaTime);

        event System.Action<ISystem> OnSystemDamaged;
        event System.Action<ISystem> OnSystemRepaired;
        event System.Action<ISystem> OnSystemDestroyed;
    }

    public enum SystemType
    {
        Reactor,
        Engines,
        Shields,
        Weapons,
        Piloting,
        Sensors,
        Doors,
        Medbay,
        Oxygen,
        Cloak,
        Teleporter,
        DroneControl,
        Hacking,
        MindControl,
        CommsArray,
        RegenerationBay
    }
}
