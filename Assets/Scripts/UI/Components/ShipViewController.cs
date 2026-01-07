using UnityEngine;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Controls the visual representation of the player's ship.
    /// </summary>
    public class ShipViewController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform shipContainer;
        [SerializeField] private Camera shipCamera;

        public void Initialize()
        {
            Debug.Log("[ShipViewController] Initialized");
        }

        public void UpdateView()
        {
            // Update ship visuals
        }

        public void SetZoom(float zoom)
        {
            if (shipCamera != null)
            {
                shipCamera.orthographicSize = zoom;
            }
        }
    }
}
