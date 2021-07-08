using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameObjectUtils {
    /// <summary>
    /// Finds all the objects of type T. Supports interfaces.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> FindObjectsOfType<T>(bool activeSceneOnly) {
        var result = new List<T>();

        // TODO: Workaround by only searching for listeners in active scene. This can cause trouble if 
        // ----  there are interactables expecting time events in subscenes (but I believe its not the case)
        //                                                                        - You believed wrong
        if (activeSceneOnly) {
            var activeScene = SceneManager.GetActiveScene();
            var goArr = activeScene.GetRootGameObjects();

            // go through open objects
            for (int i = 0; i < goArr.Length; i++) {
                GameObject rootGameObject = goArr[i];
                if (!rootGameObject.activeSelf) continue;

                // go through all components
                var compArr = rootGameObject.GetComponentsInChildren<T>();
                for (int j = 0; j < compArr.Length; j++) {
                    result.Add(compArr[j]);
                }
            }
        }

        else {
            // go through open scenes
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                // go through open objects
                foreach (var rootGameObject in SceneManager.GetSceneAt(i).GetRootGameObjects()) {
                    if (!rootGameObject.activeSelf) continue;

                    // go through all components
                    foreach (var childInterface in rootGameObject.GetComponentsInChildren<T>())
                        result.Add(childInterface);
                }
            }
        }

        return result;
    }
    /// <summary>
    /// Finds the first object of type T. Supports Interfaces.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T FindObjectOfType<T>() {
        // go through open scenes
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            // go through open objects
            foreach (var rootGameObject in SceneManager.GetSceneAt(i).GetRootGameObjects()) {
                if (!rootGameObject.activeSelf) continue;

                // return first valid component if found
                return rootGameObject.GetComponentInChildren<T>();
            }
        }

        return default;
    }
}