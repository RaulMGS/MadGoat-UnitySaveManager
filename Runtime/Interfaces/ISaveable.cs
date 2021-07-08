using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MGFramework.Saves {
    public interface ISaveable {
        void OnGameLoaded();
        void OnGameSaving();
    }
}