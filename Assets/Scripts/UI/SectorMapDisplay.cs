using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBreaker.Core;
using VoidBreaker.Sector;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Displays the sector map with beacons and connections.
    /// </summary>
    public class SectorMapDisplay : MonoBehaviour
    {
        [Header("Map Container")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private RectTransform beaconContainer;
        [SerializeField] private RectTransform connectionContainer;

        [Header("Prefabs")]
        [SerializeField] private BeaconMarker beaconMarkerPrefab;
        [SerializeField] private Image connectionLinePrefab;

        [Header("Pursuit Fleet")]
        [SerializeField] private RectTransform pursuitLine;
        [SerializeField] private Image pursuitFill;
        [SerializeField] private TextMeshProUGUI pursuitWarningText;

        [Header("Sector Info")]
        [SerializeField] private TextMeshProUGUI sectorNameText;
        [SerializeField] private TextMeshProUGUI sectorTypeText;
        [SerializeField] private TextMeshProUGUI beaconCountText;

        [Header("Legend")]
        [SerializeField] private Transform legendContainer;

        [Header("Colors")]
        [SerializeField] private Color unknownColor = Color.gray;
        [SerializeField] private Color combatColor = Color.red;
        [SerializeField] private Color shopColor = Color.green;
        [SerializeField] private Color eventColor = Color.blue;
        [SerializeField] private Color distressColor = Color.yellow;
        [SerializeField] private Color exitColor = Color.magenta;
        [SerializeField] private Color visitedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color currentColor = Color.cyan;
        [SerializeField] private Color dangerZoneColor = new Color(1f, 0f, 0f, 0.3f);

        private SectorManager sectorManager;
        private List<BeaconMarker> beaconMarkers = new List<BeaconMarker>();
        private List<Image> connectionLines = new List<Image>();
        private Beacon currentBeacon;

        public void Initialize(SectorManager manager)
        {
            sectorManager = manager;
            RefreshMap();
        }

        public void RefreshMap()
        {
            if (sectorManager == null) return;

            ClearMap();

            var sector = sectorManager.CurrentSectorData;
            if (sector == null) return;

            // Update sector info
            UpdateSectorInfo(sector);

            // Draw connections first (so they're behind beacons)
            DrawConnections(sector);

            // Create beacon markers
            CreateBeaconMarkers(sector);

            // Update pursuit display
            UpdatePursuitDisplay();
        }

        private void ClearMap()
        {
            foreach (var marker in beaconMarkers)
            {
                if (marker != null)
                    Destroy(marker.gameObject);
            }
            beaconMarkers.Clear();

            foreach (var line in connectionLines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            connectionLines.Clear();
        }

        private void UpdateSectorInfo(SectorData sector)
        {
            if (sectorNameText)
                sectorNameText.text = sector.SectorName;

            if (sectorTypeText)
                sectorTypeText.text = sector.SectorType.ToString();

            if (beaconCountText)
                beaconCountText.text = $"{sector.Beacons.Count} Beacons";
        }

        private void DrawConnections(SectorData sector)
        {
            if (connectionLinePrefab == null || connectionContainer == null)
                return;

            // Track drawn connections to avoid duplicates
            HashSet<string> drawnConnections = new HashSet<string>();

            foreach (var beacon in sector.Beacons)
            {
                foreach (var connection in beacon.Connections)
                {
                    // Create unique key for this connection
                    string key1 = $"{beacon.BeaconId}-{connection.BeaconId}";
                    string key2 = $"{connection.BeaconId}-{beacon.BeaconId}";

                    if (drawnConnections.Contains(key1) || drawnConnections.Contains(key2))
                        continue;

                    drawnConnections.Add(key1);

                    // Create line
                    var line = Instantiate(connectionLinePrefab, connectionContainer);
                    PositionLine(line, beacon.Position, connection.Position);

                    // Color based on whether path is accessible
                    bool accessible = CanTravelTo(connection);
                    line.color = accessible ?
                        new Color(0.5f, 0.5f, 0.5f, 0.8f) :
                        new Color(0.3f, 0.3f, 0.3f, 0.3f);

                    connectionLines.Add(line);
                }
            }
        }

        private void PositionLine(Image line, Vector2 start, Vector2 end)
        {
            // Convert positions to UI space
            Vector2 uiStart = WorldToMapPosition(start);
            Vector2 uiEnd = WorldToMapPosition(end);

            Vector2 direction = uiEnd - uiStart;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            RectTransform rt = line.rectTransform;
            rt.anchoredPosition = uiStart;
            rt.sizeDelta = new Vector2(distance, 2f);
            rt.rotation = Quaternion.Euler(0, 0, angle);
            rt.pivot = new Vector2(0, 0.5f);
        }

        private void CreateBeaconMarkers(SectorData sector)
        {
            if (beaconMarkerPrefab == null || beaconContainer == null)
                return;

            currentBeacon = sectorManager.CurrentBeacon;

            foreach (var beacon in sector.Beacons)
            {
                var marker = Instantiate(beaconMarkerPrefab, beaconContainer);
                marker.Initialize(beacon, this);

                // Position marker
                Vector2 uiPos = WorldToMapPosition(beacon.Position);
                marker.GetComponent<RectTransform>().anchoredPosition = uiPos;

                // Set color based on type and state
                Color markerColor = GetBeaconColor(beacon);
                marker.SetColor(markerColor);

                // Mark current beacon
                if (beacon == currentBeacon)
                {
                    marker.SetCurrent(true);
                }

                // Check if in danger zone
                if (IsInDangerZone(beacon))
                {
                    marker.SetInDanger(true);
                }

                beaconMarkers.Add(marker);
            }
        }

        private Color GetBeaconColor(Beacon beacon)
        {
            if (beacon.Visited)
                return visitedColor;

            if (beacon == currentBeacon)
                return currentColor;

            // If sensors reveal beacon type
            bool revealed = HasLongRangeScanners() || beacon.Revealed;

            if (!revealed)
                return unknownColor;

            return beacon.BeaconType switch
            {
                BeaconType.Combat => combatColor,
                BeaconType.Shop => shopColor,
                BeaconType.Event => eventColor,
                BeaconType.Distress => distressColor,
                BeaconType.Exit => exitColor,
                BeaconType.Danger => combatColor,
                BeaconType.Quest => Color.yellow,
                _ => unknownColor
            };
        }

        private Vector2 WorldToMapPosition(Vector2 worldPos)
        {
            // Convert world position to map UI position
            // This depends on your map scale
            float scale = 50f; // pixels per world unit
            return worldPos * scale;
        }

        private void UpdatePursuitDisplay()
        {
            if (sectorManager == null) return;

            int pursuitProgress = sectorManager.PursuitProgress;
            int maxPursuit = 10; // Columns before caught

            float pursuitPercent = (float)pursuitProgress / maxPursuit;

            if (pursuitFill)
            {
                pursuitFill.fillAmount = pursuitPercent;
                pursuitFill.color = Color.Lerp(Color.green, Color.red, pursuitPercent);
            }

            if (pursuitWarningText)
            {
                if (pursuitPercent > 0.8f)
                    pursuitWarningText.text = "FLEET CLOSING IN!";
                else if (pursuitPercent > 0.5f)
                    pursuitWarningText.text = "Fleet approaching...";
                else
                    pursuitWarningText.text = "";
            }
        }

        private bool IsInDangerZone(Beacon beacon)
        {
            if (sectorManager == null) return false;
            return beacon.Column <= sectorManager.PursuitProgress;
        }

        private bool CanTravelTo(Beacon beacon)
        {
            if (currentBeacon == null) return false;

            // Can only travel to connected beacons
            return currentBeacon.Connections.Contains(beacon);
        }

        private bool HasLongRangeScanners()
        {
            var run = GameManager.Instance?.CurrentRun;
            if (run == null) return false;

            return run.State.augments.Contains("long_range_scanners");
        }

        // ==================== BEACON INTERACTION ====================

        public void OnBeaconClicked(Beacon beacon)
        {
            if (!CanTravelTo(beacon))
            {
                Debug.Log("[SectorMap] Cannot travel to this beacon");
                return;
            }

            if (IsInDangerZone(beacon))
            {
                Debug.Log("[SectorMap] Warning: This beacon is in the danger zone!");
                // Could show confirmation dialog
            }

            // Travel to beacon
            sectorManager?.TravelToBeacon(beacon);
        }

        public void OnBeaconHovered(Beacon beacon)
        {
            // Show beacon info tooltip
        }

        public void OnBeaconUnhovered()
        {
            // Hide tooltip
        }
    }

    /// <summary>
    /// Individual beacon marker on the map.
    /// </summary>
    public class BeaconMarker : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image currentIndicator;
        [SerializeField] private Image dangerIndicator;
        [SerializeField] private TextMeshProUGUI labelText;

        private Beacon beacon;
        private SectorMapDisplay mapDisplay;

        public void Initialize(Beacon b, SectorMapDisplay display)
        {
            beacon = b;
            mapDisplay = display;

            if (button)
            {
                button.onClick.AddListener(OnClicked);
            }
        }

        public void SetColor(Color color)
        {
            if (iconImage) iconImage.color = color;
        }

        public void SetCurrent(bool isCurrent)
        {
            if (currentIndicator)
                currentIndicator.gameObject.SetActive(isCurrent);
        }

        public void SetInDanger(bool inDanger)
        {
            if (dangerIndicator)
                dangerIndicator.gameObject.SetActive(inDanger);
        }

        private void OnClicked()
        {
            mapDisplay?.OnBeaconClicked(beacon);
        }

        public void OnPointerEnter()
        {
            mapDisplay?.OnBeaconHovered(beacon);
        }

        public void OnPointerExit()
        {
            mapDisplay?.OnBeaconUnhovered();
        }
    }
}
