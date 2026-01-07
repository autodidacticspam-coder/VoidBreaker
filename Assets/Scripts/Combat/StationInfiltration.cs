using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Crew;
using VoidBreaker.Events;

namespace VoidBreaker.Combat
{
    /// <summary>
    /// Station Infiltration - Final encounter for the Phantom evolution path.
    /// A stealth-based encounter where you infiltrate the Hegemony command station.
    /// </summary>
    public class StationInfiltration : MonoBehaviour
    {
        [Header("Mission State")]
        [SerializeField] private InfiltrationPhase currentPhase = InfiltrationPhase.Approach;
        [SerializeField] private float alertLevel = 0f;
        [SerializeField] private float maxAlert = 100f;
        [SerializeField] private bool isDetected = false;
        [SerializeField] private int objectivesCompleted = 0;
        [SerializeField] private int totalObjectives = 4;

        [Header("Station Layout")]
        [SerializeField] private List<StationSection> sections = new List<StationSection>();
        [SerializeField] private StationSection currentSection;

        [Header("References")]
        [SerializeField] private ShipController playerShip;
        [SerializeField] private List<CrewMember> infiltrationTeam = new List<CrewMember>();

        [Header("Timers")]
        [SerializeField] private float missionTimer = 0f;
        [SerializeField] private float maxMissionTime = 300f; // 5 minutes
        [SerializeField] private float alertDecayRate = 2f;
        [SerializeField] private float alertGainRate = 5f;

        // Events
        public event Action<InfiltrationPhase> OnPhaseChanged;
        public event Action<float> OnAlertChanged;
        public event Action<int> OnObjectiveCompleted;
        public event Action<bool> OnMissionEnded;

        public float AlertLevel => alertLevel;
        public float AlertPercent => alertLevel / maxAlert;
        public bool IsDetected => isDetected;
        public int ObjectivesCompleted => objectivesCompleted;
        public InfiltrationPhase CurrentPhase => currentPhase;

        public void Initialize(ShipController player)
        {
            playerShip = player;

            // Create station layout
            CreateStationLayout();

            // Start approach phase
            currentPhase = InfiltrationPhase.Approach;

            Debug.Log("[StationInfiltration] Mission begins - Infiltrate the Hegemony Command Station");

            GameEvents.TriggerBossPhaseChanged(new BossPhaseChangedArgs
            {
                bossName = "HEGEMONY COMMAND STATION",
                phase = 1,
                maxPhases = 4
            });
        }

        private void CreateStationLayout()
        {
            sections.Clear();

            // Create station sections
            sections.Add(new StationSection
            {
                SectionName = "Hangar Bay",
                SecurityLevel = 1,
                HasObjective = false
            });

            sections.Add(new StationSection
            {
                SectionName = "Crew Quarters",
                SecurityLevel = 2,
                HasObjective = false
            });

            sections.Add(new StationSection
            {
                SectionName = "Communications Hub",
                SecurityLevel = 3,
                HasObjective = true,
                ObjectiveDescription = "Disable long-range communications"
            });

            sections.Add(new StationSection
            {
                SectionName = "Power Core",
                SecurityLevel = 4,
                HasObjective = true,
                ObjectiveDescription = "Overload the main reactor"
            });

            sections.Add(new StationSection
            {
                SectionName = "Command Bridge",
                SecurityLevel = 5,
                HasObjective = true,
                ObjectiveDescription = "Eliminate the Station Commander"
            });

            sections.Add(new StationSection
            {
                SectionName = "AI Core",
                SecurityLevel = 5,
                HasObjective = true,
                ObjectiveDescription = "Upload the virus to the Hegemony network"
            });

            currentSection = sections[0];
        }

        private void Update()
        {
            if (currentPhase == InfiltrationPhase.Complete || currentPhase == InfiltrationPhase.Failed)
                return;

            float deltaTime = Time.deltaTime;
            missionTimer += deltaTime;

            // Check mission time limit
            if (missionTimer >= maxMissionTime)
            {
                FailMission("Time expired - station lockdown complete");
                return;
            }

            // Update alert level
            UpdateAlertLevel(deltaTime);

            // Update based on phase
            switch (currentPhase)
            {
                case InfiltrationPhase.Approach:
                    UpdateApproach(deltaTime);
                    break;
                case InfiltrationPhase.Infiltration:
                    UpdateInfiltration(deltaTime);
                    break;
                case InfiltrationPhase.Extraction:
                    UpdateExtraction(deltaTime);
                    break;
            }
        }

