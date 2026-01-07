using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Sector
{
    /// <summary>
    /// Manages sector generation, beacon navigation, and pursuit mechanics.
    /// </summary>
    public class SectorManager : MonoBehaviour
    {
        [Header("Current Sector")]
        [SerializeField] private int currentSectorNumber;
        [SerializeField] private SectorType currentSectorType;
        [SerializeField] private List<Beacon> beacons = new List<Beacon>();
        [SerializeField] private Beacon currentBeacon;
        [SerializeField] private Beacon exitBeacon;

        [Header("Pursuit")]
        [SerializeField] private int pursuitLevel = 0;
        [SerializeField] private HashSet<int> pursuedBeacons = new HashSet<int>();
        [SerializeField] private int turnsUntilCatch = 3;

        [Header("Settings")]
        [SerializeField] private int minBeacons = 15;
        [SerializeField] private int maxBeacons = 25;

        // Public accessors
        public int CurrentSectorNumber => currentSectorNumber;
        public SectorType CurrentSectorType => currentSectorType;
        public IReadOnlyList<Beacon> Beacons => beacons;
        public Beacon CurrentBeacon => currentBeacon;
        public Beacon ExitBeacon => exitBeacon;
        public int PursuitLevel => pursuitLevel;
        public bool IsPursued => pursuedBeacons.Contains(currentBeacon?.Index ?? -1);

        /// <summary>
        /// Current sector data (convenience wrapper).
        /// </summary>
        public SectorData CurrentSector => new SectorData
        {
            sectorNumber = currentSectorNumber,
            type = currentSectorType,
            beaconCount = beacons.Count
        };

        /// <summary>
        /// Gets a beacon by its string ID.
        /// </summary>
        public Beacon GetBeacon(string beaconId)
        {
            if (int.TryParse(beaconId, out int index))
            {
                return beacons.Find(b => b.Index == index);
            }
            return beacons.Find(b => b.id == beaconId);
        }

        /// <summary>
        /// Sets the current beacon (for loading/teleportation).
        /// </summary>
        public void SetCurrentBeacon(Beacon beacon)
        {
            if (beacon != null && beacons.Contains(beacon))
            {
                currentBeacon = beacon;
                beacon.SetVisited();
            }
        }

        // Events
        public event Action<Beacon> OnBeaconSelected;
        public event Action<int> OnPursuitAdvanced;

        // ==================== SECTOR GENERATION ====================

        /// <summary>
        /// Generates a new sector with procedural beacon placement.
        /// </summary>
        public void GenerateSector(int sectorNumber)
        {
            currentSectorNumber = sectorNumber;
            currentSectorType = DetermineSectorType(sectorNumber);

            // Clear previous sector
            foreach (var beacon in beacons)
            {
                Destroy(beacon.gameObject);
            }
            beacons.Clear();
            pursuedBeacons.Clear();
            pursuitLevel = 0;

            // Determine beacon count
            int beaconCount = UnityEngine.Random.Range(minBeacons, maxBeacons + 1);

            // Generate beacon positions (column-based layout like FTL)
            int columns = 6;
            int beaconsPerColumn = Mathf.CeilToInt((float)beaconCount / columns);

            int beaconIndex = 0;
            for (int col = 0; col < columns && beaconIndex < beaconCount; col++)
            {
                int beaconsInThisColumn = Mathf.Min(beaconsPerColumn, beaconCount - beaconIndex);

                // Add some variance
                if (col > 0 && col < columns - 1)
                {
                    beaconsInThisColumn = UnityEngine.Random.Range(2, beaconsPerColumn + 1);
                }

                for (int row = 0; row < beaconsInThisColumn && beaconIndex < beaconCount; row++)
                {
                    var beacon = CreateBeacon(beaconIndex, col, row, beaconsInThisColumn);
                    beacons.Add(beacon);
                    beaconIndex++;
                }
            }

            // Set starting beacon (first column)
            currentBeacon = beacons[0];
            currentBeacon.SetVisited();

            // Set exit beacon (last column)
            exitBeacon = beacons[beacons.Count - 1];
            exitBeacon.SetAsExit();

            // Generate connections between beacons
            GenerateConnections();

            // Assign beacon types
            AssignBeaconTypes();

            Debug.Log($"[SectorManager] Generated sector {sectorNumber} ({currentSectorType}) with {beacons.Count} beacons");
        }

        private Beacon CreateBeacon(int index, int column, int row, int rowsInColumn)
        {
            var beaconObj = new GameObject($"Beacon_{index}");
            beaconObj.transform.SetParent(transform);

            // Position based on column and row
            float x = column * 3f;
            float yOffset = (rowsInColumn - 1) * 0.5f;
            float y = row * 2f - yOffset + UnityEngine.Random.Range(-0.5f, 0.5f);

            beaconObj.transform.localPosition = new Vector3(x, y, 0);

            var beacon = beaconObj.AddComponent<Beacon>();
            beacon.Initialize(index, column);

            return beacon;
        }

        private void GenerateConnections()
        {
            // Connect beacons in adjacent columns
            for (int i = 0; i < beacons.Count; i++)
            {
                var beacon = beacons[i];

                // Find beacons in the next column
                foreach (var other in beacons)
                {
                    if (other.Column == beacon.Column + 1)
                    {
                        // Check if close enough to connect
                        float distance = Vector2.Distance(beacon.Position, other.Position);
                        if (distance < 4f)
                        {
                            beacon.AddConnection(other);
                        }
                    }
                }

                // Ensure at least one forward connection
                if (beacon.Connections.Count == 0 && beacon.Column < 5)
                {
                    var closest = FindClosestInColumn(beacon, beacon.Column + 1);
                    if (closest != null)
                    {
                        beacon.AddConnection(closest);
                    }
                }
            }
        }

        private Beacon FindClosestInColumn(Beacon from, int targetColumn)
        {
            Beacon closest = null;
            float closestDist = float.MaxValue;

            foreach (var beacon in beacons)
            {
                if (beacon.Column == targetColumn)
                {
                    float dist = Vector2.Distance(from.Position, beacon.Position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = beacon;
                    }
                }
            }

            return closest;
        }

        private void AssignBeaconTypes()
        {
            // Distribution based on sector type
            float shopChance = currentSectorType == SectorType.Civilian ? 0.15f : 0.1f;
            float combatChance = currentSectorType == SectorType.Hostile ? 0.5f : 0.35f;
            float eventChance = 0.25f;
            float distressChance = 0.1f;
            float emptyChance = 0.1f;

            foreach (var beacon in beacons)
            {
                if (beacon == currentBeacon || beacon == exitBeacon)
                    continue;

                float roll = UnityEngine.Random.value;

                if (roll < shopChance)
                {
                    beacon.SetType(BeaconType.Shop);
                }
                else if (roll < shopChance + combatChance)
                {
                    beacon.SetType(BeaconType.Combat);
                }
                else if (roll < shopChance + combatChance + eventChance)
                {
                    beacon.SetType(BeaconType.Event);
                }
                else if (roll < shopChance + combatChance + eventChance + distressChance)
                {
                    beacon.SetType(BeaconType.Distress);
                }
                else
                {
                    beacon.SetType(BeaconType.Empty);
                }
            }

            // Guarantee at least one shop in later sectors
            if (currentSectorNumber >= 2)
            {
                bool hasShop = false;
                foreach (var beacon in beacons)
                {
                    if (beacon.Type == BeaconType.Shop)
                    {
                        hasShop = true;
                        break;
                    }
                }

                if (!hasShop)
                {
                    // Pick a random non-special beacon
                    var candidates = beacons.FindAll(b =>
                        b != currentBeacon && b != exitBeacon && b.Type != BeaconType.Exit);
                    if (candidates.Count > 0)
                    {
                        candidates[UnityEngine.Random.Range(0, candidates.Count)].SetType(BeaconType.Shop);
                    }
                }
            }
        }

        private SectorType DetermineSectorType(int sectorNumber)
        {
            // Sector type distribution
            return sectorNumber switch
            {
                1 => SectorType.Civilian,
                2 => UnityEngine.Random.value < 0.5f ? SectorType.Civilian : SectorType.Hostile,
                3 => SectorType.Hostile,
                4 => UnityEngine.Random.value < 0.3f ? SectorType.Nebula : SectorType.Hostile,
                5 => SectorType.Hostile,
                6 => UnityEngine.Random.value < 0.4f ? SectorType.Abandoned : SectorType.Hostile,
                7 => SectorType.Hegemony,
                8 => SectorType.TheBreach,
                _ => SectorType.Hostile
            };
        }

        // ==================== NAVIGATION ====================

        /// <summary>
        /// Attempts to jump to a beacon.
        /// </summary>
        public bool JumpToBeacon(Beacon target)
        {
            if (!CanJumpTo(target))
            {
                Debug.Log("[SectorManager] Cannot jump to this beacon");
                return false;
            }

            // Check fuel
            var runManager = GameManager.Instance?.CurrentRun;
            if (runManager == null || !runManager.SpendFuel(1))
            {
                Debug.Log("[SectorManager] Not enough fuel!");
                return false;
            }

            // Advance pursuit
            AdvancePursuit();

            // Trigger jump
            GameEvents.TriggerFTLJumpStarted(new FTLJumpStartedArgs
            {
                fromBeacon = currentBeacon,
                toBeacon = target,
                jumpDuration = 1f
            });

            currentBeacon = target;
            target.SetVisited();

            GameEvents.TriggerJumpCompleted(new JumpCompletedArgs
            {
                arrivedAt = target,
                fuelUsed = 1
            });

            GameEvents.TriggerBeaconReached(new BeaconReachedArgs
            {
                beacon = target,
                type = target.Type
            });

            // Trigger beacon encounter
            TriggerBeaconEncounter(target);

            return true;
        }

        public bool CanJumpTo(Beacon target)
        {
            if (target == null || target == currentBeacon)
                return false;

            // Must be connected or have special ability
            return currentBeacon.IsConnectedTo(target);
        }

        /// <summary>
        /// Gets all beacons reachable from current position.
        /// </summary>
        public List<Beacon> GetReachableBeacons()
        {
            return new List<Beacon>(currentBeacon.Connections);
        }

        private void TriggerBeaconEncounter(Beacon beacon)
        {
            switch (beacon.Type)
            {
                case BeaconType.Combat:
                    // Start combat
                    GameManager.Instance?.SetState(GameState.Combat);
                    break;

                case BeaconType.Shop:
                    GameManager.Instance?.SetState(GameState.Shop);
                    break;

                case BeaconType.Event:
                case BeaconType.Distress:
                    GameManager.Instance?.SetState(GameState.Event);
                    break;

                case BeaconType.Exit:
                    // Advance to next sector
                    GameManager.Instance?.CurrentRun?.AdvanceToNextSector();
                    break;

                case BeaconType.Empty:
                    // Nothing happens - stay on map
                    break;

                case BeaconType.Danger:
                    // Hard combat
                    GameManager.Instance?.SetState(GameState.Combat);
                    break;
            }

            // Check if pursued
            if (IsPursued)
            {
                // ASB (Anti-Ship Battery) attacks during encounters
                Debug.Log("[SectorManager] Under fire from pursuit fleet!");
            }
        }

        // ==================== PURSUIT ====================

        private void AdvancePursuit()
        {
            pursuitLevel++;

            // Mark beacons as pursued (fleet catches up)
            int beaconsToMark = pursuitLevel / 2;
            foreach (var beacon in beacons)
            {
                if (beacon.Column < beaconsToMark)
                {
                    pursuedBeacons.Add(beacon.Index);
                }
            }

            OnPursuitAdvanced?.Invoke(pursuitLevel);

            GameEvents.TriggerPursuitAdvanced(new PursuitAdvancedArgs
            {
                pursuitLevel = pursuitLevel,
                beaconsUntilCaught = GetBeaconsUntilPursuit()
            });

            Debug.Log($"[SectorManager] Pursuit advanced to level {pursuitLevel}");
        }

        public int GetBeaconsUntilPursuit()
        {
            // How many jumps until current beacon is caught
            int currentColumn = currentBeacon?.Column ?? 0;
            int pursuedColumn = pursuitLevel / 2;
            return Mathf.Max(0, currentColumn - pursuedColumn);
        }

        public bool IsBeaconPursued(Beacon beacon)
        {
            return pursuedBeacons.Contains(beacon.Index);
        }

        // ==================== SAVE/LOAD ====================

        public SectorSaveData GetSaveData()
        {
            var data = new SectorSaveData
            {
                sectorNumber = currentSectorNumber,
                sectorType = currentSectorType,
                currentBeaconIndex = currentBeacon?.Index ?? 0,
                pursuitLevel = pursuitLevel,
                beaconData = new List<BeaconSaveData>()
            };

            foreach (var beacon in beacons)
            {
                data.beaconData.Add(beacon.GetSaveData());
            }

            return data;
        }

        public void LoadFromSave(SectorSaveData data)
        {
            currentSectorNumber = data.sectorNumber;
            currentSectorType = data.sectorType;
            pursuitLevel = data.pursuitLevel;

            // Rebuild beacons from save
            // (In full implementation, would recreate beacon layout)

            Debug.Log($"[SectorManager] Loaded sector {currentSectorNumber}");
        }
    }

    public enum SectorType
    {
        Civilian,
        Hostile,
        Nebula,
        Abandoned,
        Hegemony,
        TheBreach
    }

    [Serializable]
    public class SectorSaveData
    {
        public int sectorNumber;
        public SectorType sectorType;
        public int currentBeaconIndex;
        public int pursuitLevel;
        public List<BeaconSaveData> beaconData;
    }

    /// <summary>
    /// Simple data structure representing current sector state.
    /// </summary>
    public struct SectorData
    {
        public int sectorNumber;
        public SectorType type;
        public int beaconCount;
    }
}
