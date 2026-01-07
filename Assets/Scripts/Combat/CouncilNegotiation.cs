using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Events;

namespace VoidBreaker.Combat
{
    /// <summary>
    /// Council Negotiation - Final encounter for the Herald evolution path.
    /// A diplomacy-based encounter where you address the Galactic Council.
    /// </summary>
    public class CouncilNegotiation : MonoBehaviour
    {
        [Header("Negotiation State")]
        [SerializeField] private NegotiationPhase currentPhase = NegotiationPhase.Opening;
        [SerializeField] private int supportVotes = 0;
        [SerializeField] private int oppositionVotes = 0;
        [SerializeField] private int undecidedVotes = 5;
        [SerializeField] private int totalVotes = 7;
        [SerializeField] private int votesNeeded = 4;

        [Header("Council Members")]
        [SerializeField] private List<CouncilMember> councilMembers = new List<CouncilMember>();

        [Header("Resources")]
        [SerializeField] private int diplomaticCapital = 3;
        [SerializeField] private int evidenceTokens = 0;
        [SerializeField] private int influencePoints = 0;

        [Header("References")]
        [SerializeField] private ShipController playerShip;

        [Header("Dialogue")]
        [SerializeField] private CouncilDialogue currentDialogue;
        [SerializeField] private int argumentsMade = 0;
        [SerializeField] private int maxArguments = 5;

        // Events
        public event Action<NegotiationPhase> OnPhaseChanged;
        public event Action<CouncilMember, bool> OnVoteChanged;
        public event Action<string> OnDialogueTriggered;
        public event Action<bool> OnNegotiationEnded;

        public int SupportVotes => supportVotes;
        public int OppositionVotes => oppositionVotes;
        public int UndecidedVotes => undecidedVotes;
        public int DiplomaticCapital => diplomaticCapital;
        public NegotiationPhase CurrentPhase => currentPhase;

        public void Initialize(ShipController player)
        {
            playerShip = player;

            // Calculate starting resources based on Herald evolution
            var evolution = playerShip.GetComponent<EvolutionManager>();
            if (evolution != null)
            {
                int heraldLevel = evolution.GetPathLevel(EvolutionPath.Herald);
                diplomaticCapital += heraldLevel;
                influencePoints = heraldLevel * 10;
            }

            // Create council members
            CreateCouncil();

            // Start opening phase
            currentPhase = NegotiationPhase.Opening;

            Debug.Log("[CouncilNegotiation] The Galactic Council convenes to hear your case");

            GameEvents.TriggerBossPhaseChanged(new BossPhaseChangedArgs
            {
                bossName = "GALACTIC COUNCIL",
                phase = 1,
                maxPhases = 3
            });

            TriggerDialogue("opening_statement");
        }

        private void CreateCouncil()
        {
            councilMembers.Clear();

            // Create the 7 council members
            councilMembers.Add(new CouncilMember
            {
                Name = "Admiral Kethros",
                Faction = CouncilFaction.Military,
                Disposition = Disposition.Hostile,
                Concern = "Security",
                SwayDifficulty = 3,
                Description = "The Hegemony's military arm. Values strength above all."
            });

            councilMembers.Add(new CouncilMember
            {
                Name = "Arbiter Velenn",
                Faction = CouncilFaction.Judiciary,
                Disposition = Disposition.Neutral,
                Concern = "Justice",
                SwayDifficulty = 2,
                Description = "Keeper of galactic law. Seeks truth and fairness."
            });

            councilMembers.Add(new CouncilMember
            {
                Name = "Chancellor Mira",
                Faction = CouncilFaction.Commerce,
                Disposition = Disposition.Neutral,
                Concern = "Prosperity",
                SwayDifficulty = 2,
                Description = "Represents trade guilds. Values stability and profit."
            });

            councilMembers.Add(new CouncilMember
            {
                Name = "Elder Thraxis",
                Faction = CouncilFaction.Science,
                Disposition = Disposition.Sympathetic,
                Concern = "Knowledge",
                SwayDifficulty = 1,
                Description = "Head of the Science Collective. Curious about your evolution."
            });

            councilMembers.Add(new CouncilMember
            {
                Name = "Voice Serenna",
                Faction = CouncilFaction.Religion,
                Disposition = Disposition.Neutral,
                Concern = "Harmony",
                SwayDifficulty = 2,
                Description = "Spiritual leader. Believes in cosmic balance."
            });

            councilMembers.Add(new CouncilMember
            {
                Name = "Representative Korr",
                Faction = CouncilFaction.Colonies,
                Disposition = Disposition.Sympathetic,
                Concern = "Freedom",
                SwayDifficulty = 1,
                Description = "Voice of the outer colonies. Has suffered under Hegemony rule."
            });

            councilMembers.Add(new CouncilMember
            {
                Name = "Director Nexus",
                Faction = CouncilFaction.Intelligence,
                Disposition = Disposition.Hostile,
                Concern = "Control",
                SwayDifficulty = 4,
                Description = "Hegemony intelligence chief. Knows your every move."
            });

            // Count initial votes
            foreach (var member in councilMembers)
            {
                if (member.Disposition == Disposition.Sympathetic)
                {
                    supportVotes++;
                    undecidedVotes--;
                }
                else if (member.Disposition == Disposition.Hostile)
                {
                    oppositionVotes++;
                    undecidedVotes--;
                }
            }
        }

