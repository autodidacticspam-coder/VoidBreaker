# VOIDBREAKER - System Architecture
## Version 1.0 | Lead Architect: The Structurer

---

## ARCHITECTURAL PRINCIPLES

1. **Composition Over Inheritance** - Favor component-based design
2. **Dependency Injection** - Systems receive dependencies, don't create them
3. **Data-Driven Design** - ScriptableObjects for game data
4. **Event-Driven Communication** - Systems communicate via events, not direct references
5. **Single Responsibility** - Each class does one thing well
6. **Testability** - Core logic separated from Unity-specific code

---

## HIGH-LEVEL SYSTEM DIAGRAM

```
┌─────────────────────────────────────────────────────────────────┐
│                        GAME MANAGER                             │
│  (Singleton - Manages game state, scene transitions)            │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│  RUN MANAGER  │    │ SAVE MANAGER  │    │ AUDIO MANAGER │
│  (Run state)  │    │ (Persistence) │    │   (Sound)     │
└───────────────┘    └───────────────┘    └───────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────────┐
│                         RUN STATE                               │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────────────┐   │
│  │  SHIP   │  │  CREW   │  │ SECTOR  │  │   EVOLUTION     │   │
│  │ MANAGER │  │ MANAGER │  │ MANAGER │  │    MANAGER      │   │
│  └─────────┘  └─────────┘  └─────────┘  └─────────────────┘   │
│       │            │            │               │              │
│       ▼            ▼            ▼               ▼              │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────────────┐   │
│  │ COMBAT  │  │RELATIONS│  │  EVENT  │  │   MUTATION      │   │
│  │ SYSTEM  │  │ SYSTEM  │  │ SYSTEM  │  │    SYSTEM       │   │
│  └─────────┘  └─────────┘  └─────────┘  └─────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## FOLDER STRUCTURE

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── RunManager.cs
│   │   ├── SaveManager.cs
│   │   ├── GameEvents.cs
│   │   └── Interfaces/
│   │       ├── ISystem.cs
│   │       ├── IDamageable.cs
│   │       ├── ITargetable.cs
│   │       └── ISaveable.cs
│   │
│   ├── Ship/
│   │   ├── ShipController.cs
│   │   ├── ShipSystem.cs
│   │   ├── PowerManager.cs
│   │   ├── RoomManager.cs
│   │   ├── Systems/
│   │   │   ├── ReactorSystem.cs
│   │   │   ├── EngineSystem.cs
│   │   │   ├── ShieldSystem.cs
│   │   │   ├── WeaponSystem.cs
│   │   │   ├── PilotingSystem.cs
│   │   │   ├── SensorSystem.cs
│   │   │   ├── DoorSystem.cs
│   │   │   ├── MedbaySystem.cs
│   │   │   └── OxygenSystem.cs
│   │   └── Room.cs
│   │
│   ├── Crew/
│   │   ├── CrewManager.cs
│   │   ├── CrewMember.cs
│   │   ├── CrewAI.cs
│   │   ├── CrewSkills.cs
│   │   ├── RelationshipSystem.cs
│   │   └── MoraleSystem.cs
│   │
│   ├── Combat/
│   │   ├── CombatManager.cs
│   │   ├── CombatState.cs
│   │   ├── Weapons/
│   │   │   ├── Weapon.cs
│   │   │   ├── LaserWeapon.cs
│   │   │   ├── MissileWeapon.cs
│   │   │   ├── BeamWeapon.cs
│   │   │   └── IonWeapon.cs
│   │   ├── Projectile.cs
│   │   ├── DamageCalculator.cs
│   │   └── EvasionCalculator.cs
│   │
│   ├── Sector/
│   │   ├── SectorManager.cs
│   │   ├── SectorGenerator.cs
│   │   ├── Beacon.cs
│   │   ├── BeaconTypes.cs
│   │   └── PursuitManager.cs
│   │
│   ├── Events/
│   │   ├── EventManager.cs
│   │   ├── EventParser.cs
│   │   ├── EventChoice.cs
│   │   └── RewardDrafter.cs
│   │
│   ├── Evolution/
│   │   ├── EvolutionManager.cs
│   │   ├── MutationTree.cs
│   │   ├── Mutation.cs
│   │   ├── EvolutionPath.cs
│   │   └── ShipVisualEvolver.cs
│   │
│   ├── UI/
│   │   ├── UIManager.cs
│   │   ├── Screens/
│   │   │   ├── TitleScreen.cs
│   │   │   ├── ShipSelectScreen.cs
│   │   │   ├── SectorMapScreen.cs
│   │   │   ├── CombatScreen.cs
│   │   │   └── EventScreen.cs
│   │   ├── HUD/
│   │   │   ├── HullBar.cs
│   │   │   ├── PowerBar.cs
│   │   │   └── WeaponSlots.cs
│   │   └── Components/
│   │       ├── DraftPanel.cs
│   │       └── TooltipSystem.cs
│   │
│   ├── Audio/
│   │   ├── AudioManager.cs
│   │   └── MusicController.cs
│   │
│   └── Save/
│       ├── SaveData.cs
│       ├── RunSaveData.cs
│       └── MetaProgressionData.cs
│
├── ScriptableObjects/
│   ├── Ships/
│   │   └── ShipDefinition.cs
│   ├── Crew/
│   │   └── RaceDefinition.cs
│   ├── Weapons/
│   │   └── WeaponDefinition.cs
│   ├── Events/
│   │   └── EventDefinition.cs
│   └── Mutations/
│       └── MutationDefinition.cs
│
└── Prefabs/
    ├── Ship/
    ├── Crew/
    ├── Weapons/
    ├── UI/
    └── Effects/
```

