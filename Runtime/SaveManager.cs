using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System;
using System.Linq; 

namespace MGFramework.Saves {
    [ExecuteAlways]
    public class SaveManager : MonoBehaviour {
        public const string MANAGER_VERSION = "1.0.0";
        public const string META_KEY = "meta";
        public const string META_KEY_PATH = "path";
        public const string META_KEY_TIMESTAMP = "timestamp";
        public const string META_KEY_SESSION = "session";
        public const string META_KEY_PRODUCT_VERSION = "version";
        public const string META_KEY_SAVE_VERSION = "sversion";
        public const string META_KEY_SAVE_TYPE = "stype";
        public const string META_KEY_SAVE_PROGRESS = "sprogress";
        public const string DATETIME_FORMAT = "yyyyMMdd_HHmmss";

        public static SaveManager instance; 

        public string localizationAutoSave;
        public string localizationManualSave; 

        public Dictionary<string, SaveContainer> currentSaveContents { get; private set; }
        public string SaveDataPath { get; private set; }

        private void OnEnable() {
            // First we want to check if we already have an instance
            if(instance != null) {
                Debug.LogWarning("Only one save manager can be active at once. Destroying this one");
                Destroy(gameObject);
            }

            // If we don't we assign this one
            else {
                // Create new save path
                currentSaveContents = null;
                SaveDataPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + Application.productName + "\\Saves";

                // Setup instance
                instance = this;
                DontDestroyOnLoad(this);
            }
        }

        /// <summary>
        /// Returns the seed of the current save's playthrough.
        /// </summary>
        /// <returns></returns>
        public int GetSeedOfPlaythrough() {
            if (currentSaveContents == null) return 0;

            var metaContainer = ReadContainer(META_KEY);
            if (metaContainer == null) return 0;
            var sessionId = metaContainer.GetEntry<string>(META_KEY_SESSION);


            int result;
            int.TryParse(sessionId.Replace("_", ""), out result);
            return result;
        }

        /// <summary>
        /// Updates a save container. If the container is not already in the save contents it will be added
        /// </summary>
        /// <param name="container"></param>
        public void UpdateContainer(SaveContainer container) {
            // Add updated instance
            if (!currentSaveContents.ContainsKey(container.ck))
                currentSaveContents.Add(container.ck, container);
            else currentSaveContents[container.ck] = container;
        }
        /// <summary>
        /// Gets a save container by key (for a specific object etc)
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public SaveContainer ReadContainer(string identifier) {
            if (!currentSaveContents.ContainsKey(identifier)) return null;
            else return currentSaveContents[identifier];
        }

        /// <summary>
        /// Creates a new save file at path
        /// </summary>
        /// <param name="saveFolderPath"></param>
        public string SaveFileCreate(string saveFolderPath, bool newSession, bool autosave = false) {
            // Create directory if missing
            Directory.CreateDirectory(saveFolderPath);
            var saveFilePath = saveFolderPath + "\\" + (autosave ? "auto" : "") + GetNextSaveStringFormatted();

            // Handle initialization if new session
            if (newSession) {
                currentSaveContents = new Dictionary<string, SaveContainer>();
                var metadata = new SaveContainer(META_KEY);

                // Manage meta entries
                metadata.SetEntry(META_KEY_PATH, saveFilePath);
                metadata.SetEntry(META_KEY_SESSION, DateTime.Now.ToString(DATETIME_FORMAT));

                // Save to file
                UpdateContainer(metadata);
            }

            SaveFileWrite(saveFilePath, autosave);
            return saveFilePath;
        }
        /// <summary>
        /// Returns the metadatas of all the save files in folder. Supports filtering by session id 
        /// </summary>
        /// <param name="saveFolderPath"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public List<SaveContainer> SaveFileGetAll(string saveFolderPath, string sessionId = "") {
            var metas = new List<SaveContainer>();
            if (!Directory.Exists(saveFolderPath)) return metas;