        // ==================== DIALOGUE SYSTEM ====================

        private void TriggerDialogue(string dialogueId)
        {
            currentDialogue = GetDialogue(dialogueId);

            if (currentDialogue != null)
            {
                OnDialogueTriggered?.Invoke(currentDialogue.Text);
            }
        }

        private CouncilDialogue GetDialogue(string id)
        {
            // In full implementation, load from database
            return id switch
            {
                "opening_statement" => new CouncilDialogue
                {
                    Id = id,
                    Speaker = "Arbiter Velenn",
                    Text = "The Council recognizes the entity known as 'VoidBreaker.' You stand accused of " +
                           "sedition against the Hegemony, destruction of property, and unlawful evolution. " +
                           "How do you plead?",
                    Choices = new[]
                    {
                        new DialogueChoice { Text = "Not guilty - the Hegemony is the true threat.", Effect = DialogueEffect.Neutral },
                        new DialogueChoice { Text = "I did what was necessary for survival.", Effect = DialogueEffect.Sympathetic },
                        new DialogueChoice { Text = "Guilty - but I had no choice.", Effect = DialogueEffect.Honest }
                    }
                },
                _ => null
            };
        }

        public void MakeDialogueChoice(int choiceIndex)
        {
            if (currentDialogue == null || choiceIndex >= currentDialogue.Choices.Length)
                return;

            var choice = currentDialogue.Choices[choiceIndex];

            // Apply effect
            switch (choice.Effect)
            {
                case DialogueEffect.Sympathetic:
                    influencePoints += 10;
                    SwayNeutralMember(1);
                    break;
                case DialogueEffect.Aggressive:
                    influencePoints -= 5;
                    SwayNeutralMember(-1);
                    break;
                case DialogueEffect.Honest:
                    // Arbiter Velenn appreciates honesty
                    SwaySpecificMember("Arbiter Velenn", 1);
                    influencePoints += 5;
                    break;
            }

            // Progress to arguments phase
            if (currentPhase == NegotiationPhase.Opening)
            {
                TransitionToArguments();
            }
        }

        private void TransitionToArguments()
        {
            currentPhase = NegotiationPhase.Arguments;
            OnPhaseChanged?.Invoke(currentPhase);

            GameEvents.TriggerBossPhaseChanged(new BossPhaseChangedArgs
            {
                bossName = "GALACTIC COUNCIL",
                phase = 2,
                maxPhases = 3
            });

            Debug.Log("[CouncilNegotiation] Present your arguments to the Council");
        }

        // ==================== ARGUMENTS PHASE ====================

