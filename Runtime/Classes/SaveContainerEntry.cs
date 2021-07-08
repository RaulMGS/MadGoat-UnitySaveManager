using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NorthernLights.Engine.Saves {  
    [System.Serializable]
    public class SaveContainerEntry {
        [SerializeField]
        public string k;
        [SerializeField]
        public object v;

        public SaveContainerEntry(string key, object value) {
            k = key;
            v = value;
        }
    }
}