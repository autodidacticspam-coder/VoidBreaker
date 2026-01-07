using UnityEngine;
using VoidBreaker.Ship;

namespace VoidBreaker.UI
{
    /// <summary>
    /// UI component showing power allocation for ship systems.
    /// </summary>
    public class SystemPowerBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform powerBlockContainer;

        private ShipSystem targetSystem;

        public void SetSystem(ShipSystem system)
        {
            targetSystem = system;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            // Update power bar visuals
        }

        public void AddPower()
        {
            if (targetSystem != null)
            {
                targetSystem.SetPowerLevel(targetSystem.PowerAllocated + 1);
                UpdateDisplay();
            }
        }

        public void RemovePower()
        {
            if (targetSystem != null)
            {
                targetSystem.SetPowerLevel(targetSystem.PowerAllocated - 1);
                UpdateDisplay();
            }
        }
    }
}
