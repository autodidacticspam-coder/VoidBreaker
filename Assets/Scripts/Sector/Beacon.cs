using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Data;

namespace VoidBreaker.Sector
{
    /// <summary>
    /// Represents a single beacon/node on the sector map.
    /// </summary>
    public class Beacon : MonoBehaviour, ITargetable
    {
        [Header("Identity")]
        [SerializeField] private int index;
        [SerializeField] private int column;
        [SerializeField] private BeaconType type = BeaconType.Unknown;

        [Header("State")]
        [SerializeField] private bool isVisited = false;
        [SerializeField] private bool isRevealed = false;

        [Header("Connections")]
        [SerializeField] private List<Beacon> connections = new List<Beacon>();

        [Header("Encounter Data")]
        [SerializeField] private string eventId;
        [SerializeField] private int enemyDifficulty;
        [SerializeField] private bool hasEnemy;
        [SerializeField] private EnemyDefinition enemyDefinition;

        // ITargetable implementation
        public Vector2 Position => transform.position;
        public TargetType TargetType => TargetType.Ship; // Not really used for beacons
        public bool CanBeTargeted => false;
        public string TargetName => $"Beacon {index}";

        // Public accessors
        public int Index => index;
        public int Column => column;
        public BeaconType Type => type;
        public bool IsVisited => isVisited;
        public bool IsRevealed => isRevealed;
        public IReadOnlyList<Beacon> Connections => connections;

        // Property aliases for compatibility
        public bool Visited => isVisited;
        public bool Revealed => isRevealed;
        public BeaconType BeaconType => type;

        /// <summary>
        /// String ID for this beacon (combination of index and column).
        /// </summary>
        public string id => $"{column}_{index}";

        /// <summary>
        /// Whether this beacon has an enemy encounter.
        /// </summary>
        public bool HasEnemy => hasEnemy;

        /// <summary>
        /// The enemy definition for this beacon (if hasEnemy is true).
        /// </summary>
        public EnemyDefinition EnemyDefinition => enemyDefinition;

        /// <summary>
        /// The event ID for this beacon (if any).
        /// </summary>
        public string EventId => eventId;

        // Events
        public event Action<Beacon> OnVisited;
        public event Action<Beacon> OnTypeRevealed;

        public void Initialize(int beaconIndex, int beaconColumn)
        {
            index = beaconIndex;
            column = beaconColumn;
            type = BeaconType.Unknown;
            isVisited = false;
            isRevealed = false;
        }

        // ==================== CONNECTIONS ====================

        public void AddConnection(Beacon other)
        {
            if (other == null || other == this) return;

            if (!connections.Contains(other))
            {
                connections.Add(other);
            }

            // Bidirectional connection
            if (!other.connections.Contains(this))
            {
                other.connections.Add(this);
            }
        }

        public bool IsConnectedTo(Beacon other)
        {
            return connections.Contains(other);
        }

        public void RemoveConnection(Beacon other)
        {
            connections.Remove(other);
            other?.connections.Remove(this);
        }

        // ==================== TYPE ====================

        public void SetType(BeaconType newType)
        {
            type = newType;

            // Some types are immediately revealed
            if (type == BeaconType.Exit || type == BeaconType.Shop)
            {
                isRevealed = true;
            }
        }

        public void Reveal()
        {
            if (isRevealed) return;

            isRevealed = true;
            OnTypeRevealed?.Invoke(this);
        }

        public void SetAsExit()
        {
            type = BeaconType.Exit;
            isRevealed = true;
        }

        // ==================== VISITATION ====================

        public void SetVisited()
        {
            if (isVisited) return;

            isVisited = true;
            isRevealed = true;
            OnVisited?.Invoke(this);

            // Reveal connected beacons
            foreach (var connected in connections)
            {
                connected.Reveal();
            }
        }

        // ==================== ENCOUNTER DATA ====================

        public void SetEventId(string id)
        {
            eventId = id;
        }

        public string GetEventId()
        {
            return eventId;
        }

        public void SetEnemyDifficulty(int difficulty)
        {
            enemyDifficulty = difficulty;
        }

        public int GetEnemyDifficulty()
        {
            return enemyDifficulty;
        }

        // ==================== DISPLAY ====================

        public string GetTypeIcon()
        {
            if (!isRevealed && type != BeaconType.Exit)
                return "?";

            return type switch
            {
                BeaconType.Unknown => "?",
                BeaconType.Combat => "X",
                BeaconType.Shop => "$",
                BeaconType.Event => "!",
                BeaconType.Distress => "SOS",
                BeaconType.Empty => "o",
                BeaconType.Exit => ">",
                BeaconType.Quest => "*",
                BeaconType.Danger => "!!",
                _ => "?"
            };
        }

        public Color GetTypeColor()
        {
            if (!isRevealed && type != BeaconType.Exit)
                return Color.gray;

            return type switch
            {
                BeaconType.Unknown => Color.gray,
                BeaconType.Combat => Color.red,
                BeaconType.Shop => Color.green,
                BeaconType.Event => Color.yellow,
                BeaconType.Distress => Color.cyan,
                BeaconType.Empty => Color.white,
                BeaconType.Exit => Color.magenta,
                BeaconType.Quest => new Color(1f, 0.5f, 0f), // Orange
                BeaconType.Danger => new Color(0.8f, 0f, 0f), // Dark red
                _ => Color.gray
            };
        }

        public string GetTooltip()
        {
            if (!isRevealed && type != BeaconType.Exit)
                return "Unknown - Scout to reveal";

            return type switch
            {
                BeaconType.Unknown => "Unknown beacon",
                BeaconType.Combat => "Hostile ship detected",
                BeaconType.Shop => "Trading post",
                BeaconType.Event => "Signal detected",
                BeaconType.Distress => "Distress signal",
                BeaconType.Empty => "Empty space",
                BeaconType.Exit => "Exit to next sector",
                BeaconType.Quest => "Quest objective",
                BeaconType.Danger => "Dangerous area - proceed with caution",
                _ => "Unknown"
            };
        }

        // ==================== SAVE/LOAD ====================

        public BeaconSaveData GetSaveData()
        {
            return new BeaconSaveData
            {
                index = index,
                column = column,
                type = type,
                isVisited = isVisited,
                isRevealed = isRevealed,
                eventId = eventId,
                enemyDifficulty = enemyDifficulty,
                connectionIndices = GetConnectionIndices()
            };
        }

        private int[] GetConnectionIndices()
        {
            var indices = new int[connections.Count];
            for (int i = 0; i < connections.Count; i++)
            {
                indices[i] = connections[i].Index;
            }
            return indices;
        }

        public void LoadFromSave(BeaconSaveData data)
        {
            index = data.index;
            column = data.column;
            type = data.type;
            isVisited = data.isVisited;
            isRevealed = data.isRevealed;
            eventId = data.eventId;
            enemyDifficulty = data.enemyDifficulty;
            // Connections restored by SectorManager
        }
    }

    public enum BeaconType
    {
        Unknown,
        Combat,
        Shop,
        Event,
        Distress,
        Empty,
        Exit,
        Quest,
        Danger,
        Elite,
        Store
    }

    [Serializable]
    public class BeaconSaveData
    {
        public int index;
        public int column;
        public BeaconType type;
        public bool isVisited;
        public bool isRevealed;
        public string eventId;
        public int enemyDifficulty;
        public int[] connectionIndices;
    }
}
