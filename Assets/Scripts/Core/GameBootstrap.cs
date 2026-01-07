using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBreaker.Audio;
using VoidBreaker.Ship;
using VoidBreaker.Sector;
using VoidBreaker.Evolution;
using VoidBreaker.Events;
using VoidBreaker.UI;
using VoidBreaker.Data;
using VoidBreaker.Save;

namespace VoidBreaker.Core
{
    /// <summary>
    /// Main game bootstrap - initializes all core systems in correct order.
    /// This should be the first thing that runs in the game.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameplayScene = "Gameplay";

        [Header("Persistent Managers")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject saveManagerPrefab;
        [SerializeField] private GameObject inputManagerPrefab;

        [Header("Content Databases")]
        [SerializeField] private ContentDatabase contentDatabase;

        [Header("Settings")]
        [SerializeField] private bool skipIntro = false;
        [SerializeField] private float introDelay = 1f;

        private static GameBootstrap instance;
        private static bool isInitialized = false;

        public static ContentDatabase Content => instance?.contentDatabase;
        public static bool IsReady => isInitialized;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            StartCoroutine(InitializeSystems());
        }

        private IEnumerator InitializeSystems()
        {
            Debug.Log("[VoidBreaker] Starting game initialization...");

            // Phase 1: Core Systems
            yield return InitializeCoreSystems();

            // Phase 2: Load Settings
            yield return LoadSettings();

            // Phase 3: Initialize Audio
            yield return InitializeAudio();

            // Phase 4: Validate Content
            yield return ValidateContent();

            // Phase 5: Load Meta Progress
            yield return LoadMetaProgress();

            isInitialized = true;
            Debug.Log("[VoidBreaker] Game initialization complete!");

            // Proceed to main menu
            if (!skipIntro)
            {
                yield return new WaitForSeconds(introDelay);
            }

            GameEvents.TriggerGameInitialized();

            // Load main menu if not already there
            if (SceneManager.GetActiveScene().name != mainMenuScene)
            {
                SceneManager.LoadScene(mainMenuScene);
            }
        }

        private IEnumerator InitializeCoreSystems()
        {
            Debug.Log("[VoidBreaker] Initializing core systems...");

            // Create persistent manager instances
            if (gameManagerPrefab != null && GameManager.Instance == null)
            {
                Instantiate(gameManagerPrefab);
            }

            if (audioManagerPrefab != null && AudioManager.Instance == null)
            {
                Instantiate(audioManagerPrefab);
            }

            if (saveManagerPrefab != null && SaveManager.Instance == null)
            {
                Instantiate(saveManagerPrefab);
            }

            yield return null;
        }

        private IEnumerator LoadSettings()
        {
            Debug.Log("[VoidBreaker] Loading settings...");

            // Load player preferences
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(masterVolume);
                AudioManager.Instance.SetMusicVolume(musicVolume);
                AudioManager.Instance.SetSFXVolume(sfxVolume);
            }

