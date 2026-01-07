using UnityEngine;
using UnityEditor;
using System.IO;
using VoidBreaker.Data;

namespace VoidBreaker.Editor
{
    /// <summary>
    /// Wizard window for creating game content quickly.
    /// </summary>
    public class ContentCreationWizard : EditorWindow
    {
        private enum ContentType
        {
            Ship,
            Weapon,
            Crew,
            Mutation,
            Augment,
            Event
        }

        private ContentType selectedType = ContentType.Ship;
        private string contentName = "New Content";
        private string contentId = "new_content";

        // Ship fields
        private ShipClass shipClass = ShipClass.Fighter;
        private int hull = 30;
        private int shields = 2;
        private int power = 8;

        // Weapon fields
        private WeaponType weaponType = WeaponType.Laser;
        private int damage = 2;
        private float chargeTime = 10f;
        private WeaponRarity weaponRarity = WeaponRarity.Common;

        // Crew fields
        private CrewRace crewRace = CrewRace.Human;
        private int health = 100;
        private int combat = 50;
        private int repair = 50;

        // Mutation fields
        private EvolutionPath evolutionPath = EvolutionPath.Predator;
        private int tier = 1;
        private int cost = 1;

        private Vector2 scrollPos;

        [MenuItem("VoidBreaker/Content Creation Wizard")]
        public static void ShowWindow()
        {
            GetWindow<ContentCreationWizard>("Content Wizard");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VoidBreaker Content Creation", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            selectedType = (ContentType)EditorGUILayout.EnumPopup("Content Type", selectedType);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            contentName = EditorGUILayout.TextField("Name", contentName);
            contentId = EditorGUILayout.TextField("ID", contentId);

            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedType)
            {
                case ContentType.Ship:
                    DrawShipFields();
                    break;
                case ContentType.Weapon:
                    DrawWeaponFields();
                    break;
                case ContentType.Crew:
                    DrawCrewFields();
                    break;
                case ContentType.Mutation:
                    DrawMutationFields();
                    break;
                case ContentType.Augment:
                    DrawAugmentFields();
                    break;
                case ContentType.Event:
                    DrawEventFields();
                    break;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Content", GUILayout.Height(30)))
            {
                CreateContent();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Content Folder"))
            {
                OpenContentFolder();
            }
        }

        private void DrawShipFields()
        {
            EditorGUILayout.LabelField("Ship Properties", EditorStyles.boldLabel);
            shipClass = (ShipClass)EditorGUILayout.EnumPopup("Ship Class", shipClass);
            hull = EditorGUILayout.IntSlider("Max Hull", hull, 10, 50);
            shields = EditorGUILayout.IntSlider("Shield Layers", shields, 0, 8);
            power = EditorGUILayout.IntSlider("Reactor Power", power, 4, 30);
        }

        private void DrawWeaponFields()
        {
            EditorGUILayout.LabelField("Weapon Properties", EditorStyles.boldLabel);
            weaponType = (WeaponType)EditorGUILayout.EnumPopup("Weapon Type", weaponType);
            weaponRarity = (WeaponRarity)EditorGUILayout.EnumPopup("Rarity", weaponRarity);
            damage = EditorGUILayout.IntSlider("Damage", damage, 1, 10);
            chargeTime = EditorGUILayout.Slider("Charge Time", chargeTime, 1f, 30f);
        }

        private void DrawCrewFields()
        {
            EditorGUILayout.LabelField("Crew Properties", EditorStyles.boldLabel);
            crewRace = (CrewRace)EditorGUILayout.EnumPopup("Race", crewRace);
            health = EditorGUILayout.IntSlider("Health", health, 50, 200);
            combat = EditorGUILayout.IntSlider("Combat", combat, 0, 100);
            repair = EditorGUILayout.IntSlider("Repair", repair, 0, 100);
        }

        private void DrawMutationFields()
        {
            EditorGUILayout.LabelField("Mutation Properties", EditorStyles.boldLabel);
            evolutionPath = (EvolutionPath)EditorGUILayout.EnumPopup("Evolution Path", evolutionPath);
            tier = EditorGUILayout.IntSlider("Tier", tier, 1, 4);
            cost = EditorGUILayout.IntSlider("Cost", cost, 1, 10);
        }

        private void DrawAugmentFields()
        {
            EditorGUILayout.LabelField("Augment Properties", EditorStyles.boldLabel);
            // Similar pattern for augments
        }

        private void DrawEventFields()
        {
            EditorGUILayout.LabelField("Event Properties", EditorStyles.boldLabel);
            // Similar pattern for events
        }

        private void CreateContent()
        {
            string basePath = "Assets/Data/";

            switch (selectedType)
            {
                case ContentType.Ship:
                    CreateShipDefinition(basePath + "Ships/");
                    break;
                case ContentType.Weapon:
                    CreateWeaponDefinition(basePath + "Weapons/");
                    break;
                case ContentType.Crew:
                    CreateCrewDefinition(basePath + "Crew/");
                    break;
                case ContentType.Mutation:
                    CreateMutationDefinition(basePath + "Mutations/");
                    break;
                case ContentType.Augment:
                    CreateAugmentDefinition(basePath + "Augments/");
                    break;
                case ContentType.Event:
                    CreateEventDefinition(basePath + "Events/");
                    break;
            }

            AssetDatabase.Refresh();
        }

        private void CreateShipDefinition(string path)
        {
            EnsureDirectoryExists(path);

            ShipDefinition ship = ScriptableObject.CreateInstance<ShipDefinition>();
            ship.shipId = contentId;
            ship.shipName = contentName;
            ship.shipClass = shipClass;
            ship.maxHull = hull;
            ship.maxShieldLayers = shields;
            ship.reactorPower = power;
            ship.description = $"A {shipClass} class vessel.";

            AssetDatabase.CreateAsset(ship, path + contentId + ".asset");
            Selection.activeObject = ship;
            EditorUtility.FocusProjectWindow();

            Debug.Log($"Created ship: {contentName}");
        }

        private void CreateWeaponDefinition(string path)
        {
            EnsureDirectoryExists(path);

            WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.weaponId = contentId;
            weapon.weaponName = contentName;
            weapon.weaponType = weaponType;
            weapon.rarity = weaponRarity;
            weapon.damage = damage;
            weapon.chargeTime = chargeTime;
            weapon.description = $"A {weaponRarity} {weaponType} weapon.";

            AssetDatabase.CreateAsset(weapon, path + contentId + ".asset");
            Selection.activeObject = weapon;
            EditorUtility.FocusProjectWindow();

            Debug.Log($"Created weapon: {contentName}");
        }

        private void CreateCrewDefinition(string path)
        {
            EnsureDirectoryExists(path);

            RaceDefinition race = ScriptableObject.CreateInstance<RaceDefinition>();
            race.raceId = contentId;
            race.raceName = contentName;
            race.race = crewRace;
            race.baseHealth = health;
            race.baseCombat = combat;
            race.baseRepair = repair;

            AssetDatabase.CreateAsset(race, path + contentId + ".asset");
            Selection.activeObject = race;
            EditorUtility.FocusProjectWindow();

            Debug.Log($"Created crew race: {contentName}");
        }

        private void CreateMutationDefinition(string path)
        {
            EnsureDirectoryExists(path);

            MutationDefinition mutation = ScriptableObject.CreateInstance<MutationDefinition>();
            mutation.mutationId = contentId;
            mutation.mutationName = contentName;
            mutation.path = evolutionPath;
            mutation.tier = tier;
            mutation.cost = cost;
            mutation.description = $"A tier {tier} {evolutionPath} mutation.";

            AssetDatabase.CreateAsset(mutation, path + contentId + ".asset");
            Selection.activeObject = mutation;
            EditorUtility.FocusProjectWindow();

            Debug.Log($"Created mutation: {contentName}");
        }

        private void CreateAugmentDefinition(string path)
        {
            EnsureDirectoryExists(path);

            AugmentDefinition augment = ScriptableObject.CreateInstance<AugmentDefinition>();
            augment.augmentId = contentId;
            augment.augmentName = contentName;
            augment.description = "A ship augment.";

            AssetDatabase.CreateAsset(augment, path + contentId + ".asset");
            Selection.activeObject = augment;
            EditorUtility.FocusProjectWindow();

            Debug.Log($"Created augment: {contentName}");
        }

        private void CreateEventDefinition(string path)
        {
            EnsureDirectoryExists(path);

            EventDefinition evt = ScriptableObject.CreateInstance<EventDefinition>();
            evt.eventId = contentId;
            evt.eventTitle = contentName;
            evt.eventDescription = "Event description here.";

            AssetDatabase.CreateAsset(evt, path + contentId + ".asset");
            Selection.activeObject = evt;
            EditorUtility.FocusProjectWindow();

            Debug.Log($"Created event: {contentName}");
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                string[] parts = path.Split('/');
                string current = "";

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    string parent = current == "" ? parts[i] : current;
                    string child = parts[i + 1];

                    if (!string.IsNullOrEmpty(child) && !AssetDatabase.IsValidFolder(parent + "/" + child))
                    {
                        AssetDatabase.CreateFolder(parent, child);
                    }

                    current = parent + "/" + child;
                }
            }
        }