        public void PresentArgument(ArgumentType type, CouncilMember target)
        {
            if (currentPhase != NegotiationPhase.Arguments)
                return;

            if (argumentsMade >= maxArguments)
            {
                TransitionToVoting();
                return;
            }

            bool success = false;
            int cost = 1;

            switch (type)
            {
                case ArgumentType.Evidence:
                    if (evidenceTokens > 0)
                    {
                        evidenceTokens--;
                        success = SwayMember(target, 2);
                    }
                    break;

                case ArgumentType.Appeal:
                    if (diplomaticCapital >= cost)
                    {
                        diplomaticCapital -= cost;
                        success = AppealToValues(target);
                    }
                    break;

                case ArgumentType.Bargain:
                    if (diplomaticCapital >= 2)
                    {
                        diplomaticCapital -= 2;
                        success = MakeBargain(target);
                    }
                    break;

                case ArgumentType.Expose:
                    // Risky - expose Hegemony corruption
                    success = ExposeCorruption(target);
                    break;
            }

            argumentsMade++;

            Debug.Log($"[CouncilNegotiation] Argument {argumentsMade}/{maxArguments}: {type} to {target.Name} - {(success ? "Success" : "Failed")}");

            if (argumentsMade >= maxArguments)
            {
                TransitionToVoting();
            }
        }

        private bool SwayMember(CouncilMember member, int power)
        {
            if (member.Disposition == Disposition.Locked)
                return false;

            float chance = (float)(power + influencePoints / 20) / (member.SwayDifficulty + 1);

            if (UnityEngine.Random.value < chance)
            {
                if (member.Disposition == Disposition.Hostile)
                {
                    member.Disposition = Disposition.Neutral;
                    oppositionVotes--;
                    undecidedVotes++;
                }
                else if (member.Disposition == Disposition.Neutral)
                {
                    member.Disposition = Disposition.Sympathetic;
                    undecidedVotes--;
                    supportVotes++;
                }

                OnVoteChanged?.Invoke(member, true);
                return true;
            }

            return false;
        }

        private bool AppealToValues(CouncilMember member)
        {
            // More effective if argument matches their concern
            int bonus = 0;

            // Herald path bonuses
            var evolution = playerShip.GetComponent<EvolutionManager>();
            if (evolution != null)
            {
                bonus = evolution.GetPathLevel(EvolutionPath.Herald) / 2;
            }

            return SwayMember(member, 1 + bonus);
        }

        private bool MakeBargain(CouncilMember member)
        {
            // Commerce and Colonies are more susceptible to bargains
            int bonus = 0;
            if (member.Faction == CouncilFaction.Commerce || member.Faction == CouncilFaction.Colonies)
            {
                bonus = 2;
            }

            return SwayMember(member, 1 + bonus);
        }

        private bool ExposeCorruption(CouncilMember member)
        {
            // High risk, high reward
            // Can backfire and strengthen opposition

            float successChance = 0.4f + (evidenceTokens * 0.1f);

            if (UnityEngine.Random.value < successChance)
            {
                // Success - sway multiple members
                SwayMember(member, 3);

                foreach (var m in councilMembers)
                {
                    if (m.Faction == CouncilFaction.Judiciary || m.Faction == CouncilFaction.Colonies)
                    {
                        SwayMember(m, 1);
                    }
                }

                Debug.Log("[CouncilNegotiation] Corruption exposed! The Council is shaken.");
                return true;
            }
            else
            {
                // Backfire - strengthen opposition
                oppositionVotes++;
                if (undecidedVotes > 0) undecidedVotes--;

                Debug.Log("[CouncilNegotiation] Your accusations seem baseless. Support wavers.");
                return false;
            }
        }

        private void SwayNeutralMember(int direction)
        {
            foreach (var member in councilMembers)
            {
                if (member.Disposition == Disposition.Neutral)
                {
                    if (direction > 0)
                    {
                        member.Disposition = Disposition.Sympathetic;
                        undecidedVotes--;
                        supportVotes++;
                    }
                    else
                    {
                        member.Disposition = Disposition.Hostile;
                        undecidedVotes--;
                        oppositionVotes++;
                    }
                    break;
                }
            }
        }

        private void SwaySpecificMember(string name, int power)
        {
            var member = councilMembers.Find(m => m.Name == name);
            if (member != null)
            {
                SwayMember(member, power);
            }
        }

        // ==================== VOTING PHASE ====================

        private void TransitionToVoting()
        {
            currentPhase = NegotiationPhase.Voting;
            OnPhaseChanged?.Invoke(currentPhase);

            GameEvents.TriggerBossPhaseChanged(new BossPhaseChangedArgs
            {
                bossName = "GALACTIC COUNCIL",
                phase = 3,
                maxPhases = 3
            });

            Debug.Log("[CouncilNegotiation] The Council casts their votes...");

            // Final vote tallying
            StartCoroutine(VotingSequence());
        }

