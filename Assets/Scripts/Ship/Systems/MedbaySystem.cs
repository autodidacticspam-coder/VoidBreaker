using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Crew;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Medbay/Clone Bay system - heals crew or respawns them.
    /// </summary>
    public class MedbaySystem : ShipSystem
    {
        [Header("Medbay Mode")]
        [SerializeField] private MedbayMode mode = MedbayMode.Heal;

        [Header("Healing")]
        [SerializeField] private float healRate = 10f; // HP per second
        [SerializeField] private int healRadius = 0; // 0 = room only, 1+ = adjacent rooms

        [Header("Clone Bay")]
        [SerializeField] private bool isCloning = false;
        [SerializeField] private float cloneProgress = 0f;
        [SerializeField] private float cloneTime = 10f;
        [SerializeField] private CrewMember pendingClone;
        [SerializeField] private Queue<CrewMember> cloneQueue = new Queue<CrewMember>();

        [Header("Clone Penalties")]
        [SerializeField] private int skillLossOnClone = 1; // Lose this many skill levels

        public MedbayMode Mode => mode;
        public bool IsCloning => isCloning;
        public float CloneProgress => isCloning ? cloneProgress / cloneTime : 0f;
        public float HealRate => IsOperational && AllocatedPower > 0 ? healRate * ((float)AllocatedPower / maxPower) : 0f;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.Medbay;
            maxPower = level;

            // Higher levels = faster healing
            healRate = 8f + (level * 4f);
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (!IsOperational || AllocatedPower == 0)
                return;

            if (mode == MedbayMode.Heal)
            {
                HealCrewInRange(deltaTime);
            }
            else if (mode == MedbayMode.Clone)
            {
                ProcessCloning(deltaTime);
            }
        }

        private void HealCrewInRange(float deltaTime)
        {
            Room medbayRoom = GetMedbayRoom();
            if (medbayRoom == null) return;

            float effectiveHealRate = HealRate * deltaTime;

            // Heal crew in medbay room
            foreach (var crew in medbayRoom.GetCrewInRoom())
            {
                if (crew.CurrentHealth < crew.MaxHealth)
                {
                    crew.Heal(Mathf.CeilToInt(effectiveHealRate));
                }
            }

            // If radius > 0, heal adjacent rooms too (at reduced rate)
            if (healRadius > 0)
            {
                foreach (var adjacent in medbayRoom.AdjacentRooms)
                {
                    foreach (var crew in adjacent.GetCrewInRoom())
                    {
                        if (crew.CurrentHealth < crew.MaxHealth)
                        {
                            crew.Heal(Mathf.CeilToInt(effectiveHealRate * 0.5f));
                        }
                    }
                }
            }
        }

        private void ProcessCloning(float deltaTime)
        {
            if (!isCloning && cloneQueue.Count > 0)
            {
                // Start cloning next crew
                pendingClone = cloneQueue.Dequeue();
                isCloning = true;
                cloneProgress = 0f;
                Debug.Log($"[MedbaySystem] Starting clone of {pendingClone.CrewName}");
            }

            if (isCloning)
            {
                float cloneRate = 1f;
                if (IsManned)
                    cloneRate = 1.25f;

                cloneProgress += deltaTime * cloneRate * ((float)AllocatedPower / maxPower);

                if (cloneProgress >= cloneTime)
                {
                    CompleteClone();
                }
            }
        }

        private void CompleteClone()
        {
            if (pendingClone == null) return;

            Room medbayRoom = GetMedbayRoom();
            if (medbayRoom == null) return;

            // Respawn crew with reduced skills
            pendingClone.Respawn(medbayRoom.Position, skillLossOnClone);
            pendingClone.SetCurrentRoom(medbayRoom);

            Debug.Log($"[MedbaySystem] {pendingClone.CrewName} cloned successfully!");

            GameEvents.TriggerCrewCloned(new CrewClonedArgs
            {
                crew = pendingClone,
                skillLoss = skillLossOnClone
            });

            isCloning = false;
            cloneProgress = 0f;
            pendingClone = null;
        }

        public void QueueForCloning(CrewMember crew)
        {
            if (mode != MedbayMode.Clone)
            {
                Debug.Log("[MedbaySystem] Not in clone mode!");
                return;
            }

            if (!cloneQueue.Contains(crew))
            {
                cloneQueue.Enqueue(crew);
                Debug.Log($"[MedbaySystem] {crew.CrewName} queued for cloning");
            }
        }

        public void SetMode(MedbayMode newMode)
        {
            if (mode == newMode) return;

            mode = newMode;

            // Clear clone queue if switching away from clone mode
            if (mode != MedbayMode.Clone)
            {
                cloneQueue.Clear();
                isCloning = false;
                cloneProgress = 0f;
                pendingClone = null;
            }

            Debug.Log($"[MedbaySystem] Mode set to {mode}");
        }

        private Room GetMedbayRoom()
        {
            foreach (var room in parentShip.Rooms)
            {
                if (room.System == this)
                    return room;
            }
            return null;
        }

        public override void OnDamaged(int amount)
        {
            base.OnDamaged(amount);

            // Damage slows cloning
            if (isCloning)
            {
                cloneProgress = Mathf.Max(0, cloneProgress - 1f);
            }
        }
    }

    public enum MedbayMode
    {
        Heal,   // Standard medbay - heals living crew
        Clone   // Clone bay - respawns dead crew
    }
}
