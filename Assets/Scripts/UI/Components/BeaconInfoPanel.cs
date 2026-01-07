using UnityEngine;
using UnityEngine.UI;
using VoidBreaker.Sector;

namespace VoidBreaker.UI
{
    /// <summary>
    /// UI panel showing information about a selected beacon.
    /// </summary>
    public class BeaconInfoPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text beaconNameText;
        [SerializeField] private Text beaconTypeText;
        [SerializeField] private Text distanceText;
        [SerializeField] private Button jumpButton;

        private Beacon currentBeacon;

        public void ShowBeacon(Beacon beacon)
        {
            currentBeacon = beacon;
            gameObject.SetActive(true);
            UpdateDisplay();
        }

        public void Hide()
        {
            currentBeacon = null;
            gameObject.SetActive(false);
        }

        private void UpdateDisplay()
        {
            if (currentBeacon == null) return;

            if (beaconNameText != null)
                beaconNameText.text = currentBeacon.TargetName;

            if (beaconTypeText != null)
                beaconTypeText.text = currentBeacon.GetTooltip();

            // Update jump button state
            if (jumpButton != null)
            {
                bool canJump = CanJumpToBeacon();
                jumpButton.interactable = canJump;
            }
        }

        private bool CanJumpToBeacon()
        {
            if (currentBeacon == null) return false;
            // Check if beacon is reachable and player has fuel
            return currentBeacon.IsRevealed || currentBeacon.Type == BeaconType.Exit;
        }

        public void OnJumpClicked()
        {
            if (currentBeacon == null) return;

            Debug.Log($"[BeaconInfoPanel] Jumping to {currentBeacon.TargetName}");
            // Trigger jump through GameEvents
        }
    }
}