        private void UpdateAlertLevel(float deltaTime)
        {
            // Natural decay when not detected
            if (!isDetected && alertLevel > 0)
            {
                alertLevel -= alertDecayRate * deltaTime;
                alertLevel = Mathf.Max(0, alertLevel);
            }

            // Check detection threshold
            if (alertLevel >= maxAlert && !isDetected)
            {
                TriggerFullAlert();
            }

            OnAlertChanged?.Invoke(alertLevel);
        }

        private void TriggerFullAlert()
        {
            isDetected = true;

            Debug.Log("[StationInfiltration] FULL ALERT - You've been detected!");

            GameEvents.TriggerBossAttack(new BossAttackArgs
            {
                attackName = "STATION ALERT",
                damage = 0
            });

            // Spawn security forces in current section
            if (currentSection != null)
            {
                SpawnSecurityForces();
            }
        }

        private void SpawnSecurityForces()
        {
            // In full implementation, create hostile crew in the player's section
            Debug.Log("[StationInfiltration] Security forces deployed!");
        }

        // ==================== PHASE: APPROACH ====================

        private void UpdateApproach(float deltaTime)
        {
            // Player must dock without being detected
            // This is handled by player actions (use cloak, hack sensors, etc.)
        }

        public void AttemptDock(bool usedCloak)
        {
            if (currentPhase != InfiltrationPhase.Approach)
                return;

            if (usedCloak)
            {
                // Clean dock
                Debug.Log("[StationInfiltration] Docked undetected - infiltration begins");
                TransitionToInfiltration();
            }
            else
            {
                // Detected during approach
                alertLevel += 30f;
                Debug.Log("[StationInfiltration] Detected during approach!");
                TransitionToInfiltration();
            }
        }

        private void TransitionToInfiltration()
        {
            currentPhase = InfiltrationPhase.Infiltration;
            OnPhaseChanged?.Invoke(currentPhase);

            GameEvents.TriggerBossPhaseChanged(new BossPhaseChangedArgs
            {
                bossName = "HEGEMONY COMMAND STATION",
                phase = 2,
                maxPhases = 4
            });
        }

        // ==================== PHASE: INFILTRATION ====================

        private void UpdateInfiltration(float deltaTime)
        {
            // Alert gradually increases if in high-security areas
            if (currentSection != null && currentSection.SecurityLevel >= 3)
            {
                // Higher security = faster natural alert gain
                alertLevel += (currentSection.SecurityLevel - 2) * 0.5f * deltaTime;
            }

            // Check if all objectives complete
            if (objectivesCompleted >= totalObjectives)
            {
                TransitionToExtraction();
            }
        }

        public void MoveToSection(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= sections.Count)
                return;

            var newSection = sections[sectionIndex];

            // Movement alert check
            if (newSection.SecurityLevel > currentSection.SecurityLevel)
            {
                alertLevel += 10f * (newSection.SecurityLevel - currentSection.SecurityLevel);
            }

            currentSection = newSection;

            Debug.Log($"[StationInfiltration] Moved to {currentSection.SectionName}");
        }

        public void AttemptObjective()
        {
            if (currentSection == null || !currentSection.HasObjective)
                return;

            if (currentSection.ObjectiveComplete)
            {
                Debug.Log("[StationInfiltration] Objective already complete");
                return;
            }

            // Skill check - harder for higher security
            float successChance = 0.8f - (currentSection.SecurityLevel * 0.1f);

            // Bonus from phantom evolution
            var evolution = playerShip.GetComponent<EvolutionManager>();
            if (evolution != null)
            {
                successChance += evolution.GetPathLevel(EvolutionPath.Phantom) * 0.05f;
            }

            bool success = UnityEngine.Random.value < successChance;

            if (success)
            {
                CompleteObjective(currentSection);
            }
            else
            {
                alertLevel += 20f;
                Debug.Log("[StationInfiltration] Objective attempt failed - alert increased!");
            }
        }

