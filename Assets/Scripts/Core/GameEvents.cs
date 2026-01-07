using System;
using UnityEngine;
using VoidBreaker.Ship;
using VoidBreaker.Crew;
using VoidBreaker.Combat;
using VoidBreaker.Sector;
using VoidBreaker.Evolution;
using VoidBreaker.Events;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Central event bus for decoupled system communication.
    /// All major game events flow through here.
    /// </summary>
    public static class GameEvents
    {
        // ==================== GAME STATE EVENTS ====================

        public static event Action OnGameStarted;
        public static event Action OnGameInitialized;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action<GameOverArgs> OnGameOver;
        public static event Action<GameEndingArgs> OnGameEnding;
        public static event Action OnPauseToggle;

        public static void TriggerGameStarted() => OnGameStarted?.Invoke();
        public static void TriggerGameInitialized() => OnGameInitialized?.Invoke();
        public static void TriggerGamePaused() => OnGamePaused?.Invoke();
        public static void TriggerGameResumed() => OnGameResumed?.Invoke();
        public static void TriggerGameOver(GameOverArgs args) => OnGameOver?.Invoke(args);
        public static void TriggerGameEnding(GameEndingArgs args) => OnGameEnding?.Invoke(args);
        public static void TriggerPauseToggle() => OnPauseToggle?.Invoke();

        // ==================== COMBAT EVENTS ====================

        public static event Action<CombatStartedArgs> OnCombatStarted;
        public static event Action<CombatEndedArgs> OnCombatEnded;
        public static event Action<DamageDealtArgs> OnDamageDealt;
        public static event Action<ProjectileFiredArgs> OnProjectileFired;
        public static event Action<ProjectileHitArgs> OnProjectileHit;
        public static event Action<ProjectileMissedArgs> OnProjectileMissed;
        public static event Action<HullDamagedArgs> OnHullDamaged;
        public static event Action<HullChangedArgs> OnHullChanged;

        public static void TriggerCombatStarted(CombatStartedArgs args) => OnCombatStarted?.Invoke(args);
        public static void TriggerCombatEnded(CombatEndedArgs args) => OnCombatEnded?.Invoke(args);
        public static void TriggerDamageDealt(DamageDealtArgs args) => OnDamageDealt?.Invoke(args);
        public static void TriggerProjectileFired(ProjectileFiredArgs args) => OnProjectileFired?.Invoke(args);
        public static void TriggerProjectileHit(ProjectileHitArgs args) => OnProjectileHit?.Invoke(args);
        public static void TriggerProjectileMissed(ProjectileMissedArgs args) => OnProjectileMissed?.Invoke(args);
        public static void TriggerHullDamaged(HullDamagedArgs args) => OnHullDamaged?.Invoke(args);
        public static void TriggerHullChanged(HullChangedArgs args) => OnHullChanged?.Invoke(args);

        // ==================== BOSS EVENTS ====================

        public static event Action<BossPhaseChangedArgs> OnBossPhaseChanged;
        public static event Action<BossAttackArgs> OnBossAttack;
        public static event Action<BossDefeatedArgs> OnBossDefeated;

        public static void TriggerBossPhaseChanged(BossPhaseChangedArgs args) => OnBossPhaseChanged?.Invoke(args);
        public static void TriggerBossAttack(BossAttackArgs args) => OnBossAttack?.Invoke(args);
        public static void TriggerBossDefeated(BossDefeatedArgs args) => OnBossDefeated?.Invoke(args);

        // ==================== SHIP EVENTS ====================

        public static event Action<SystemDamagedArgs> OnSystemDamaged;
        public static event Action<SystemRepairedArgs> OnSystemRepaired;
        public static event Action<SystemDestroyedArgs> OnSystemDestroyed;
        public static event Action<PowerChangedArgs> OnPowerChanged;
        public static event Action<ShieldChangedArgs> OnShieldChanged;
        public static event Action<RoomBreachArgs> OnRoomBreach;
        public static event Action<RoomFireArgs> OnRoomFire;
        public static event Action<OxygenCriticalArgs> OnOxygenCritical;

        public static void TriggerSystemDamaged(SystemDamagedArgs args) => OnSystemDamaged?.Invoke(args);
        public static void TriggerSystemRepaired(SystemRepairedArgs args) => OnSystemRepaired?.Invoke(args);
        public static void TriggerSystemDestroyed(SystemDestroyedArgs args) => OnSystemDestroyed?.Invoke(args);
        public static void TriggerPowerChanged(PowerChangedArgs args) => OnPowerChanged?.Invoke(args);
        public static void TriggerShieldChanged(ShieldChangedArgs args) => OnShieldChanged?.Invoke(args);
        public static void TriggerRoomBreach(RoomBreachArgs args) => OnRoomBreach?.Invoke(args);
        public static void TriggerRoomFire(RoomFireArgs args) => OnRoomFire?.Invoke(args);
        public static void TriggerOxygenCritical(OxygenCriticalArgs args) => OnOxygenCritical?.Invoke(args);

        // ==================== CLOAK/TELEPORTER EVENTS ====================

        public static event Action<CloakActivatedArgs> OnCloakActivated;
        public static event Action<CloakDeactivatedArgs> OnCloakDeactivated;
        public static event Action<CrewTeleportedArgs> OnCrewTeleported;
        public static event Action<HackDeployedArgs> OnHackDeployed;
        public static event Action<HackStartedArgs> OnHackStarted;
        public static event Action<HackEndedArgs> OnHackEnded;
        public static event Action<MindControlStartedArgs> OnMindControlStarted;
        public static event Action<MindControlEndedArgs> OnMindControlEnded;

        public static void TriggerCloakActivated(CloakActivatedArgs args) => OnCloakActivated?.Invoke(args);
        public static void TriggerCloakDeactivated(CloakDeactivatedArgs args) => OnCloakDeactivated?.Invoke(args);
        public static void TriggerCrewTeleported(CrewTeleportedArgs args) => OnCrewTeleported?.Invoke(args);
        public static void TriggerHackDeployed(HackDeployedArgs args) => OnHackDeployed?.Invoke(args);
        public static void TriggerHackStarted(HackStartedArgs args) => OnHackStarted?.Invoke(args);
        public static void TriggerHackEnded(HackEndedArgs args) => OnHackEnded?.Invoke(args);
        public static void TriggerMindControlStarted(MindControlStartedArgs args) => OnMindControlStarted?.Invoke(args);
        public static void TriggerMindControlEnded(MindControlEndedArgs args) => OnMindControlEnded?.Invoke(args);

        // ==================== CREW EVENTS ====================

        public static event Action<CrewMovedArgs> OnCrewMoved;
        public static event Action<CrewDiedArgs> OnCrewDied;
        public static event Action<CrewHiredArgs> OnCrewHired;
        public static event Action<CrewClonedArgs> OnCrewCloned;
        public static event Action<CrewSkillUpArgs> OnCrewSkillUp;
        public static event Action<RelationshipChangedArgs> OnRelationshipChanged;
        public static event Action<MoraleChangedArgs> OnMoraleChanged;

        public static void TriggerCrewMoved(CrewMovedArgs args) => OnCrewMoved?.Invoke(args);
        public static void TriggerCrewDied(CrewDiedArgs args) => OnCrewDied?.Invoke(args);
        public static void TriggerCrewHired(CrewHiredArgs args) => OnCrewHired?.Invoke(args);
        public static void TriggerCrewCloned(CrewClonedArgs args) => OnCrewCloned?.Invoke(args);
        public static void TriggerCrewSkillUp(CrewSkillUpArgs args) => OnCrewSkillUp?.Invoke(args);
        public static void TriggerRelationshipChanged(RelationshipChangedArgs args) => OnRelationshipChanged?.Invoke(args);
        public static void TriggerMoraleChanged(MoraleChangedArgs args) => OnMoraleChanged?.Invoke(args);

        // ==================== SECTOR/TRAVEL EVENTS ====================

        public static event Action<BeaconReachedArgs> OnBeaconReached;
        public static event Action<SectorChangedArgs> OnSectorChanged;
        public static event Action<PursuitAdvancedArgs> OnPursuitAdvanced;
        public static event Action<FTLJumpStartedArgs> OnFTLJumpStarted;
        public static event Action<JumpCompletedArgs> OnJumpCompleted;

        public static void TriggerBeaconReached(BeaconReachedArgs args) => OnBeaconReached?.Invoke(args);
        public static void TriggerSectorChanged(SectorChangedArgs args) => OnSectorChanged?.Invoke(args);
        public static void TriggerPursuitAdvanced(PursuitAdvancedArgs args) => OnPursuitAdvanced?.Invoke(args);
        public static void TriggerFTLJumpStarted(FTLJumpStartedArgs args) => OnFTLJumpStarted?.Invoke(args);
        public static void TriggerJumpCompleted(JumpCompletedArgs args) => OnJumpCompleted?.Invoke(args);

        // ==================== EVOLUTION EVENTS ====================

        public static event Action<MutationUnlockedArgs> OnMutationUnlocked;
        public static event Action<EvolutionPathChangedArgs> OnEvolutionPathChanged;
        public static event Action<MutationPointsChangedArgs> OnMutationPointsChanged;
        public static event Action<ShipEvolvedArgs> OnShipEvolved;

        public static void TriggerMutationUnlocked(MutationUnlockedArgs args) => OnMutationUnlocked?.Invoke(args);
        public static void TriggerEvolutionPathChanged(EvolutionPathChangedArgs args) => OnEvolutionPathChanged?.Invoke(args);
        public static void TriggerMutationPointsChanged(MutationPointsChangedArgs args) => OnMutationPointsChanged?.Invoke(args);
        public static void TriggerShipEvolved(ShipEvolvedArgs args) => OnShipEvolved?.Invoke(args);

        // ==================== RESOURCE EVENTS ====================

        public static event Action<ScrapChangedArgs> OnScrapChanged;
        public static event Action<FuelChangedArgs> OnFuelChanged;
        public static event Action<ResourcesChangedArgs> OnResourcesChanged;

        public static void TriggerScrapChanged(ScrapChangedArgs args) => OnScrapChanged?.Invoke(args);
        public static void TriggerFuelChanged(FuelChangedArgs args) => OnFuelChanged?.Invoke(args);
        public static void TriggerResourcesChanged(ResourcesChangedArgs args) => OnResourcesChanged?.Invoke(args);

        // ==================== EVENT/SHOP EVENTS ====================

        public static event Action<EventStartedArgs> OnEventStarted;
        public static event Action<EventEndedArgs> OnEventEnded;
        public static event Action<EventResultArgs> OnEventResult;
        public static event Action<ShopOpenedArgs> OnShopOpened;
        public static event Action<ItemPurchasedArgs> OnItemPurchased;
        public static event Action<DraftOptionsArgs> OnDraftOptionsPresented;
        public static event Action<DraftChoiceMadeArgs> OnDraftChoiceMade;

        public static void TriggerEventStarted(EventStartedArgs args) => OnEventStarted?.Invoke(args);
        public static void TriggerEventEnded(EventEndedArgs args) => OnEventEnded?.Invoke(args);
        public static void TriggerEventResult(EventResultArgs args) => OnEventResult?.Invoke(args);
        public static void TriggerShopOpened(ShopOpenedArgs args) => OnShopOpened?.Invoke(args);
        public static void TriggerItemPurchased(ItemPurchasedArgs args) => OnItemPurchased?.Invoke(args);
        public static void TriggerDraftOptionsPresented(DraftOptionsArgs args) => OnDraftOptionsPresented?.Invoke(args);
        public static void TriggerDraftChoiceMade(DraftChoiceMadeArgs args) => OnDraftChoiceMade?.Invoke(args);

        // Additional draft events
        public static event Action<DraftStartArgs> OnDraftStart;
        public static event Action<DraftCompleteArgs> OnDraftComplete;
        public static event Action<EventChoiceMadeArgs> OnEventChoiceMade;
        public static event Action<WeaponFiredArgs> OnWeaponFired;

        public static void TriggerDraftStart(DraftStartArgs args) => OnDraftStart?.Invoke(args);
        public static void TriggerDraftComplete(DraftCompleteArgs args) => OnDraftComplete?.Invoke(args);
        public static void TriggerEventChoiceMade(EventChoiceMadeArgs args) => OnEventChoiceMade?.Invoke(args);
        public static void TriggerWeaponFired(WeaponFiredArgs args) => OnWeaponFired?.Invoke(args);

        // ==================== GAMEPLAY CONTROLLER EVENTS ====================
        // These events are used by GameplayController for gameplay flow

        public static event Action<BeaconSelectedArgs> OnBeaconSelected;
        public static event Action<CombatStartArgs> OnCombatStart;
        public static event Action<CombatEndArgs> OnCombatEnd;
        public static event Action<EventStartArgs> OnEventStart;
        public static event Action<EventCompleteArgs> OnEventComplete;
        public static event Action<ShopEnterArgs> OnShopEnter;
        public static event Action OnShopExit;
        public static event Action<PlayerDefeatedArgs> OnPlayerDefeated;
        public static event Action<SectorCompleteArgs> OnSectorComplete;

        public static void TriggerBeaconSelected(BeaconSelectedArgs args) => OnBeaconSelected?.Invoke(args);
        public static void TriggerCombatStart(CombatStartArgs args) => OnCombatStart?.Invoke(args);
        public static void TriggerCombatEnd(CombatEndArgs args) => OnCombatEnd?.Invoke(args);
        public static void TriggerEventStart(EventStartArgs args) => OnEventStart?.Invoke(args);
        public static void TriggerEventComplete(EventCompleteArgs args) => OnEventComplete?.Invoke(args);
        public static void TriggerShopEnter(ShopEnterArgs args) => OnShopEnter?.Invoke(args);
        public static void TriggerShopExit() => OnShopExit?.Invoke();
        public static void TriggerPlayerDefeated(PlayerDefeatedArgs args) => OnPlayerDefeated?.Invoke(args);
        public static void TriggerSectorComplete(SectorCompleteArgs args) => OnSectorComplete?.Invoke(args);

        // ==================== CLEAR ALL (for scene transitions) ====================

        public static void ClearAllListeners()
        {
            OnGameStarted = null;
            OnGameInitialized = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameOver = null;
            OnGameEnding = null;
            OnPauseToggle = null;
            OnCombatStarted = null;
            OnCombatEnded = null;
            OnDamageDealt = null;
            OnProjectileFired = null;
            OnProjectileHit = null;
            OnProjectileMissed = null;
            OnHullDamaged = null;
            OnHullChanged = null;
            OnBossPhaseChanged = null;
            OnBossAttack = null;
            OnBossDefeated = null;
            OnSystemDamaged = null;
            OnSystemRepaired = null;
            OnSystemDestroyed = null;
            OnPowerChanged = null;
            OnShieldChanged = null;
            OnRoomBreach = null;
            OnRoomFire = null;
            OnOxygenCritical = null;
            OnCloakActivated = null;
            OnCloakDeactivated = null;
            OnCrewTeleported = null;
            OnHackDeployed = null;
            OnHackStarted = null;
            OnHackEnded = null;
            OnMindControlStarted = null;
            OnMindControlEnded = null;
            OnCrewMoved = null;
            OnCrewDied = null;
            OnCrewHired = null;
            OnCrewCloned = null;
            OnCrewSkillUp = null;
            OnRelationshipChanged = null;
            OnMoraleChanged = null;
            OnBeaconReached = null;
            OnSectorChanged = null;
            OnPursuitAdvanced = null;
            OnFTLJumpStarted = null;
            OnJumpCompleted = null;
            OnMutationUnlocked = null;
            OnEvolutionPathChanged = null;
            OnMutationPointsChanged = null;
            OnShipEvolved = null;
            OnScrapChanged = null;
            OnFuelChanged = null;
            OnResourcesChanged = null;
            OnEventStarted = null;
            OnEventEnded = null;
            OnEventResult = null;
            OnShopOpened = null;
            OnItemPurchased = null;
            OnDraftOptionsPresented = null;
            OnDraftChoiceMade = null;
            OnDraftStart = null;
            OnDraftComplete = null;
            OnEventChoiceMade = null;
            OnWeaponFired = null;
            OnBeaconSelected = null;
            OnCombatStart = null;
            OnCombatEnd = null;
            OnEventStart = null;
            OnEventComplete = null;
            OnShopEnter = null;
            OnShopExit = null;
            OnPlayerDefeated = null;
            OnSectorComplete = null;
        }
    }

    // ==================== EVENT ARGUMENT STRUCTS ====================

    public struct GameOverArgs
    {
        public bool victory;
        public string reason;
        public int finalScore;
    }

    public struct GameEndingArgs
    {
        public EndingType endingType;
        public string endingTitle;
        public string endingDescription;
    }

    public enum EndingType { PredatorVictory, PhantomVictory, HeraldVictory, Defeat }

    public struct CombatStartedArgs
    {
        public ShipController playerShip;
        public ShipController enemyShip;
        public bool isAmbush;
    }

    public struct CombatEndedArgs
    {
        public bool playerWon;
        public int scrapReward;
        public int mutationPointsReward;
    }

    public struct DamageDealtArgs
    {
        public ShipController target;
        public int damage;
        public DamageType type;
    }

    public struct ProjectileFiredArgs
    {
        public Weapon weapon;
        public Room targetRoom;
    }

    public struct ProjectileHitArgs
    {
        public Room targetRoom;
        public DamageInfo damage;
    }

    public struct ProjectileMissedArgs
    {
        public ShipController targetShip;
        public int evasionRoll;
    }

    public struct HullDamagedArgs
    {
        public ShipController ship;
        public int damage;
        public int remainingHull;
    }

    public struct HullChangedArgs
    {
        public int currentHull;
        public int maxHull;
        public int change;
    }

    public struct BossPhaseChangedArgs
    {
        public string bossName;
        public int phase;
        public int maxPhases;
    }

    public struct BossAttackArgs
    {
        public string attackName;
        public int damage;
    }

    public struct BossDefeatedArgs
    {
        public string bossName;
        public EvolutionPath evolutionPath;
    }

    public struct SystemDamagedArgs
    {
        public ShipSystem system;
        public int damageAmount;
        public int remainingHealth;
    }

    public struct SystemRepairedArgs
    {
        public ShipSystem system;
        public int repairAmount;
        public int currentHealth;
    }

    public struct SystemDestroyedArgs
    {
        public ShipSystem system;
        public bool causedHullDamage;
    }

    public struct PowerChangedArgs
    {
        public int totalPower;
        public int usedPower;
        public int availablePower;
    }

    public struct ShieldChangedArgs
    {
        public int currentLayers;
        public int previousLayers;
        public int maxLayers;
        public float rechargeProgress;

        // Alias for compatibility
        public int newLayers => currentLayers;
    }

    public struct RoomBreachArgs
    {
        public Room room;
        public int breachLevel;
    }

    public struct RoomFireArgs
    {
        public Room room;
        public int fireLevel;
    }

    public struct OxygenCriticalArgs
    {
        public ShipController ship;
        public float oxygenLevel;
    }

    public struct CloakActivatedArgs
    {
        public ShipController ship;
        public float duration;
    }

    public struct CloakDeactivatedArgs
    {
        public ShipController ship;
    }

    public struct CrewTeleportedArgs
    {
        public ShipController fromShip;
        public ShipController toShip;
        public Room targetRoom;
        public int crewCount;
    }

    public struct HackDeployedArgs
    {
        public ShipController sourceShip;
        public ShipController targetShip;
        public SystemType targetSystem;
    }

    public struct HackStartedArgs
    {
        public SystemType targetSystem;
        public float duration;
    }

    public struct HackEndedArgs
    {
        public SystemType targetSystem;
    }

    public struct MindControlStartedArgs
    {
        public CrewMember controlledCrew;
        public float duration;
    }

    public struct MindControlEndedArgs
    {
        public CrewMember controlledCrew;
    }

    public struct CrewMovedArgs
    {
        public CrewMember crew;
        public Room fromRoom;
        public Room toRoom;
    }

    public struct CrewDiedArgs
    {
        public CrewMember crew;
        public DamageType causeOfDeath;
        public Room deathRoom;
    }

    public struct CrewHiredArgs
    {
        public CrewMember crew;
        public int cost;
    }

    public struct CrewClonedArgs
    {
        public CrewMember crew;
        public int skillLoss;
    }

    public struct CrewSkillUpArgs
    {
        public CrewMember crew;
        public SkillType skill;
        public int newLevel;
    }

    public struct RelationshipChangedArgs
    {
        public CrewMember crewA;
        public CrewMember crewB;
        public int newRelationshipLevel;
        public BondLevel bondLevel;
    }

    public struct MoraleChangedArgs
    {
        public int newMorale;
        public int change;
        public string reason;
    }

    public struct BeaconReachedArgs
    {
        public Beacon beacon;
        public BeaconType type;
    }

    public struct SectorChangedArgs
    {
        public int newSector;
        public SectorType sectorType;
    }

    public struct PursuitAdvancedArgs
    {
        public int pursuitLevel;
        public int beaconsUntilCaught;
    }

    public struct FTLJumpStartedArgs
    {
        public Beacon fromBeacon;
        public Beacon toBeacon;
        public float jumpDuration;
    }

    public struct JumpCompletedArgs
    {
        public Beacon arrivedAt;
        public int fuelUsed;
    }

    public struct MutationUnlockedArgs
    {
        public Mutation mutation;
        public EvolutionPath path;
        public int tier;
    }

    public struct EvolutionPathChangedArgs
    {
        public EvolutionPath newDominantPath;
        public int pathLevel;
    }

    public struct MutationPointsChangedArgs
    {
        public int currentPoints;
        public int change;
        public string source;
    }

    public struct ShipEvolvedArgs
    {
        public int newVisualStage;
        public EvolutionPath dominantPath;
    }

    public struct ScrapChangedArgs
    {
        public int newAmount;
        public int change;
    }

    public struct FuelChangedArgs
    {
        public int newAmount;
        public int change;
    }

    public struct ResourcesChangedArgs
    {
        public ResourceType resource;
        public int amount;
        public int newTotal;
    }

    public enum ResourceType { Scrap, Fuel, Missiles, DroneParts, MutationPoints }

    public struct EventStartedArgs
    {
        public string eventId;
        public string title;
    }

    public struct EventEndedArgs
    {
        public string eventId;
        public OutcomeType outcomeType;
    }

    public struct EventChoiceMadeArgs
    {
        public string eventId;
        public int choiceIndex;
        public string choiceText;
    }

    public struct EventResultArgs
    {
        public string text;
    }

    public struct ShopOpenedArgs
    {
        public int itemCount;
    }

    public struct ItemPurchasedArgs
    {
        public string itemName;
        public int price;
        public string itemType;
    }

    public struct DraftOptionsArgs
    {
        public RewardOption[] options;
        public RewardContext context;
    }

    public struct DraftChoiceMadeArgs
    {
        public RewardOption chosen;
        public int choiceIndex;
    }

    // ==================== GAMEPLAY CONTROLLER ARG STRUCTS ====================

    public struct BeaconSelectedArgs
    {
        public string BeaconId;
    }

    public struct CombatStartArgs
    {
        public ShipController PlayerShip;
        public ShipController EnemyShip;
        public bool IsAmbush;
    }

    public struct CombatEndArgs
    {
        public bool PlayerWon;
        public int ScrapReward;
        public int MutationPointsReward;
    }

    public struct EventStartArgs
    {
        public string EventId;
        public string EventTitle;
    }

    public struct EventCompleteArgs
    {
        public string EventId;
        public OutcomeType Outcome;
    }

    public struct ShopEnterArgs
    {
        public int ShopTier;
    }

    public struct PlayerDefeatedArgs
    {
        public string Reason;
        public int SectorReached;
    }

    public struct SectorCompleteArgs
    {
        public int SectorNumber;
        public SectorType SectorType;
    }

    public struct DraftStartArgs
    {
        public DraftContext context;
        public DraftOption[] options;
    }

    public struct DraftCompleteArgs
    {
        public DraftOption selectedOption;
        public int optionIndex;
    }

    public struct WeaponFiredArgs
    {
        public VoidBreaker.Combat.Weapon weapon;
        public VoidBreaker.Ship.ShipController source;
        public VoidBreaker.Ship.ShipController target;
        public VoidBreaker.Ship.Room targetRoom;
    }

    public enum DraftContext
    {
        PostCombat,
        Event,
        Shop,
        SectorStart
    }

    public class DraftOption
    {
        public DraftOptionType type;
        public string displayName;
        public string description;
        public UnityEngine.Sprite icon;
        public object data;
    }

    public enum DraftOptionType
    {
        Weapon,
        Augment,
        Crew,
        Scrap,
        Fuel,
        Drone,
        MutationPoints,
        Resources
    }
}