        private void OpenContentFolder()
        {
            string path = "Assets/Data/";
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            EditorUtility.RevealInFinder(path);
        }
    }

    /// <summary>
    /// Quick menu items for common operations.
    /// </summary>
    public static class VoidBreakerMenuItems
    {
        [MenuItem("VoidBreaker/Create/Ship Definition")]
        private static void CreateShip()
        {
            CreateAsset<ShipDefinition>("Assets/Data/Ships/NewShip.asset");
        }

        [MenuItem("VoidBreaker/Create/Weapon Definition")]
        private static void CreateWeapon()
        {
            CreateAsset<WeaponDefinition>("Assets/Data/Weapons/NewWeapon.asset");
        }

        [MenuItem("VoidBreaker/Create/Mutation Definition")]
        private static void CreateMutation()
        {
            CreateAsset<MutationDefinition>("Assets/Data/Mutations/NewMutation.asset");
        }

        [MenuItem("VoidBreaker/Create/Augment Definition")]
        private static void CreateAugment()
        {
            CreateAsset<AugmentDefinition>("Assets/Data/Augments/NewAugment.asset");
        }

        [MenuItem("VoidBreaker/Create/Event Definition")]
        private static void CreateEvent()
        {
            CreateAsset<EventDefinition>("Assets/Data/Events/NewEvent.asset");
        }

        [MenuItem("VoidBreaker/Create/Race Definition")]
        private static void CreateRace()
        {
            CreateAsset<RaceDefinition>("Assets/Data/Crew/NewRace.asset");
        }

        private static void CreateAsset<T>(string path) where T : ScriptableObject
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();

            Selection.activeObject = asset;
            EditorUtility.FocusProjectWindow();
        }

        [MenuItem("VoidBreaker/Open Documentation")]
        private static void OpenDocs()
        {
            Application.OpenURL("https://github.com/autodidacticspam-coder/VoidBreaker/wiki");
        }

        [MenuItem("VoidBreaker/Validate All Content")]
        private static void ValidateContent()
        {
            ValidateShips();
            ValidateWeapons();
            ValidateMutations();
            Debug.Log("Content validation complete!");
        }

        private static void ValidateShips()
        {
            string[] guids = AssetDatabase.FindAssets("t:ShipDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ShipDefinition ship = AssetDatabase.LoadAssetAtPath<ShipDefinition>(path);

                if (string.IsNullOrEmpty(ship.shipId))
                    Debug.LogWarning($"Ship at {path} has no ID!");
                if (ship.maxHull <= 0)
                    Debug.LogWarning($"Ship {ship.shipName} has invalid hull value!");
            }
        }

        private static void ValidateWeapons()
        {
            string[] guids = AssetDatabase.FindAssets("t:WeaponDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);

                if (string.IsNullOrEmpty(weapon.weaponId))
                    Debug.LogWarning($"Weapon at {path} has no ID!");
                if (weapon.damage <= 0)
                    Debug.LogWarning($"Weapon {weapon.weaponName} has invalid damage!");
            }
        }

        private static void ValidateMutations()
        {
            string[] guids = AssetDatabase.FindAssets("t:MutationDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MutationDefinition mutation = AssetDatabase.LoadAssetAtPath<MutationDefinition>(path);

                if (string.IsNullOrEmpty(mutation.mutationId))
                    Debug.LogWarning($"Mutation at {path} has no ID!");
                if (mutation.tier < 1 || mutation.tier > 4)
                    Debug.LogWarning($"Mutation {mutation.mutationName} has invalid tier!");
            }
        }
    }
}