---

## CORE INTERFACES

### ISystem.cs
```csharp
public interface ISystem
{
    string SystemName { get; }
    int MaxLevel { get; }
    int CurrentLevel { get; }
    int PowerRequired { get; }
    int PowerAllocated { get; set; }
    bool IsOperational { get; }
    bool IsManned { get; }

    void Initialize();
    void OnPowerChanged(int newPower);
    void TakeDamage(int amount);
    void Repair(int amount);
    void OnCrewManned(CrewMember crew);
    void OnCrewLeft();
}
```

### IDamageable.cs
```csharp
public interface IDamageable
{
    int MaxHealth { get; }
    int CurrentHealth { get; }
    bool IsDestroyed { get; }

    void TakeDamage(DamageInfo damage);
    void Heal(int amount);
    event System.Action<DamageInfo> OnDamaged;
    event System.Action OnDestroyed;
}
```

### ITargetable.cs
```csharp
public interface ITargetable
{
    Vector2 Position { get; }
    TargetType TargetType { get; }
    bool CanBeTargeted { get; }
}
```

### ISaveable.cs
```csharp
public interface ISaveable
{
    string SaveKey { get; }
    object GetSaveData();
    void LoadSaveData(object data);
}
```

---

## EVENT SYSTEM (GameEvents.cs)

```csharp
// Central event bus for decoupled communication
public static class GameEvents
{
    // Combat Events
    public static event System.Action<CombatStartedArgs> OnCombatStarted;
    public static event System.Action<CombatEndedArgs> OnCombatEnded;
    public static event System.Action<DamageDealtArgs> OnDamageDealt;
    public static event System.Action<WeaponFiredArgs> OnWeaponFired;

    // Ship Events
    public static event System.Action<SystemDamagedArgs> OnSystemDamaged;
    public static event System.Action<PowerChangedArgs> OnPowerChanged;
    public static event System.Action<HullChangedArgs> OnHullChanged;

    // Crew Events
    public static event System.Action<CrewMovedArgs> OnCrewMoved;
    public static event System.Action<CrewDiedArgs> OnCrewDied;
    public static event System.Action<RelationshipChangedArgs> OnRelationshipChanged;
    public static event System.Action<MoraleChangedArgs> OnMoraleChanged;

    // Sector Events
    public static event System.Action<BeaconReachedArgs> OnBeaconReached;
    public static event System.Action<SectorChangedArgs> OnSectorChanged;
    public static event System.Action<PursuitAdvancedArgs> OnPursuitAdvanced;

    // Evolution Events
    public static event System.Action<MutationUnlockedArgs> OnMutationUnlocked;
    public static event System.Action<EvolutionPathChangedArgs> OnEvolutionPathChanged;
    public static event System.Action<MutationPointsChangedArgs> OnMutationPointsChanged;

    // UI Events
    public static event System.Action<DraftOptionsArgs> OnDraftOptionsPresented;
    public static event System.Action<EventPresentedArgs> OnEventPresented;
}
```

---

## DATA SCHEMAS (ScriptableObjects)

### ShipDefinition.asset
```csharp
[CreateAssetMenu(fileName = "Ship", menuName = "VoidBreaker/Ship Definition")]
public class ShipDefinition : ScriptableObject
{
    public string shipName;
    public string description;
    public Sprite shipSprite;
    public Sprite[] evolutionSprites; // Per path, per tier

    public int baseHull;
    public int baseReactor;
    public int baseEvasion;

    public RoomLayout roomLayout;
    public SystemLoadout startingSystems;
    public WeaponDefinition[] startingWeapons;
    public RaceDefinition[] startingCrew;

    public EvolutionPath preferredPath;
    public float pathBonus; // Bonus MP for preferred path
}
```

