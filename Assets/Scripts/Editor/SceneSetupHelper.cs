using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace VoidBreaker.Editor
{
    /// <summary>
    /// Editor tool to help set up game scenes with required components.
    /// </summary>
    public class SceneSetupHelper : EditorWindow
    {
        [MenuItem("VoidBreaker/Scene Setup Helper")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupHelper>("Scene Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VoidBreaker Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Create Scenes:", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Bootstrap Scene", GUILayout.Height(30)))
            {
                CreateBootstrapScene();
            }

            if (GUILayout.Button("Create Main Menu Scene", GUILayout.Height(30)))
            {
                CreateMainMenuScene();
            }

            if (GUILayout.Button("Create Gameplay Scene", GUILayout.Height(30)))
            {
                CreateGameplayScene();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Setup Current Scene:", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Core Managers"))
            {
                AddCoreManagers();
            }

            if (GUILayout.Button("Add UI Canvas"))
            {
                AddUICanvas();
            }

            if (GUILayout.Button("Add Combat Setup"))
            {
                AddCombatSetup();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create Prefabs:", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Manager Prefabs"))
            {
                CreateManagerPrefabs();
            }

            if (GUILayout.Button("Create UI Prefabs"))
            {
                CreateUIPrefabs();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Project Setup:", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Folder Structure"))
            {
                CreateFolderStructure();
            }

            if (GUILayout.Button("Setup Build Settings"))
            {
                SetupBuildSettings();
            }
        }

        // ==================== SCENE CREATION ====================

        private void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create Bootstrap object
            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<Core.GameBootstrap>();

            // Save scene
            string path = "Assets/Scenes/Bootstrap.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);

            Debug.Log("Created Bootstrap scene at: " + path);
        }

        private void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create UI Canvas
            CreateMainMenuUI();

            // Save scene
            string path = "Assets/Scenes/MainMenu.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);

            Debug.Log("Created Main Menu scene at: " + path);
        }

        private void CreateGameplayScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Add gameplay components
            AddCoreManagers();
            AddCombatSetup();
            AddUICanvas();

            // Save scene
            string path = "Assets/Scenes/Gameplay.unity";
            EnsureDirectoryExists(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);

            Debug.Log("Created Gameplay scene at: " + path);
        }

        // ==================== COMPONENT ADDITION ====================

        private void AddCoreManagers()
        {
            // Game Controller
            if (Object.FindObjectOfType<Core.GameplayController>() == null)
            {
                var controller = new GameObject("GameplayController");
                controller.AddComponent<Core.GameplayController>();
            }

            // Input Manager
            if (Object.FindObjectOfType<Core.InputManager>() == null)
            {
                var input = new GameObject("InputManager");
                input.AddComponent<Core.InputManager>();
            }

            // Draft Manager
            if (Object.FindObjectOfType<Core.DraftManager>() == null)
            {
                var draft = new GameObject("DraftManager");
                draft.AddComponent<Core.DraftManager>();
            }

            Debug.Log("Added core managers to scene");
        }

        private void AddUICanvas()
        {
            // Check if canvas exists
            var existingCanvas = Object.FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                Debug.Log("Canvas already exists in scene");
                return;
            }

            // Create canvas
            var canvasObj = new GameObject("UICanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Add UI Manager
            canvasObj.AddComponent<UI.UIManager>();

            // Create HUD
            var hud = new GameObject("HUD");
            hud.transform.SetParent(canvasObj.transform, false);
            hud.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
            var rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Debug.Log("Added UI Canvas to scene");
        }

        private void AddCombatSetup()
        {
            // Combat Manager
            if (Object.FindObjectOfType<Combat.CombatManager>() == null)
            {
                var combat = new GameObject("CombatManager");
                combat.AddComponent<Combat.CombatManager>();
            }

            // Player spawn point
            var playerSpawn = GameObject.Find("PlayerSpawnPoint");
            if (playerSpawn == null)
            {
                playerSpawn = new GameObject("PlayerSpawnPoint");
                playerSpawn.transform.position = new Vector3(-5, 0, 0);
            }

            // Enemy spawn point
            var enemySpawn = GameObject.Find("EnemySpawnPoint");
            if (enemySpawn == null)
            {
                enemySpawn = new GameObject("EnemySpawnPoint");
                enemySpawn.transform.position = new Vector3(5, 0, 0);
            }

            Debug.Log("Added combat setup to scene");
        }

        private void CreateMainMenuUI()
        {
            // Create canvas
            var canvasObj = new GameObject("MainMenuCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Add controller
            canvasObj.AddComponent<UI.MainMenuController>();

            // Create panels
            CreateUIPanel(canvasObj.transform, "MainPanel");
            CreateUIPanel(canvasObj.transform, "ShipSelectPanel");
            CreateUIPanel(canvasObj.transform, "SettingsPanel");
            CreateUIPanel(canvasObj.transform, "CreditsPanel");

            Debug.Log("Created Main Menu UI structure");
        }

        private void CreateUIPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        }

        // ==================== PREFAB CREATION ====================

        private void CreateManagerPrefabs()
        {
            string prefabPath = "Assets/Prefabs/Managers/";
            EnsureDirectoryExists(prefabPath);

            // Game Manager prefab
            CreatePrefab<Core.GameManager>(prefabPath, "GameManager");

            // Audio Manager prefab
            CreatePrefab<Audio.AudioManager>(prefabPath, "AudioManager");

            // Save Manager prefab
            CreatePrefab<Core.SaveManager>(prefabPath, "SaveManager");

            Debug.Log("Created manager prefabs in: " + prefabPath);
        }

        private void CreateUIPrefabs()
        {
            string prefabPath = "Assets/Prefabs/UI/";
            EnsureDirectoryExists(prefabPath);

            // Ship Card prefab
            var shipCard = new GameObject("ShipCard");
            shipCard.AddComponent<UnityEngine.UI.Image>();
            shipCard.AddComponent<UnityEngine.UI.Button>();
            PrefabUtility.SaveAsPrefabAsset(shipCard, prefabPath + "ShipCard.prefab");
            Object.DestroyImmediate(shipCard);

            // Draft Option Card prefab
            var draftCard = new GameObject("DraftOptionCard");
            draftCard.AddComponent<UnityEngine.UI.Image>();
            draftCard.AddComponent<UnityEngine.UI.Button>();
            PrefabUtility.SaveAsPrefabAsset(draftCard, prefabPath + "DraftOptionCard.prefab");
            Object.DestroyImmediate(draftCard);

            Debug.Log("Created UI prefabs in: " + prefabPath);
        }

        private void CreatePrefab<T>(string path, string name) where T : Component
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(go, path + name + ".prefab");
            Object.DestroyImmediate(go);
        }

        // ==================== PROJECT SETUP ====================

        private void CreateFolderStructure()
        {
            string[] folders = new string[]
            {
                "Assets/Scenes",
                "Assets/Prefabs",
                "Assets/Prefabs/Managers",
                "Assets/Prefabs/UI",
                "Assets/Prefabs/Ships",
                "Assets/Prefabs/Weapons",
                "Assets/Prefabs/Effects",
                "Assets/Data",
                "Assets/Data/Ships",
                "Assets/Data/Weapons",
                "Assets/Data/Augments",
                "Assets/Data/Crew",
                "Assets/Data/Mutations",
                "Assets/Data/Events",
                "Assets/Data/Enemies",
                "Assets/Art",
                "Assets/Art/Sprites",
                "Assets/Art/UI",
                "Assets/Art/Ships",
                "Assets/Audio",
                "Assets/Audio/Music",
                "Assets/Audio/SFX",
                "Assets/Fonts",
                "Assets/Materials"
            };

            foreach (string folder in folders)
            {
                EnsureDirectoryExists(folder);
            }

            AssetDatabase.Refresh();
            Debug.Log("Created folder structure");
        }

        private void SetupBuildSettings()
        {
            // Get all scene paths
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Gameplay.unity"
            };

            // Check which scenes exist
            var validScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            foreach (string path in scenePaths)
            {
                if (File.Exists(path))
                {
                    validScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = validScenes.ToArray();

            Debug.Log($"Added {validScenes.Count} scenes to build settings");
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    /// <summary>
    /// Quick access toolbar for common operations.
    /// </summary>
    public static class VoidBreakerToolbar
    {
        [MenuItem("VoidBreaker/Play from Bootstrap _F5")]
        private static void PlayFromBootstrap()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            // Open bootstrap scene and play
            string bootstrapPath = "Assets/Scenes/Bootstrap.unity";
            if (File.Exists(bootstrapPath))
            {
                EditorSceneManager.OpenScene(bootstrapPath);
                EditorApplication.isPlaying = true;
            }
            else
            {
                Debug.LogWarning("Bootstrap scene not found! Create it first.");
            }
        }

        [MenuItem("VoidBreaker/Open Main Menu Scene")]
        private static void OpenMainMenu()
        {
            string path = "Assets/Scenes/MainMenu.unity";
            if (File.Exists(path))
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        [MenuItem("VoidBreaker/Open Gameplay Scene")]
        private static void OpenGameplay()
        {
            string path = "Assets/Scenes/Gameplay.unity";
            if (File.Exists(path))
            {
                EditorSceneManager.OpenScene(path);
            }
        }
    }
}
