using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
namespace NorthernLights.Engine.Saves {
    [System.Serializable]
    public class SaveContainer {
        [SerializeField]
        public string ck; 
        [SerializeField]
        public List<SaveContainerEntry> cc;

        public SaveContainer(string containerKey) {
            this.ck = containerKey;
        }

        /// <summary>
        /// Sets the value of a save entry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetEntry<T>(string key, T value) {
            if (cc == null) cc = new List<SaveContainerEntry>();

            var found = cc.Find(x => x.k == key);
            if (found != null) found.v = value;
            else cc.Add(new SaveContainerEntry(key, value));
        }
        /// <summary>
        /// Returns the value of a save entry 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetEntry<T>(string key) {
            var found = cc.Find(x => x.k == key);
            if (found?.v is T) return (T)found.v;

            // fix for floats returning as doubles
            else if (found?.v is double && typeof(T) == typeof(float))
                return (T)Convert.ChangeType(found.v, typeof(T));

            // fix for ints returning as long
            else if (found?.v is long && typeof(T) == typeof(int))
                return (T)Convert.ChangeType(found.v, typeof(T));

            // nested classes?
            else if (found?.v is JToken token) return token.ToObject<T>();

            else return default;
        }
    }
} 