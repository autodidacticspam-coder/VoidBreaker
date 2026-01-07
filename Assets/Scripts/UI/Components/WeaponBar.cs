using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Combat;

namespace VoidBreaker.UI
{
    /// <summary>
    /// UI component showing equipped weapons.
    /// </summary>
    public class WeaponBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform weaponContainer;
        [SerializeField] private GameObject weaponSlotPrefab;

        private List<Weapon> weapons = new List<Weapon>();

        public void SetWeapons(IReadOnlyList<Weapon> weaponList)
        {
            weapons = new List<Weapon>(weaponList);
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            // Update weapon bar visuals
        }

        public void SelectWeapon(int index)
        {
            if (index >= 0 && index < weapons.Count)
            {
                Debug.Log($"[WeaponBar] Selected weapon {index}");
            }
        }

        public void ToggleWeaponPower(int index)
        {
            if (index >= 0 && index < weapons.Count)
            {
                weapons[index].TogglePower();
                UpdateDisplay();
            }
        }
    }
}