        private System.Collections.IEnumerator VotingSequence()
        {
            yield return new WaitForSeconds(2f);

            // Announce each vote
            foreach (var member in councilMembers)
            {
                string vote = member.Disposition switch
                {
                    Disposition.Sympathetic => "AYE",
                    Disposition.Hostile => "NAY",
                    _ => "ABSTAIN"
                };

                Debug.Log($"[CouncilNegotiation] {member.Name} votes: {vote}");

                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(1f);

            // Determine outcome
            if (supportVotes >= votesNeeded)
            {
                Victory();
            }
            else if (oppositionVotes >= votesNeeded)
            {
                Defeat();
            }
            else
            {
                // Tie or abstention - council is deadlocked
                Stalemate();
            }
        }

        private void Victory()
        {
            currentPhase = NegotiationPhase.Victory;

            Debug.Log("[CouncilNegotiation] THE COUNCIL RULES IN YOUR FAVOR!");

            OnNegotiationEnded?.Invoke(true);

            GameEvents.TriggerBossDefeated(new BossDefeatedArgs
            {
                bossName = "GALACTIC COUNCIL",
                evolutionPath = EvolutionPath.Herald
            });

            GameEvents.TriggerGameEnding(new GameEndingArgs
            {
                endingType = EndingType.HeraldVictory,
                endingTitle = "VOICE OF THE VOID",
                endingDescription = "The Council rules: the Hegemony has overreached. Your evolution is recognized as " +
                    "a new form of life, deserving of rights and recognition. The galaxy will never be the same. " +
                    "You have spoken, and the stars listened."
            });
        }

        private void Defeat()
        {
            currentPhase = NegotiationPhase.Defeat;

            Debug.Log("[CouncilNegotiation] THE COUNCIL RULES AGAINST YOU!");

            OnNegotiationEnded?.Invoke(false);

            // Option to fight your way out or accept fate
            GameEvents.TriggerBossAttack(new BossAttackArgs
            {
                attackName = "GUILTY VERDICT",
                damage = 0
            });

            // Could trigger escape combat here
        }

        private void Stalemate()
        {
            Debug.Log("[CouncilNegotiation] THE COUNCIL IS DEADLOCKED!");

            // Partial victory - you escape but without full recognition
            currentPhase = NegotiationPhase.Victory;
            OnNegotiationEnded?.Invoke(true);

            GameEvents.TriggerGameEnding(new GameEndingArgs
            {
                endingType = EndingType.HeraldVictory,
                endingTitle = "HUNG JURY",
                endingDescription = "The Council cannot reach a verdict. In the chaos, you slip away. " +
                    "Not a full victory, but you have planted seeds of doubt about the Hegemony. " +
                    "Change is coming."
            });
        }

        public void AddEvidence(int amount)
        {
            evidenceTokens += amount;
        }

        public void AddDiplomaticCapital(int amount)
        {
            diplomaticCapital += amount;
        }
    }

    public enum NegotiationPhase
    {
        Opening,
        Arguments,
        Voting,
        Victory,
        Defeat
    }

    public enum ArgumentType
    {
        Evidence,   // Present proof of Hegemony wrongdoing
        Appeal,     // Appeal to a member's values
        Bargain,    // Offer something in exchange
        Expose      // Risky - expose corruption
    }

    [Serializable]
    public class CouncilMember
    {
        public string Name;
        public CouncilFaction Faction;
        public Disposition Disposition;
        public string Concern;
        public int SwayDifficulty;
        public string Description;
    }

    public enum CouncilFaction
    {
        Military,
        Judiciary,
        Commerce,
        Science,
        Religion,
        Colonies,
        Intelligence
    }

    public enum Disposition
    {
        Hostile,
        Neutral,
        Sympathetic,
        Locked // Cannot be swayed
    }

    [Serializable]
    public class CouncilDialogue
    {
        public string Id;
        public string Speaker;
        public string Text;
        public DialogueChoice[] Choices;
    }

    [Serializable]
    public class DialogueChoice
    {
        public string Text;
        public DialogueEffect Effect;
    }

    public enum DialogueEffect
    {
        Neutral,
        Sympathetic,
        Aggressive,
        Honest,
        Deceptive
    }
}
