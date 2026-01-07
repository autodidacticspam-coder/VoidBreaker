using System;
using System.IO;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Save
{
    /// <summary>
    /// MonoBehaviour singleton manager for saving and loading game data.
    /// Persists across scenes.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager instance;
        public static SaveManager Instance => instance;

        private const string SETTINGS_KEY = "VoidBreaker_Settings";
        private const string META_KEY = "VoidBreaker_Meta";
        private const string RUN_SAVE_FILE = "current_run.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, "Saves");

        public bool HasSaveData => HasRunSave();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSaveDirectory();
        }

        // ==================== SETTINGS ====================

        public void SaveSettings(GameSettings settings)
        {
            SaveSettingsStatic(settings);
        }

        public static void SaveSettingsStatic(GameSettings settings)
        {
            try
            {
                string json = JsonUtility.ToJson(settings);
                PlayerPrefs.SetString(SETTINGS_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveManager] Settings saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save settings: {e.Message}");
            }
        }

        public GameSettings LoadSettings()
        {
            return LoadSettingsStatic();
        }

        public static GameSettings LoadSettingsStatic()
        {
            try
            {
                if (PlayerPrefs.HasKey(SETTINGS_KEY))
                {
                    string json = PlayerPrefs.GetString(SETTINGS_KEY);
                    return JsonUtility.FromJson<GameSettings>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load settings: {e.Message}");
            }

            return null;
        }

        // ==================== META PROGRESSION ====================

        public void SaveMetaProgress()
        {
            var meta = GameManager.Instance?.MetaProgression;
            if (meta != null)
            {
                SaveMetaProgression(meta);
            }
        }

        public void SaveMetaProgression(MetaProgressionData meta)
        {
            SaveMetaProgressionStatic(meta);
        }

        public static void SaveMetaProgressionStatic(MetaProgressionData meta)
        {
            try
            {
                string json = JsonUtility.ToJson(meta);
                PlayerPrefs.SetString(META_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveManager] Meta progression saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save meta progression: {e.Message}");
            }
        }

        public void LoadMetaProgress()
        {
            // This method is called from GameBootstrap to load meta progress
            // The GameManager handles the actual meta data
            Debug.Log("[SaveManager] Loading meta progress...");
        }

        public MetaProgressionData LoadMetaProgression()
        {
            return LoadMetaProgressionStatic();
        }

        public static MetaProgressionData LoadMetaProgressionStatic()
        {
            try
            {
                if (PlayerPrefs.HasKey(META_KEY))
                {
                    string json = PlayerPrefs.GetString(META_KEY);
                    return JsonUtility.FromJson<MetaProgressionData>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load meta progression: {e.Message}");
            }

            return null;
        }

        // Static accessors for GameManager to use
        public static MetaProgressionData LoadMetaProgression_Static() => LoadMetaProgressionStatic();
        public static GameSettings LoadSettings_Static() => LoadSettingsStatic();
        public static void SaveSettings_Static(GameSettings settings) => SaveSettingsStatic(settings);
        public static void SaveMetaProgression_Static(MetaProgressionData meta) => SaveMetaProgressionStatic(meta);

        // ==================== RUN SAVES ====================

        public void SaveGame()
        {
            var runManager = GameManager.Instance?.CurrentRun;
            if (runManager != null)
            {
                SaveRun(runManager.GetSaveData());
            }
        }

        public void LoadGame()
        {
            var data = LoadRunSave();
            if (data != null)
            {
                GameManager.Instance?.ContinueRun();
            }
        }

        public void SaveRun(RunSaveData runData)
        {
            SaveRunStatic(runData);
        }

        public static void SaveRunStatic(RunSaveData runData)
        {
            try
            {
                EnsureSaveDirectoryStatic();

                string json = JsonUtility.ToJson(runData, true);
                string path = Path.Combine(SavePath, RUN_SAVE_FILE);
                File.WriteAllText(path, json);

                Debug.Log($"[SaveManager] Run saved to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save run: {e.Message}");
            }
        }

        public RunSaveData LoadRunSave()
        {
            return LoadRunSaveStatic();
        }

        public static RunSaveData LoadRunSaveStatic()
        {
            try
            {
                string path = Path.Combine(SavePath, RUN_SAVE_FILE);

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonUtility.FromJson<RunSaveData>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load run: {e.Message}");
            }

            return null;
        }

        public bool HasRunSave()
        {
            return HasRunSaveStatic();
        }

        public static bool HasRunSaveStatic()
        {
            string path = Path.Combine(SavePath, RUN_SAVE_FILE);
            return File.Exists(path);
        }

        public void DeleteRunSave()
        {
            DeleteRunSaveStatic();
        }

        public static void DeleteRunSaveStatic()
        {
            try
            {
                string path = Path.Combine(SavePath, RUN_SAVE_FILE);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log("[SaveManager] Run save deleted");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to delete run save: {e.Message}");
            }
        }

        // ==================== UTILITY ====================

        private void EnsureSaveDirectory()
        {
            EnsureSaveDirectoryStatic();
        }

        private static void EnsureSaveDirectoryStatic()
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }
        }

        /// <summary>
        /// Clears all save data (for debugging or reset).
        /// </summary>
        public void ClearAllData()
        {
            ClearAllDataStatic();
        }

        public static void ClearAllDataStatic()
        {
            PlayerPrefs.DeleteKey(SETTINGS_KEY);
            PlayerPrefs.DeleteKey(META_KEY);
            DeleteRunSaveStatic();

            PlayerPrefs.Save();
            Debug.Log("[SaveManager] All save data cleared");
        }

        /// <summary>
        /// Exports save data to a portable format.
        /// </summary>
        public string ExportSaveData()
        {
            var exportData = new SaveExportData
            {
                settings = LoadSettings(),
                meta = LoadMetaProgression(),
                currentRun = LoadRunSave(),
                exportTime = DateTime.Now.ToString("O")
            };

            return JsonUtility.ToJson(exportData, true);
        }

        /// <summary>
        /// Imports save data from a portable format.
        /// </summary>
        public bool ImportSaveData(string json)
        {
            try
            {
                var exportData = JsonUtility.FromJson<SaveExportData>(json);

                if (exportData.settings != null)
                    SaveSettings(exportData.settings);

                if (exportData.meta != null)
                    SaveMetaProgression(exportData.meta);

                if (exportData.currentRun != null)
                    SaveRun(exportData.currentRun);

                Debug.Log("[SaveManager] Save data imported successfully");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to import save data: {e.Message}");
                return false;
            }
        }
    }

    [Serializable]
    public class SaveExportData
    {
        public GameSettings settings;
        public MetaProgressionData meta;
        public RunSaveData currentRun;
        public string exportTime;
    }
}

