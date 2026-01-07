using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Sector;

namespace VoidBreaker.Events
{
    /// <summary>
    /// Manages text events - the narrative encounters that drive the game.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private GameEvent currentEvent;
        [SerializeField] private bool eventActive = false;
        [SerializeField] private EventOutcome lastOutcome;

        [Header("Event Database")]
        [SerializeField] private List<GameEvent> allEvents = new List<GameEvent>();
        [SerializeField] private List<GameEvent> sectorEvents = new List<GameEvent>();
        [SerializeField] private List<GameEvent> distressEvents = new List<GameEvent>();
        [SerializeField] private List<GameEvent> questEvents = new List<GameEvent>();

        [Header("References")]
        [SerializeField] private ShipController playerShip;

        public GameEvent CurrentEvent => currentEvent;
        public bool EventActive => eventActive;
        public EventOutcome LastOutcome => lastOutcome;

        // Events
        public event Action<GameEvent> OnEventStarted;
        public event Action<EventOutcome> OnEventEnded;
        public event Action<EventChoice> OnChoiceMade;

        public void Initialize(ShipController player)
        {
            playerShip = player;
            LoadEventDatabase();
        }

        private void LoadEventDatabase()
        {
            // Create event database (in full game, load from ScriptableObjects)
            CreateBaseEvents();
            CreateDistressEvents();
            CreateQuestEvents();

            Debug.Log($"[EventManager] Loaded {allEvents.Count} events");
        }

        // ==================== EVENT TRIGGERING ====================

        public void TriggerRandomEvent(BeaconType beaconType, SectorType sectorType)
        {
            List<GameEvent> validEvents = GetValidEvents(beaconType, sectorType);

            if (validEvents.Count == 0)
            {
                Debug.Log("[EventManager] No valid events for this location");
                TriggerEmptyEvent();
                return;
            }

            // Weight-based selection
            float totalWeight = 0f;
            foreach (var evt in validEvents)
            {
                totalWeight += evt.Weight;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var evt in validEvents)
            {
                cumulative += evt.Weight;
                if (roll <= cumulative)
                {
                    StartEvent(evt);
                    return;
                }
            }

            // Fallback
            StartEvent(validEvents[0]);
        }

        public void TriggerSpecificEvent(string eventId)
        {
            var evt = allEvents.Find(e => e.EventId == eventId);
            if (evt != null)
            {
                StartEvent(evt);
            }
            else
            {
                Debug.LogWarning($"[EventManager] Event not found: {eventId}");
            }
        }

