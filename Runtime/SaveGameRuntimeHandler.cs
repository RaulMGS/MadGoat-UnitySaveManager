using NorthernLights.Engine.Saves;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveGameRuntimeHandler : MonoBehaviour {
    #region Fields
    /// <summary>
    /// Enables or disables periodic autosaves
    /// </summary>
    [Tooltip("Enables or disables periodic autosaves")]
    public bool autosaveByTime;
    /// <summary>
    /// Minutes between autosaves periods
    /// </summary>
    [Tooltip("Minutes between autosaves periods")]
    public int autosaveMinutes = 10;
    #endregion

    #region Public API
    /// <summary>
    /// Triggers an autosave
    /// </summary>
    public void TriggerAutoSave() {
        try {
            SaveManager.instance.SaveFileCreate(SaveManager.instance.SaveDataPath, false, true);
        }
        catch (Exception ex) {
            Debug.LogException(ex);
        }
    }
    #endregion

    #region Implementation
    private void OnEnable() {
        StartCoroutine(AutoSaveRoutine());
        SaveManager.instance.SaveFileNotifyLoaded();
    }
    private void OnDisable() {
        StopCoroutine(AutoSaveRoutine());
    }
    private IEnumerator AutoSaveRoutine() {
        while (enabled) {
            // Waith fixed amount of minutes to autosave
            yield return new WaitForSeconds(60 * autosaveMinutes);
            TriggerAutoSave(); 
        }
    }
    #endregion
}
