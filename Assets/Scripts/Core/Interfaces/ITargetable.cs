using UnityEngine;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Interface for anything that can be targeted by weapons
    /// </summary>
    public interface ITargetable
    {
        Vector2 Position { get; }
        TargetType TargetType { get; }
        bool CanBeTargeted { get; }
        string TargetName { get; }
    }

    public enum TargetType
    {
        Ship,
        Room,
        System,
        Crew,
        Projectile,
        Drone
    }
}