            foreach (var file in Directory.GetFiles(saveFolderPath, "*.sav")) {
                try {

                    var saveMeta = new SaveContainer(META_KEY);
                    FileInfo fi = new FileInfo(file);

                    // Override path in case we change location
                    saveMeta.SetEntry(META_KEY_PATH, file);
                    saveMeta.SetEntry(META_KEY_SAVE_TYPE, fi.Name.StartsWith("a") ? localizationAutoSave : localizationManualSave);
                    saveMeta.SetEntry(META_KEY_TIMESTAMP, fi.LastWriteTime.ToString(DATETIME_FORMAT));

                    // TODO: Add sessionId filtering here

                    metas.Add(saveMeta);
                }
                catch {
                    Debug.LogWarning("Found invalid save file at: " + file);
                }
            }
            return metas;
        }

        /// <summary>
        /// Notifies the objects and writes the current save instance at path
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveFileWrite(string filePath, bool autosave = false) {
            if (currentSaveContents == null) return;

            // notify saveable listeners of save hapening
            var listeners = GameObjectUtils.FindObjectsOfType<ISaveable>(false);
            for (int i = 0; i < listeners.Count; i++) {
                try {
                    listeners[i].OnGameSaving();
                }
                catch (Exception ex) { Debug.LogException(ex); }
            }

            // Update save metadata - todo add more entries
            var metadata = ReadContainer(META_KEY);
            metadata.SetEntry(META_KEY_PATH, filePath);
            metadata.SetEntry(META_KEY_PRODUCT_VERSION, Application.version);   // update product version
            metadata.SetEntry(META_KEY_SAVE_VERSION, MANAGER_VERSION);          // update save version
            metadata.SetEntry(META_KEY_TIMESTAMP, DateTime.Now.ToString(DATETIME_FORMAT));          // update save time
            metadata.SetEntry(META_KEY_SAVE_TYPE, autosave ? localizationAutoSave : localizationManualSave);

            UpdateContainer(metadata);

            // serialize and encrypt save data  
            var serializedData = Serialize(currentSaveContents);

            // Write to file
            File.WriteAllText(filePath, serializedData);

            // Keep only latest 5 autosaves
            if (autosave) {
                var directory = new DirectoryInfo(SaveDataPath);
                var files = directory.GetFiles("autoSave*").OrderByDescending(f => f.LastWriteTime).ToArray();
                for (int i = 0; i < files.Length; i++) {
                    try {
                        if (i >= 5) File.Delete(files[i].FullName);
                    }
                    catch (Exception ex) {
                        Debug.LogException(ex);
                    }
                }
            }
        }
        /// <summary>
        /// Loads a given save file into current save instance and notifies objects
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveFileLoad(string filePath) {
            var data = File.ReadAllText(filePath);
            try {
                currentSaveContents = Deserialize(data);
            }
            catch {
                currentSaveContents = null;
            }

            SaveFileNotifyLoaded();
        }
        /// <summary>
        /// Notifies all saveable components of a savefile being loaded
        /// </summary>
        public void SaveFileNotifyLoaded() {
            if (currentSaveContents == null) return;

            // notify saveable listeners of load
            var listeners = GameObjectUtils.FindObjectsOfType<ISaveable>(false);
            for (int i = 0; i < listeners.Count; i++) {
                try {
                    listeners[i].OnGameLoaded();
                }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
        /// <summary>
        /// Deletes a save file given by path
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveFileDelete(string filePath) {
            File.Delete(filePath);
        }
        /// <summary>
        /// Deletes all save files at base path that contain the given session identifier
        /// </summary>
        /// <param name="sessionId"></param>
        public void SaveFileDeleteBySession(string sessionId) {
            foreach (var save in SaveFileGetAll(SaveDataPath, sessionId)) {
                SaveFileDelete(save.GetEntry<string>("filePath"));
            }
        }

        private string Serialize(object data) {
            return JsonConvert.SerializeObject(data, Formatting.None);
        }
        private Dictionary<string, SaveContainer> Deserialize(string data) {
            return JsonConvert.DeserializeObject<Dictionary<string, SaveContainer>>(data);
        }

        private string GetNextSaveStringFormatted() {
            return "Save_" + "_" + DateTime.Now.ToString(DATETIME_FORMAT) + ".sav";
        }
    }
}