using System;
using UnityEngine;
using VoidBreaker.Ship;
using VoidBreaker.UI;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Handles all player input and dispatches to appropriate systems.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ShipController playerShip;
        [SerializeField] private UIManager uiManager;

        [Header("Settings")]
        [SerializeField] private bool invertScrollZoom = false;
        [SerializeField] private float scrollSensitivity = 1f;

        [Header("Input State")]
        [SerializeField] private InputMode currentMode = InputMode.Normal;
        [SerializeField] private bool isTargeting = false;
        [SerializeField] private int selectedWeaponIndex = -1;
        [SerializeField] private Room selectedRoom;
        [SerializeField] private Crew.CrewMember selectedCrew;

        // Events
        public event Action<Room> OnRoomClicked;
        public event Action<Room> OnRoomRightClicked;
        public event Action<Crew.CrewMember> OnCrewClicked;
        public event Action<int> OnWeaponSelected;
        public event Action<Room> OnTargetSelected;
        public event Action OnSelectionCleared;

        public InputMode CurrentMode => currentMode;
        public bool IsTargeting => isTargeting;
        public Room SelectedRoom => selectedRoom;

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleGlobalInput();

            switch (currentMode)
            {
                case InputMode.Normal:
                    HandleNormalInput();
                    break;
                case InputMode.Targeting:
                    HandleTargetingInput();
                    break;
                case InputMode.CrewCommand:
                    HandleCrewCommandInput();
                    break;
            }
        }

        // ==================== GLOBAL INPUT ====================

        private void HandleGlobalInput()
        {
            // Pause toggle
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameEvents.TriggerPauseToggle();
            }

            // Cancel/Clear selection
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isTargeting || selectedCrew != null || selectedRoom != null)
                {
                    ClearSelection();
                }
                else if (uiManager != null && uiManager.CurrentState != UIState.MainMenu)
                {
                    // Let UI handle escape
                }
            }

            // Quick weapon keys (1-4)
            for (int i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectWeapon(i);
                }
            }

            // Tab for evolution tree
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // Handled by UIManager
            }

            // M for map
            if (Input.GetKeyDown(KeyCode.M))
            {
                // Handled by UIManager
            }

            // Power redistribution hotkeys
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                HandlePowerInput();
            }
        }

        private void HandlePowerInput()
        {
            if (playerShip == null) return;

            var power = playerShip.Power;
            if (power == null) return;

            // Shift + key to add power, Ctrl + Shift + key to remove
            bool removing = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (Input.GetKeyDown(KeyCode.S)) // Shields
            {
                var shields = playerShip.GetSystem<ShieldSystem>();
                if (shields != null)
                {
                    if (removing) power.RemovePower(shields, 1);
                    else power.AllocatePower(shields, 1);
                }
            }

            if (Input.GetKeyDown(KeyCode.E)) // Engines
            {
                var engines = playerShip.GetSystem<EngineSystem>();
                if (engines != null)
                {
                    if (removing) power.RemovePower(engines, 1);
                    else power.AllocatePower(engines, 1);
                }
            }

            if (Input.GetKeyDown(KeyCode.W)) // Weapons
            {
                var weapons = playerShip.GetSystem<WeaponControlSystem>();
                if (weapons != null)
                {
                    if (removing) power.RemovePower(weapons, 1);
                    else power.AllocatePower(weapons, 1);
                }
            }

            if (Input.GetKeyDown(KeyCode.O)) // Oxygen
            {
                var oxygen = playerShip.GetSystem<OxygenSystem>();
                if (oxygen != null)
                {
                    if (removing) power.RemovePower(oxygen, 1);
                    else power.AllocatePower(oxygen, 1);
                }
            }
        }

        // ==================== NORMAL INPUT ====================

        private void HandleNormalInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }

            HandleMouseHover();
        }

        private void HandleLeftClick()
        {
            var hit = GetObjectUnderMouse();

            if (hit != null)
            {
                // Check for room click
                var room = hit.GetComponent<Room>();
                if (room != null)
                {
                    SelectRoom(room);
                    OnRoomClicked?.Invoke(room);
                    return;
                }

                // Check for crew click
                var crew = hit.GetComponent<Crew.CrewMember>();
                if (crew != null)
                {
                    SelectCrew(crew);
                    OnCrewClicked?.Invoke(crew);
                    return;
                }
            }

            // Clicked empty space
            ClearSelection();
        }

        private void HandleRightClick()
        {
            var hit = GetObjectUnderMouse();

            if (hit != null)
            {
                // Right-click on room
                var room = hit.GetComponent<Room>();
                if (room != null)
                {
                    OnRoomRightClicked?.Invoke(room);

                    // If crew selected, command them to this room
                    if (selectedCrew != null)
                    {
                        selectedCrew.MoveTo(room);
                    }

                    return;
                }
            }
        }

        private void HandleMouseHover()
        {
            // Could show tooltips for objects under mouse
        }

        // ==================== TARGETING INPUT ====================

        private void HandleTargetingInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var hit = GetObjectUnderMouse();

                if (hit != null)
                {
                    var room = hit.GetComponent<Room>();
                    if (room != null)
                    {
                        // Check if room is on enemy ship
                        var shipController = room.GetComponentInParent<ShipController>();
                        if (shipController != null && shipController != playerShip)
                        {
                            SelectTarget(room);
                            return;
                        }
                    }
                }
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargeting();
            }
        }

        private void SelectTarget(Room targetRoom)
        {
            if (selectedWeaponIndex >= 0)
            {
                var weapons = playerShip.GetSystem<WeaponControlSystem>();
                if (weapons != null && selectedWeaponIndex < weapons.Weapons.Count)
                {
                    weapons.SetWeaponTarget(weapons.Weapons[selectedWeaponIndex], targetRoom);
                }
            }

            OnTargetSelected?.Invoke(targetRoom);
            CancelTargeting();
        }

        private void CancelTargeting()
        {
            isTargeting = false;
            selectedWeaponIndex = -1;
            currentMode = InputMode.Normal;
        }

        // ==================== CREW COMMAND INPUT ====================

        private void HandleCrewCommandInput()
        {
            if (selectedCrew == null)
            {
                currentMode = InputMode.Normal;
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                var hit = GetObjectUnderMouse();

                if (hit != null)
                {
                    var room = hit.GetComponent<Room>();
                    if (room != null)
                    {
                        selectedCrew.MoveTo(room);
                    }

                    var targetCrew = hit.GetComponent<Crew.CrewMember>();
                    if (targetCrew != null && targetCrew.IsHostile)
                    {
                        selectedCrew.SetCombatTarget(targetCrew);
                    }
                }
            }
        }

        // ==================== SELECTION MANAGEMENT ====================

        public void SelectRoom(Room room)
        {
            selectedRoom = room;
            selectedCrew = null;
        }

        public void SelectCrew(Crew.CrewMember crew)
        {
            selectedCrew = crew;
            currentMode = InputMode.CrewCommand;
        }

        public void SelectWeapon(int weaponIndex)
        {
            var weapons = playerShip?.GetSystem<WeaponControlSystem>();
            if (weapons == null || weaponIndex >= weapons.Weapons.Count)
                return;

            selectedWeaponIndex = weaponIndex;
            isTargeting = true;
            currentMode = InputMode.Targeting;

            OnWeaponSelected?.Invoke(weaponIndex);
        }

        public void ClearSelection()
        {
            selectedRoom = null;
            selectedCrew = null;
            selectedWeaponIndex = -1;
            isTargeting = false;
            currentMode = InputMode.Normal;

            OnSelectionCleared?.Invoke();
        }

        // ==================== UTILITY ====================

        private GameObject GetObjectUnderMouse()
        {
            if (mainCamera == null) return null;

            Vector3 mousePos = Input.mousePosition;
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null)
            {
                return hit.collider.gameObject;
            }

            return null;
        }

        public void SetPlayerShip(ShipController ship)
        {
            playerShip = ship;
        }
    }

    public enum InputMode
    {
        Normal,
        Targeting,
        CrewCommand,
        Paused
    }
}