### WeaponDefinition.asset
```csharp
[CreateAssetMenu(fileName = "Weapon", menuName = "VoidBreaker/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    public string weaponName;
    public WeaponType weaponType;
    public Sprite icon;

    public int powerRequired;
    public float chargeTime;
    public int damage;
    public int shieldPiercing;
    public int projectileCount;
    public float spread;

    public bool usesAmmo;
    public int ammoCost;

    public DamageType damageType;
    public StatusEffect[] appliedEffects;

    public int scrapValue;
    public Rarity rarity;
}
```

### MutationDefinition.asset
```csharp
[CreateAssetMenu(fileName = "Mutation", menuName = "VoidBreaker/Mutation Definition")]
public class MutationDefinition : ScriptableObject
{
    public string mutationName;
    public string description;
    public Sprite icon;

    public EvolutionPath path;
    public int tier; // 1-4
    public int cost; // Mutation points
    public MutationDefinition[] prerequisites;

    public StatModifier[] statModifiers;
    public AbilityUnlock[] unlockedAbilities;
    public VisualChange visualChange;
}
```

### EventDefinition.asset
```csharp
[CreateAssetMenu(fileName = "Event", menuName = "VoidBreaker/Event Definition")]
public class EventDefinition : ScriptableObject
{
    public string eventId;
    public string title;
    [TextArea(3, 10)]
    public string description;

    public SectorType[] validSectors;
    public BeaconType triggerBeacon;
    public EventRequirement[] requirements;

    public EventChoice[] choices;
    public bool isUnique; // Can only occur once per run
    public float weight; // Spawn probability
}
```

---

## STATE MANAGEMENT

### RunState.cs (Central Run Data)
```csharp
[System.Serializable]
public class RunState
{
    // Ship State
    public ShipState ship;
    public List<CrewState> crew;
    public List<WeaponState> weapons;
    public EvolutionState evolution;

    // Resources
    public int scrap;
    public int fuel;
    public int missiles;
    public int droneParts;

    // Progress
    public int currentSector;
    public int currentBeacon;
    public SectorMap sectorMap;
    public PursuitState pursuit;

    // Flags
    public HashSet<string> completedEvents;
    public HashSet<string> unlockedAchievements;

    // Stats
    public RunStatistics statistics;
}
```

---

## DEPENDENCY INJECTION PATTERN

```csharp
// Services are registered at startup and injected into systems
public class ServiceLocator
{
    private static Dictionary<System.Type, object> services = new();

    public static void Register<T>(T service) where T : class
    {
        services[typeof(T)] = service;
    }

    public static T Get<T>() where T : class
    {
        return services.TryGetValue(typeof(T), out var service)
            ? (T)service
            : null;
    }
}

// Example usage in systems:
public class CombatManager : MonoBehaviour
{
    private IDamageCalculator damageCalculator;
    private IEvasionCalculator evasionCalculator;
    private IAudioService audioService;

    private void Awake()
    {
        damageCalculator = ServiceLocator.Get<IDamageCalculator>();
        evasionCalculator = ServiceLocator.Get<IEvasionCalculator>();
        audioService = ServiceLocator.Get<IAudioService>();
    }
}
```

---

## DESIGN PATTERNS USED

| Pattern | Usage |
|---------|-------|
| **Singleton** | GameManager, AudioManager (one instance needed) |
| **Observer** | GameEvents system (decoupled communication) |
| **State** | Combat states, Game states |
| **Strategy** | Weapon behaviors, AI behaviors |
| **Factory** | Event generation, Enemy spawning |
| **Command** | Player actions (for undo/replay) |
| **Object Pool** | Projectiles, particles |
| **Flyweight** | ScriptableObject data (shared definitions) |

---

## SAVE SYSTEM ARCHITECTURE

```
┌─────────────────────────────────────────┐
│           SAVE MANAGER                  │
├─────────────────────────────────────────┤
│  SaveCurrentRun()                       │
│  LoadRun()                              │
│  SaveMetaProgression()                  │
│  LoadMetaProgression()                  │
└─────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│         SERIALIZATION                   │
│   (JSON via Unity's JsonUtility)        │
└─────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│         PERSISTENCE                     │
│  - PlayerPrefs (quick save)             │
│  - File system (full save)              │
│  - Cloud sync (future)                  │
└─────────────────────────────────────────┘
```

---

*Architecture Document v1.0*
*Lead Architect: The Structurer*
*"Clean code is not written by following a set of rules. Clean code is written by a programmer who cares."*
