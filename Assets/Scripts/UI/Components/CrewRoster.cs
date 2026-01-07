using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Crew;

namespace VoidBreaker.UI
{
    /// <summary>
    /// UI component showing the crew roster.
    /// </summary>
    public class CrewRoster : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform crewContainer;
        [SerializeField] private GameObject crewEntryPrefab;

        private List<CrewMember> displayedCrew = new List<CrewMember>();

        public void SetCrew(IReadOnlyList<CrewMember> crew)
        {
            displayedCrew = new List<CrewMember>(crew);
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            // Update crew roster visuals
        }

        public void SelectCrew(CrewMember crew)
        {
            Debug.Log($"[CrewRoster] Selected: {crew.CrewName}");
        }
    }
}