        private void CompleteObjective(StationSection section)
        {
            section.ObjectiveComplete = true;
            objectivesCompleted++;

            OnObjectiveCompleted?.Invoke(objectivesCompleted);

            Debug.Log($"[StationInfiltration] Objective complete: {section.ObjectiveDescription} ({objectivesCompleted}/{totalObjectives})");

            GameEvents.TriggerBossAttack(new BossAttackArgs
            {
                attackName = $"OBJECTIVE: {section.ObjectiveDescription}",
                damage = 0
            });
        }

        private void TransitionToExtraction()
        {
            currentPhase = InfiltrationPhase.Extraction;
            OnPhaseChanged?.Invoke(currentPhase);

            // Full alert on extraction
            alertLevel = maxAlert;
            isDetected = true;

            Debug.Log("[StationInfiltration] All objectives complete - EXTRACT NOW!");

            GameEvents.TriggerBossPhaseChanged(new BossPhaseChangedArgs
            {
                bossName = "HEGEMONY COMMAND STATION",
                phase = 3,
                maxPhases = 4
            });
        }

        // ==================== PHASE: EXTRACTION ====================

        private void UpdateExtraction(float deltaTime)
        {
            // Must return to hangar and escape
            // Time pressure with station collapsing
        }

        public void AttemptExtraction()
        {
            if (currentPhase != InfiltrationPhase.Extraction)
                return;

            if (currentSection != sections[0]) // Must be in hangar
            {
                Debug.Log("[StationInfiltration] Must return to Hangar Bay to extract!");
                return;
            }

            // Extraction successful
            CompleteMission();
        }

        private void CompleteMission()
        {
            currentPhase = InfiltrationPhase.Complete;

            Debug.Log("[StationInfiltration] MISSION COMPLETE - Station destroyed!");

            OnMissionEnded?.Invoke(true);

            GameEvents.TriggerBossDefeated(new BossDefeatedArgs
            {
                bossName = "HEGEMONY COMMAND STATION",
                evolutionPath = EvolutionPath.Phantom
            });

            GameEvents.TriggerGameEnding(new GameEndingArgs
            {
                endingType = EndingType.PhantomVictory,
                endingTitle = "GHOST IN THE MACHINE",
                endingDescription = "The Hegemony Command Station erupts in a cascade of explosions. " +
                    "Their network is compromised, their fleet blind. You slip away into the void, " +
                    "a phantom that was never there. The perfect shadow."
            });

            Destroy(gameObject, 2f);
        }

        private void FailMission(string reason)
        {
            currentPhase = InfiltrationPhase.Failed;

            Debug.Log($"[StationInfiltration] MISSION FAILED - {reason}");

            OnMissionEnded?.Invoke(false);

            // Start combat as extraction
            var combatManager = FindObjectOfType<CombatManager>();
            if (combatManager != null)
            {
                // Create station defense fleet and fight way out
                Debug.Log("[StationInfiltration] Fight your way out!");
            }
        }

        // ==================== PLAYER ACTIONS ====================

        public void UseDistraction()
        {
            alertLevel -= 15f;
            alertLevel = Mathf.Max(0, alertLevel);

            Debug.Log("[StationInfiltration] Distraction deployed - alert reduced");
        }

        public void HackSecurity()
        {
            if (currentSection != null)
            {
                currentSection.SecurityLevel = Mathf.Max(1, currentSection.SecurityLevel - 1);
                alertLevel -= 10f;

                Debug.Log($"[StationInfiltration] Security hacked - {currentSection.SectionName} security lowered");
            }
        }

        public void Assassinate()
        {
            // Silent kill reduces alert
            alertLevel -= 5f;
            alertLevel = Mathf.Max(0, alertLevel);
        }
    }

    public enum InfiltrationPhase
    {
        Approach,
        Infiltration,
        Extraction,
        Complete,
        Failed
    }

    [Serializable]
    public class StationSection
    {
        public string SectionName;
        public int SecurityLevel;
        public bool HasObjective;
        public string ObjectiveDescription;
        public bool ObjectiveComplete;
    }
}