            yield return null;
        }

        private IEnumerator InitializeAudio()
        {
            Debug.Log("[VoidBreaker] Initializing audio...");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicState(MusicState.MainMenu);
            }

            yield return null;
        }

        private IEnumerator ValidateContent()
        {
            Debug.Log("[VoidBreaker] Validating content database...");

            if (contentDatabase == null)
            {
                Debug.LogError("[VoidBreaker] No content database assigned!");
                yield break;
            }

            int errors = contentDatabase.ValidateAll();
            if (errors > 0)
            {
                Debug.LogWarning($"[VoidBreaker] Content validation found {errors} issues.");
            }

            yield return null;
        }

        private IEnumerator LoadMetaProgress()
        {
            Debug.Log("[VoidBreaker] Loading meta progress...");

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadMetaProgress();
            }

            yield return null;
        }

        /// <summary>
        /// Start a new game with the selected ship.
        /// </summary>
        public static void StartNewGame(ShipDefinition shipDefinition)
        {
            if (!isInitialized)
            {
                Debug.LogError("Cannot start game - bootstrap not complete!");
                return;
            }

            GameManager.Instance?.StartNewRun(shipDefinition);
            SceneManager.LoadScene(instance.gameplayScene);
        }

        /// <summary>
        /// Continue from a saved game.
        /// </summary>
        public static void ContinueGame()
        {
            if (!isInitialized)
            {
                Debug.LogError("Cannot continue game - bootstrap not complete!");
                return;
            }

            if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData)
            {
                SaveManager.Instance.LoadGame();
                SceneManager.LoadScene(instance.gameplayScene);
            }
        }

        /// <summary>
        /// Return to main menu from gameplay.
        /// </summary>
        public static void ReturnToMainMenu()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicState(MusicState.MainMenu);
            }

            SceneManager.LoadScene(instance.mainMenuScene);
        }

        /// <summary>
        /// Quit the game.
        /// </summary>
        public static void QuitGame()
        {
            // Save settings before quitting
            SaveSettings();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void SaveSettings()
        {
            if (AudioManager.Instance != null)
            {
                PlayerPrefs.SetFloat("MasterVolume", AudioManager.Instance.MasterVolume);
                PlayerPrefs.SetFloat("MusicVolume", AudioManager.Instance.MusicVolume);
                PlayerPrefs.SetFloat("SFXVolume", AudioManager.Instance.SFXVolume);
            }

            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Central database for all game content.
    /// </summary>
    [CreateAssetMenu(fileName = "ContentDatabase", menuName = "VoidBreaker/Content Database")]
    public class ContentDatabase : ScriptableObject
    {
        [Header("Ships")]
        public List<ShipDefinition> ships = new List<ShipDefinition>();

        [Header("Weapons")]
        public List<WeaponDefinition> weapons = new List<WeaponDefinition>();

        [Header("Augments")]
        public List<AugmentDefinition> augments = new List<AugmentDefinition>();

        [Header("Crew Races")]
        public List<RaceDefinition> crewRaces = new List<RaceDefinition>();

        [Header("Mutations")]
        public List<MutationDefinition> mutations = new List<MutationDefinition>();

        [Header("Events")]
        public List<EventDefinition> events = new List<EventDefinition>();

        [Header("Enemies")]
        public List<EnemyShipDefinition> enemies = new List<EnemyShipDefinition>();

        // Lookup caches
        private Dictionary<string, ShipDefinition> shipLookup;
        private Dictionary<string, WeaponDefinition> weaponLookup;
        private Dictionary<string, AugmentDefinition> augmentLookup;
        private Dictionary<string, RaceDefinition> raceLookup;
        private Dictionary<string, MutationDefinition> mutationLookup;
        private Dictionary<string, EventDefinition> eventLookup;

        public void BuildLookups()
        {
            shipLookup = new Dictionary<string, ShipDefinition>();
            foreach (var ship in ships)
            {
                if (ship != null && !string.IsNullOrEmpty(ship.shipId))
                    shipLookup[ship.shipId] = ship;
            }

            weaponLookup = new Dictionary<string, WeaponDefinition>();
            foreach (var weapon in weapons)
            {
                if (weapon != null && !string.IsNullOrEmpty(weapon.weaponId))
                    weaponLookup[weapon.weaponId] = weapon;
            }

            augmentLookup = new Dictionary<string, AugmentDefinition>();
            foreach (var augment in augments)
            {
                if (augment != null && !string.IsNullOrEmpty(augment.augmentId))
                    augmentLookup[augment.augmentId] = augment;
            }

            raceLookup = new Dictionary<string, RaceDefinition>();
            foreach (var race in crewRaces)
            {
                if (race != null && !string.IsNullOrEmpty(race.raceId))
                    raceLookup[race.raceId] = race;
            }

            mutationLookup = new Dictionary<string, MutationDefinition>();
            foreach (var mutation in mutations)
            {
                if (mutation != null && !string.IsNullOrEmpty(mutation.mutationId))
                    mutationLookup[mutation.mutationId] = mutation;
            }

            eventLookup = new Dictionary<string, EventDefinition>();
            foreach (var evt in events)
            {
                if (evt != null && !string.IsNullOrEmpty(evt.eventId))
                    eventLookup[evt.eventId] = evt;
            }
        }

        public ShipDefinition GetShip(string id) => shipLookup?.TryGetValue(id, out var s) == true ? s : null;
        public WeaponDefinition GetWeapon(string id) => weaponLookup?.TryGetValue(id, out var w) == true ? w : null;
        public AugmentDefinition GetAugment(string id) => augmentLookup?.TryGetValue(id, out var a) == true ? a : null;
        public RaceDefinition GetRace(string id) => raceLookup?.TryGetValue(id, out var r) == true ? r : null;
        public MutationDefinition GetMutation(string id) => mutationLookup?.TryGetValue(id, out var m) == true ? m : null;
        public EventDefinition GetEvent(string id) => eventLookup?.TryGetValue(id, out var e) == true ? e : null;

        public List<ShipDefinition> GetUnlockedShips()
        {
            var unlocked = new List<ShipDefinition>();
            foreach (var ship in ships)
            {
                if (ship.isUnlockedByDefault || IsShipUnlocked(ship.shipId))
                {
                    unlocked.Add(ship);
                }
            }
            return unlocked;
        }

        public List<MutationDefinition> GetMutationsByPath(EvolutionPath path)
        {
            var result = new List<MutationDefinition>();
            foreach (var mutation in mutations)
            {
                if (mutation.path == path)
                    result.Add(mutation);
            }
            return result;
        }

        public List<MutationDefinition> GetMutationsByTier(int tier)
        {
            var result = new List<MutationDefinition>();
            foreach (var mutation in mutations)
            {
                if (mutation.tier == tier)
                    result.Add(mutation);
            }
            return result;
        }

        private bool IsShipUnlocked(string shipId)
        {
            return PlayerPrefs.GetInt($"ship_unlocked_{shipId}", 0) == 1;
        }

        public int ValidateAll()
        {
            int errors = 0;

            foreach (var ship in ships)
            {
                if (ship == null) { errors++; continue; }
                if (string.IsNullOrEmpty(ship.shipId)) { Debug.LogWarning($"Ship {ship.name} has no ID"); errors++; }
            }

            foreach (var weapon in weapons)
            {
                if (weapon == null) { errors++; continue; }
                if (string.IsNullOrEmpty(weapon.weaponId)) { Debug.LogWarning($"Weapon {weapon.name} has no ID"); errors++; }
            }

            foreach (var mutation in mutations)
            {
                if (mutation == null) { errors++; continue; }
                if (string.IsNullOrEmpty(mutation.mutationId)) { Debug.LogWarning($"Mutation {mutation.name} has no ID"); errors++; }
            }

            BuildLookups();

            return errors;
        }
    }

    /// <summary>
    /// Definition for enemy ship encounters.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "VoidBreaker/Enemy Ship Definition")]
    public class EnemyShipDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId;
        public string enemyName;
        public EnemyFaction faction;

        [Header("Threat")]
        public int threatLevel = 1; // 1-5
        public int minSector = 1;
        public int maxSector = 8;

        [Header("Ship Stats")]
        public int hull = 20;
        public int shields = 2;
        public int evasion = 10;

        [Header("Loadout")]
        public List<WeaponDefinition> weapons = new List<WeaponDefinition>();
        public List<AugmentDefinition> augments = new List<AugmentDefinition>();
        public int crewCount = 3;

        [Header("Rewards")]
        public int scrapReward = 20;
        public int fuelReward = 1;
        public float weaponDropChance = 0.15f;
        public float augmentDropChance = 0.1f;

        [Header("Behavior")]
        public EnemyBehavior behavior = EnemyBehavior.Aggressive;
        public float fleeThreshold = 0.2f;
        public bool canSurrender = true;

        [Header("Visuals")]
        public Sprite shipSprite;
        public Color shipColor = Color.white;
    }

    public enum EnemyFaction
    {
        Pirate,
        Rebel,
        Federation,
        Mantis,
        Zoltan,
        Rock,
        Engi,
        Slug,
        Crystal,
        Lanius,
        VoidCorrupted
    }

    public enum EnemyBehavior
    {
        Aggressive,
        Defensive,
        Tactical,
        Cowardly,
        Suicidal,
        Hunter
    }
}