// Static accessor class for backwards compatibility with code that uses SaveManager.LoadSettings() etc.
namespace VoidBreaker.Core
{
    /// <summary>
    /// Static accessor for SaveManager methods.
    /// For backwards compatibility with code using static SaveManager calls.
    /// </summary>
    public static class SaveManagerStatic
    {
        public static GameSettings LoadSettings() => VoidBreaker.Save.SaveManager.LoadSettingsStatic();
        public static void SaveSettings(GameSettings settings) => VoidBreaker.Save.SaveManager.SaveSettingsStatic(settings);
        public static MetaProgressionData LoadMetaProgression() => VoidBreaker.Save.SaveManager.LoadMetaProgressionStatic();
        public static void SaveMetaProgression(MetaProgressionData meta) => VoidBreaker.Save.SaveManager.SaveMetaProgressionStatic(meta);
        public static RunSaveData LoadRunSave() => VoidBreaker.Save.SaveManager.LoadRunSaveStatic();
        public static void SaveRun(RunSaveData data) => VoidBreaker.Save.SaveManager.SaveRunStatic(data);
        public static bool HasRunSave() => VoidBreaker.Save.SaveManager.HasRunSaveStatic();
        public static void DeleteRunSave() => VoidBreaker.Save.SaveManager.DeleteRunSaveStatic();
    }
}