        private void TriggerEmptyEvent()
        {
            var emptyEvent = new GameEvent
            {
                EventId = "empty_beacon",
                Title = "Empty Space",
                Description = "Nothing of interest at this location. The void stretches endlessly in all directions.",
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        ChoiceId = "leave",
                        Text = "Continue onward.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome { Type = EventOutcomeType.Nothing, Weight = 100 }
                        }
                    }
                }
            };

            StartEvent(emptyEvent);
        }

        private List<GameEvent> GetValidEvents(BeaconType beacon, SectorType sector)
        {
            List<GameEvent> valid = new List<GameEvent>();

            foreach (var evt in allEvents)
            {
                if (evt.MeetsRequirements(playerShip, beacon, sector))
                {
                    valid.Add(evt);
                }
            }

            return valid;
        }

        // ==================== EVENT FLOW ====================

        public void StartEvent(GameEvent evt)
        {
            currentEvent = evt.Clone();
            eventActive = true;

            // Process any entry effects
            foreach (var effect in currentEvent.OnEnterEffects)
            {
                ApplyEffect(effect);
            }

            OnEventStarted?.Invoke(currentEvent);

            GameEvents.TriggerEventStarted(new EventStartedArgs
            {
                eventId = currentEvent.EventId,
                title = currentEvent.Title
            });

            Debug.Log($"[EventManager] Event started: {currentEvent.Title}");
        }

        public void MakeChoice(int choiceIndex)
        {
            if (!eventActive || currentEvent == null)
                return;

            if (choiceIndex < 0 || choiceIndex >= currentEvent.Choices.Count)
                return;

            var choice = currentEvent.Choices[choiceIndex];

            // Check if choice has requirements
            if (!choice.MeetsRequirements(playerShip))
            {
                Debug.Log("[EventManager] Choice requirements not met");
                return;
            }

            OnChoiceMade?.Invoke(choice);

            // Resolve outcome
            var outcome = choice.ResolveOutcome();
            lastOutcome = outcome;

            // Apply outcome effects
            ApplyOutcome(outcome);

            // Check if event continues or ends
            if (outcome.NextEventId != null)
            {
                // Chain to next event
                TriggerSpecificEvent(outcome.NextEventId);
            }
            else
            {
                // End event
                EndEvent(outcome);
            }
        }

        private void EndEvent(EventOutcome outcome)
        {
            eventActive = false;

            OnEventEnded?.Invoke(outcome);

            GameEvents.TriggerEventEnded(new EventEndedArgs
            {
                eventId = currentEvent?.EventId,
                outcomeType = outcome.Type
            });

            currentEvent = null;

            Debug.Log("[EventManager] Event ended");
        }

        // ==================== OUTCOME APPLICATION ====================

        private void ApplyOutcome(EventOutcome outcome)
        {
            switch (outcome.Type)
            {
                case EventOutcomeType.Nothing:
                    break;

                case EventOutcomeType.GainScrap:
                    GameManager.Instance?.CurrentRun?.AddScrap(outcome.Amount);
                    Debug.Log($"[EventManager] Gained {outcome.Amount} scrap");
                    break;

                case EventOutcomeType.LoseScrap:
                    GameManager.Instance?.CurrentRun?.SpendScrap(outcome.Amount);
                    Debug.Log($"[EventManager] Lost {outcome.Amount} scrap");
                    break;

                case EventOutcomeType.GainFuel:
                    GameManager.Instance?.CurrentRun?.AddFuel(outcome.Amount);
                    break;

                case EventOutcomeType.LoseFuel:
                    GameManager.Instance?.CurrentRun?.SpendFuel(outcome.Amount);
                    break;

                case EventOutcomeType.GainCrew:
                    SpawnRewardCrew(outcome.CrewRace);
                    break;

                case EventOutcomeType.LoseCrew:
                    KillRandomCrew();
                    break;

                case EventOutcomeType.GainWeapon:
                    GiveWeapon(outcome.WeaponId);
                    break;

                case EventOutcomeType.TakeDamage:
                    playerShip.TakeDamage(outcome.Amount);
                    break;

                case EventOutcomeType.RepairHull:
                    playerShip.RepairHull(outcome.Amount);
                    break;

                case EventOutcomeType.StartCombat:
                    StartCombatFromEvent(outcome.EnemyId);
                    break;

                case EventOutcomeType.GainMutationPoints:
                    GameManager.Instance?.CurrentRun?.AddMutationPoints(outcome.Amount);
                    break;

                case EventOutcomeType.UnlockMutation:
                    UnlockMutation(outcome.MutationId);
                    break;

                case EventOutcomeType.GainAugment:
                    GiveAugment(outcome.AugmentId);
                    break;

                case EventOutcomeType.SystemDamage:
                    DamageRandomSystem(outcome.Amount);
                    break;

                case EventOutcomeType.CrewDamage:
                    DamageAllCrew(outcome.Amount);
                    break;

                case EventOutcomeType.EvolutionBonus:
                    ApplyEvolutionBonus(outcome.EvolutionPath, outcome.Amount);
                    break;
            }

            // Apply text result
            if (!string.IsNullOrEmpty(outcome.ResultText))
            {
                GameEvents.TriggerEventResult(new EventResultArgs
                {
                    text = outcome.ResultText
                });
            }
        }

        private void ApplyEffect(EventEffect effect)
        {
            // Similar to outcomes but for entry effects
        }

        // ==================== SPECIFIC OUTCOMES ====================

        private void SpawnRewardCrew(CrewRace race)
        {
            var crewManager = playerShip.GetComponent<CrewManager>();
            if (crewManager != null)
            {
                crewManager.SpawnNewCrew(race);
            }
        }

        private void KillRandomCrew()
        {
            var crewManager = playerShip.GetComponent<CrewManager>();
            if (crewManager != null)
            {
                var allCrew = crewManager.GetAllCrew();
                if (allCrew.Count > 0)
                {
                    var victim = allCrew[UnityEngine.Random.Range(0, allCrew.Count)];
                    victim.TakeDamage(9999);
                }
            }
        }

        private void GiveWeapon(string weaponId)
        {
            // Load weapon from database and add to ship
            var weapons = playerShip.GetSystem<WeaponControlSystem>();
            if (weapons != null && !string.IsNullOrEmpty(weaponId))
            {
                // In full implementation, load from Resources
                Debug.Log($"[EventManager] Weapon reward: {weaponId}");
            }
        }

        private void GiveAugment(string augmentId)
        {
            GameManager.Instance?.CurrentRun?.AddAugment(augmentId);
        }

        private void StartCombatFromEvent(string enemyId)
        {
            // Create enemy and start combat
            var combatManager = FindObjectOfType<CombatManager>();
            if (combatManager != null)
            {
                // In full implementation, create enemy from database
                Debug.Log($"[EventManager] Starting combat with {enemyId}");
            }
        }

        private void UnlockMutation(string mutationId)
        {
            var evolution = playerShip.GetComponent<EvolutionManager>();
            if (evolution != null)
            {
                // Unlock specific mutation
            }
        }

        private void DamageRandomSystem(int amount)
        {
            if (playerShip.Systems.Count > 0)
            {
                var system = playerShip.Systems[UnityEngine.Random.Range(0, playerShip.Systems.Count)];
                system.OnDamaged(amount);
            }
        }

        private void DamageAllCrew(int amount)
        {
            var crewManager = playerShip.GetComponent<CrewManager>();
            if (crewManager != null)
            {
                foreach (var crew in crewManager.GetAllCrew())
                {
                    crew.TakeDamage(amount);
                }
            }
        }

        private void ApplyEvolutionBonus(EvolutionPath path, int points)
        {
            var evolution = playerShip.GetComponent<EvolutionManager>();
            if (evolution != null)
            {
                evolution.AddPathProgress(path, points);
            }
        }

        // ==================== EVENT DATABASE ====================

        private void CreateBaseEvents()
        {
            // Derelict ship event
            allEvents.Add(new GameEvent
            {
                EventId = "derelict_ship",
                Title = "Derelict Vessel",
                Description = "Your sensors detect a drifting ship, its hull scarred by weapons fire. " +
                    "Life signs are faint, but the cargo bay appears intact.",
                Weight = 10f,
                ValidBeacons = new[] { BeaconType.Unknown, BeaconType.Distress },
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        ChoiceId = "search",
                        Text = "Board and search the vessel.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.GainScrap,
                                Amount = 30,
                                Weight = 50,
                                ResultText = "You find useful salvage in the cargo bay. 30 scrap acquired."
                            },
                            new EventOutcome
                            {
                                Type = EventOutcomeType.StartCombat,
                                EnemyId = "pirate_scout",
                                Weight = 30,
                                ResultText = "It's a trap! Pirates emerge from hiding!"
                            },
                            new EventOutcome
                            {
                                Type = EventOutcomeType.GainCrew,
                                CrewRace = CrewRace.Human,
                                Weight = 20,
                                ResultText = "You find a survivor floating in stasis. They join your crew."
                            }
                        }
                    },
                    new EventChoice
                    {
                        ChoiceId = "ignore",
                        Text = "It's not worth the risk. Move on.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome { Type = EventOutcomeType.Nothing, Weight = 100 }
                        }
                    }
                }
            });

            // Merchant encounter
            allEvents.Add(new GameEvent
            {
                EventId = "merchant_ship",
                Title = "Wandering Merchant",
                Description = "A small trading vessel hails you, its captain offering wares at reasonable prices.",
                Weight = 8f,
                ValidBeacons = new[] { BeaconType.Unknown, BeaconType.Event },
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        ChoiceId = "trade_fuel",
                        Text = "Trade 15 scrap for 3 fuel.",
                        RequiredScrap = 15,
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.LoseScrap,
                                Amount = 15,
                                Weight = 100
                            },
                            new EventOutcome
                            {
                                Type = EventOutcomeType.GainFuel,
                                Amount = 3,
                                Weight = 100,
                                ResultText = "Fair trade. 3 fuel acquired."
                            }
                        }
                    },
                    new EventChoice
                    {
                        ChoiceId = "trade_repairs",
                        Text = "Trade 20 scrap for hull repairs.",
                        RequiredScrap = 20,
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.LoseScrap,
                                Amount = 20,
                                Weight = 100
                            },
                            new EventOutcome
                            {
                                Type = EventOutcomeType.RepairHull,
                                Amount = 10,
                                Weight = 100,
                                ResultText = "The merchant's crew makes quick work of your hull damage."
                            }
                        }
                    },
                    new EventChoice
                    {
                        ChoiceId = "leave",
                        Text = "No thanks.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome { Type = EventOutcomeType.Nothing, Weight = 100 }
                        }
                    }
                }
            });

            // Nebula anomaly
            allEvents.Add(new GameEvent
            {
                EventId = "nebula_anomaly",
                Title = "Strange Readings",
                Description = "Deep within the nebula, your sensors detect an unusual energy signature. " +
                    "It pulses in a rhythm almost like... a heartbeat.",
                Weight = 6f,
                ValidSectors = new[] { SectorType.Nebula },
                ValidBeacons = new[] { BeaconType.Unknown, BeaconType.Event },
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        ChoiceId = "investigate",
                        Text = "Follow the signal to its source.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.GainMutationPoints,
                                Amount = 15,
                                Weight = 40,
                                ResultText = "The energy washes over your ship. Something inside it... changes. (+15 Mutation Points)"
                            },
                            new EventOutcome
                            {
                                Type = EventOutcomeType.SystemDamage,
                                Amount = 2,
                                Weight = 30,
                                ResultText = "A surge of energy damages your systems!"
                            },
                            new EventOutcome
                            {
                                Type = EventOutcomeType.UnlockMutation,
                                MutationId = "void_touched",
                                Weight = 10,
                                ResultText = "Your ship resonates with the void itself. Something awakens within it."
                            },
                            new EventOutcome
                            {
                                Type = EventOutcomeType.Nothing,
                                Weight = 20,
                                ResultText = "The signal fades before you can reach it."
                            }
                        }
                    },
                    new EventChoice
                    {
                        ChoiceId = "ignore",
                        Text = "The nebula is dangerous enough. Keep moving.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome { Type = EventOutcomeType.Nothing, Weight = 100 }
                        }
                    }
                }
            });

            // Evolution choice event
            allEvents.Add(new GameEvent
            {
                EventId = "evolution_crossroads",
                Title = "The Ship Dreams",
                Description = "In the quiet of jump space, you sense something from your ship. " +
                    "It's growing. Evolving. And it awaits your guidance.",
                Weight = 4f,
                MinSector = 2,
                ValidBeacons = new[] { BeaconType.Unknown, BeaconType.Event },
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        ChoiceId = "predator",
                        Text = "Feed its hunger for battle. (Predator path)",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.EvolutionBonus,
                                EvolutionPath = EvolutionPath.Predator,
                                Amount = 20,
                                Weight = 100,
                                ResultText = "The ship's weapons hum with newfound aggression."
                            }
                        }
                    },
                    new EventChoice
                    {
                        ChoiceId = "phantom",
                        Text = "Teach it to hide. To survive. (Phantom path)",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.EvolutionBonus,
                                EvolutionPath = EvolutionPath.Phantom,
                                Amount = 20,
                                Weight = 100,
                                ResultText = "The ship seems to fade, becoming harder to detect."
                            }
                        }
                    },
                    new EventChoice
                    {
                        ChoiceId = "herald",
                        Text = "Show it the value of connection. (Herald path)",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.EvolutionBonus,
                                EvolutionPath = EvolutionPath.Herald,
                                Amount = 20,
                                Weight = 100,
                                ResultText = "The ship's systems hum with a new kind of awareness."
                            }
                        }
                    }
                }
            });
        }

        private void CreateDistressEvents()
        {
            distressEvents.Add(new GameEvent
            {
                EventId = "distress_civilian",
                Title = "Civilian Distress Call",
                Description = "A civilian transport is under attack by pirates! They're calling for help.",
                Weight = 10f,
                ValidBeacons = new[] { BeaconType.Distress },
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        ChoiceId = "help",
                        Text = "Answer their call. Fight off the pirates.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.StartCombat,
                                EnemyId = "pirate_fighter",
                                Weight = 70,
                                ResultText = "You engage the pirates!"
                            },
                            new EventOutcome
                            {
                                Type = EventOutcomeType.GainScrap,
                                Amount = 25,
                                Weight = 30,
                                ResultText = "The pirates flee at your approach. The grateful civilians reward you."
                            }
                        }
                    },
                    new EventChoice
                    {
                        ChoiceId = "ignore",
                        Text = "It's not your fight. Keep moving.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.Nothing,
                                Weight = 100,
                                ResultText = "You leave the transport to its fate."
                            }
                        }
                    }
                }
            });

            allEvents.AddRange(distressEvents);
        }

        private void CreateQuestEvents()
        {
            // Quest events are multi-part stories
            questEvents.Add(new GameEvent
            {
                EventId = "quest_ancient_beacon_start",
                Title = "Ancient Signal",
                Description = "You detect a signal using an encryption method that predates the Hegemony. " +
                    "The coordinates point to a location deep in the next sector.",
                Weight = 3f,
                MinSector = 1,
                MaxSector = 4,
                IsQuest = true,
                ValidBeacons = new[] { BeaconType.Event, BeaconType.Unknown },
                Choices = new List<EventChoice>
                {
                    new EventChoice
                    {
                        ChoiceId = "investigate",
                        Text = "Mark the coordinates. This bears investigation.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome
                            {
                                Type = EventOutcomeType.Nothing,
                                Weight = 100,
                                ResultText = "Quest started: Follow the ancient signal."
                            }
                        }
                    },
                    new EventChoice
                    {
                        ChoiceId = "ignore",
                        Text = "Ancient doesn't mean valuable. Ignore it.",
                        Outcomes = new List<EventOutcome>
                        {
                            new EventOutcome { Type = EventOutcomeType.Nothing, Weight = 100 }
                        }
                    }
                }
            });

            allEvents.AddRange(questEvents);
        }
    }

    // ==================== DATA CLASSES ====================

    [Serializable]
    public class GameEvent
    {
        public string EventId;
        public string Title;
        [TextArea(3, 10)]
        public string Description;
        public float Weight = 1f;
        public bool IsQuest = false;
        public int MinSector = 0;
        public int MaxSector = 10;
        public BeaconType[] ValidBeacons;
        public SectorType[] ValidSectors;
        public List<EventChoice> Choices = new List<EventChoice>();
        public List<EventEffect> OnEnterEffects = new List<EventEffect>();

        public bool MeetsRequirements(ShipController ship, BeaconType beacon, SectorType sector)
        {
            int currentSector = GameManager.Instance?.CurrentRun?.State.currentSector ?? 1;

            if (currentSector < MinSector || currentSector > MaxSector)
                return false;

            if (ValidBeacons != null && ValidBeacons.Length > 0)
            {
                bool found = false;
                foreach (var b in ValidBeacons)
                {
                    if (b == beacon) { found = true; break; }
                }
                if (!found) return false;
            }

            if (ValidSectors != null && ValidSectors.Length > 0)
            {
                bool found = false;
                foreach (var s in ValidSectors)
                {
                    if (s == sector) { found = true; break; }
                }
                if (!found) return false;
            }

            return true;
        }

        public GameEvent Clone()
        {
            return (GameEvent)this.MemberwiseClone();
        }
    }

    [Serializable]
    public class EventChoice
    {
        public string ChoiceId;
        [TextArea(1, 3)]
        public string Text;
        public int RequiredScrap = 0;
        public int RequiredFuel = 0;
        public CrewRace? RequiredCrewRace = null;
        public SystemType? RequiredSystem = null;
        public string RequiredAugment = null;
        public List<EventOutcome> Outcomes = new List<EventOutcome>();

        public bool MeetsRequirements(ShipController ship)
        {
            var run = GameManager.Instance?.CurrentRun;
            if (run == null) return true;

            if (RequiredScrap > 0 && run.State.scrap < RequiredScrap)
                return false;

            if (RequiredFuel > 0 && run.State.fuel < RequiredFuel)
                return false;

            if (RequiredSystem.HasValue && ship.GetSystem(RequiredSystem.Value) == null)
                return false;

            return true;
        }

        public EventOutcome ResolveOutcome()
        {
            if (Outcomes.Count == 0)
                return new EventOutcome { Type = EventOutcomeType.Nothing };

            float totalWeight = 0f;
            foreach (var outcome in Outcomes)
            {
                totalWeight += outcome.Weight;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var outcome in Outcomes)
            {
                cumulative += outcome.Weight;
                if (roll <= cumulative)
                    return outcome;
            }

            return Outcomes[0];
        }
    }

    [Serializable]
    public class EventOutcome
    {
        public EventOutcomeType Type;
        public int Amount;
        public float Weight = 100f;
        public string ResultText;
        public string NextEventId;
        public string EnemyId;
        public string WeaponId;
        public string AugmentId;
        public string MutationId;
        public CrewRace CrewRace;
        public EvolutionPath EvolutionPath;
    }

    [Serializable]
    public class EventEffect
    {
        public EventEventOutcomeType Type;
        public int Amount;
    }

    public enum EventEventOutcomeType
    {
        Nothing,
        GainScrap,
        LoseScrap,
        GainFuel,
        LoseFuel,
        GainCrew,
        LoseCrew,
        GainWeapon,
        TakeDamage,
        RepairHull,
        StartCombat,
        GainMutationPoints,
        UnlockMutation,
        GainAugment,
        SystemDamage,
        CrewDamage,
        EvolutionBonus
    }
}
