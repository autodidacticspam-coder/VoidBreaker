using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Ship
{
    /// <summary>
    /// Sensor system - provides visibility into enemy ship and reveals information.
    /// </summary>
    public class SensorSystem : ShipSystem
    {
        [Header("Sensor Levels")]
        [SerializeField] private SensorLevel currentSensorLevel = SensorLevel.None;
        [SerializeField] private bool canSeeEnemyShip = false;
        [SerializeField] private bool canSeeEnemyWeapons = false;
        [SerializeField] private bool canSeeEnemyPower = false;
        [SerializeField] private bool canSeeEnemyCrew = false;
        [SerializeField] private bool canSeeEnemySystems = false;

        [Header("Detection")]
        [SerializeField] private bool canDetectCloaked = false;
        [SerializeField] private float cloakDetectionChance = 0f;

        [Header("Nebula Penalty")]
        [SerializeField] private bool inNebula = false;
        [SerializeField] private int nebulaPenalty = 2;

        public SensorLevel CurrentSensorLevel => currentSensorLevel;
        public bool CanSeeEnemyShip => canSeeEnemyShip;
        public bool CanSeeEnemyWeapons => canSeeEnemyWeapons;
        public bool CanSeeEnemyPower => canSeeEnemyPower;
        public bool CanSeeEnemyCrew => canSeeEnemyCrew;
        public bool CanSeeEnemySystems => canSeeEnemySystems;

        public override void Initialize(ShipController ship, int level)
        {
            base.Initialize(ship, level);
            systemType = SystemType.Sensors;
            maxPower = level;

            UpdateSensorLevel();
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            UpdateSensorLevel();
        }

        private void UpdateSensorLevel()
        {
            int effectivePower = AllocatedPower;

            // Apply nebula penalty
            if (inNebula)
            {
                effectivePower -= nebulaPenalty;
            }

            // Apply damage penalty
            effectivePower -= damageLevel;

            effectivePower = Mathf.Max(0, effectivePower);

            // Determine sensor level
            currentSensorLevel = effectivePower switch
            {
                0 => SensorLevel.None,
                1 => SensorLevel.Basic,
                2 => SensorLevel.Enhanced,
                _ => SensorLevel.Advanced
            };

            // Update visibility flags
            canSeeEnemyShip = currentSensorLevel >= SensorLevel.Basic;
            canSeeEnemyWeapons = currentSensorLevel >= SensorLevel.Basic;
            canSeeEnemyPower = currentSensorLevel >= SensorLevel.Enhanced;
            canSeeEnemyCrew = currentSensorLevel >= SensorLevel.Enhanced;
            canSeeEnemySystems = currentSensorLevel >= SensorLevel.Advanced;

            // Cloak detection at level 3
            canDetectCloaked = currentLevel >= 3 && effectivePower >= 3;
            cloakDetectionChance = canDetectCloaked ? 0.25f : 0f;
        }

        public void SetNebulaState(bool nebula)
        {
            inNebula = nebula;
            UpdateSensorLevel();

            if (nebula)
            {
                Debug.Log("[SensorSystem] Nebula interference detected - sensors reduced");
            }
        }

        public bool TryDetectCloakedShip()
        {
            if (!canDetectCloaked) return false;

            bool detected = Random.value < cloakDetectionChance;

            if (detected)
            {
                Debug.Log("[SensorSystem] Cloaked vessel detected!");
            }

            return detected;
        }

        public EnemyShipInfo GetEnemyShipInfo(ShipController enemy)
        {
            var info = new EnemyShipInfo();

            if (!canSeeEnemyShip || enemy == null)
            {
                info.visible = false;
                return info;
            }

            info.visible = true;
            info.hullPercent = (float)enemy.CurrentHealth / enemy.MaxHealth;

            if (canSeeEnemyWeapons)
            {
                var weapons = enemy.GetSystem<WeaponControlSystem>();
                if (weapons != null)
                {
                    info.weaponCount = weapons.Weapons.Count;
                    info.weaponsCharging = weapons.GetChargingWeaponCount();
                }
            }

            if (canSeeEnemyPower)
            {
                info.shieldLayers = enemy.GetSystem<ShieldSystem>()?.CurrentLayers ?? 0;
                info.maxShieldLayers = enemy.GetSystem<ShieldSystem>()?.MaxLayers ?? 0;
            }

            if (canSeeEnemyCrew)
            {
                info.crewCount = enemy.CrewCount;
                info.crewLocations = new int[enemy.Rooms.Count];
                for (int i = 0; i < enemy.Rooms.Count; i++)
                {
                    info.crewLocations[i] = enemy.Rooms[i].CrewCount;
                }
            }

            if (canSeeEnemySystems)
            {
                info.systemDamage = new int[enemy.Systems.Count];
                for (int i = 0; i < enemy.Systems.Count; i++)
                {
                    info.systemDamage[i] = enemy.Systems[i].DamageLevel;
                }
            }

            return info;
        }
    }

    public enum SensorLevel
    {
        None,       // No visibility
        Basic,      // See ship layout, weapons
        Enhanced,   // See shields, crew positions
        Advanced    // See system damage, power allocation
    }

    public struct EnemyShipInfo
    {
        public bool visible;
        public float hullPercent;
        public int weaponCount;
        public int weaponsCharging;
        public int shieldLayers;
        public int maxShieldLayers;
        public int crewCount;
        public int[] crewLocations;
        public int[] systemDamage;
    }
}
