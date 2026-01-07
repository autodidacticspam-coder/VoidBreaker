using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Sector
{
    /// <summary>
    /// Generates sector layouts with beacons and encounters.
    /// </summary>
    public class SectorGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private int minBeacons = 8;
        [SerializeField] private int maxBeacons = 12;
        [SerializeField] private int pathCount = 3;

        [Header("Beacon Weights")]
        [SerializeField] private float emptyWeight = 0.2f;
        [SerializeField] private float combatWeight = 0.3f;
        [SerializeField] private float eventWeight = 0.25f;
        [SerializeField] private float shopWeight = 0.1f;
        [SerializeField] private float eliteWeight = 0.1f;
        [SerializeField] private float bossWeight = 0.05f;

        /// <summary>
        /// Generates a new sector layout.
        /// </summary>
        public SectorData GenerateSector(int sectorNumber, SectorType sectorType)
        {
            var data = new SectorData
            {
                sectorNumber = sectorNumber,
                sectorType = sectorType,
                beacons = new List<BeaconData>()
            };

            int beaconCount = Random.Range(minBeacons, maxBeacons + 1);

            // Generate beacons in layers
            int layers = 4;
            int beaconsPerLayer = beaconCount / layers;

            for (int layer = 0; layer < layers; layer++)
            {
                int beaconsInLayer = (layer == layers - 1)
                    ? beaconCount - (beaconsPerLayer * (layers - 1))
                    : beaconsPerLayer;

                for (int i = 0; i < beaconsInLayer; i++)
                {
                    var beacon = GenerateBeacon(layer, i, beaconsInLayer, sectorNumber);
                    data.beacons.Add(beacon);
                }
            }

            // Add exit beacon
            data.beacons.Add(new BeaconData
            {
                id = $"exit_{sectorNumber}",
                type = BeaconType.Exit,
                layer = layers,
                position = new Vector2(layers * 2f, 0)
            });

            // Generate connections between layers
            GenerateConnections(data.beacons, layers);

            return data;
        }

        private BeaconData GenerateBeacon(int layer, int index, int count, int sectorNumber)
        {
            float ySpread = 2f;
            float yOffset = (index - (count - 1) / 2f) * ySpread;

            var beacon = new BeaconData
            {
                id = $"beacon_{layer}_{index}",
                layer = layer,
                position = new Vector2(layer * 2f, yOffset),
                type = RollBeaconType(layer, sectorNumber)
            };

            return beacon;
        }

        private BeaconType RollBeaconType(int layer, int sectorNumber)
        {
            // First beacon is always empty or has a simple event
            if (layer == 0)
                return Random.value < 0.5f ? BeaconType.Empty : BeaconType.Event;

            float roll = Random.value;
            float cumulative = 0;

            cumulative += emptyWeight;
            if (roll < cumulative) return BeaconType.Empty;

            cumulative += combatWeight;
            if (roll < cumulative) return BeaconType.Combat;

            cumulative += eventWeight;
            if (roll < cumulative) return BeaconType.Event;

            cumulative += shopWeight;
            if (roll < cumulative) return BeaconType.Shop;

            cumulative += eliteWeight;
            if (roll < cumulative) return BeaconType.Elite;

            return BeaconType.Combat;
        }

        private void GenerateConnections(List<BeaconData> beacons, int layers)
        {
            for (int layer = 0; layer < layers; layer++)
            {
                var currentLayer = beacons.FindAll(b => b.layer == layer);
                var nextLayer = beacons.FindAll(b => b.layer == layer + 1);

                if (nextLayer.Count == 0) continue;

                foreach (var beacon in currentLayer)
                {
                    // Connect to 1-2 beacons in the next layer
                    int connections = Random.Range(1, 3);
                    var candidates = new List<BeaconData>(nextLayer);

                    for (int i = 0; i < connections && candidates.Count > 0; i++)
                    {
                        // Prefer nearby beacons
                        candidates.Sort((a, b) =>
                            Vector2.Distance(beacon.position, a.position)
                            .CompareTo(Vector2.Distance(beacon.position, b.position)));

                        beacon.connections.Add(candidates[0].id);
                        candidates.RemoveAt(0);
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class SectorData
    {
        public int sectorNumber;
        public SectorType sectorType;
        public List<BeaconData> beacons;
    }

    [System.Serializable]
    public class BeaconData
    {
        public string id;
        public BeaconType type;
        public int layer;
        public Vector2 position;
        public List<string> connections = new List<string>();
    }

    public enum SectorType
    {
        Civilian,
        Military,
        Pirate,
        Rebel,
        Federation,
        Nebula,
        Asteroid,
        Final
    }
}
