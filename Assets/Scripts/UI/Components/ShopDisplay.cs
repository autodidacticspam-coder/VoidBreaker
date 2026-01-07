using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.UI
{
    /// <summary>
    /// UI component for the shop/store interface.
    /// </summary>
    public class ShopDisplay : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject weaponsPanel;
        [SerializeField] private GameObject augmentsPanel;
        [SerializeField] private GameObject crewPanel;
        [SerializeField] private GameObject repairsPanel;

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshShop();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void RefreshShop()
        {
            // Refresh shop items based on current sector and shop type
        }

        public void SelectTab(int tabIndex)
        {
            weaponsPanel?.SetActive(tabIndex == 0);
            augmentsPanel?.SetActive(tabIndex == 1);
            crewPanel?.SetActive(tabIndex == 2);
            repairsPanel?.SetActive(tabIndex == 3);
        }

        public void PurchaseItem(string itemId)
        {
            var runManager = GameManager.Instance?.CurrentRun;
            if (runManager == null) return;

            Debug.Log($"[ShopDisplay] Attempting to purchase: {itemId}");
            // Handle purchase logic
        }
    }
}
