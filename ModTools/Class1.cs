using UnityModManagerNet;
using UnityEngine;

namespace ModTools {
    public static class Main {
        public static bool enabled;
        
        public static UnityModManager.ModEntry mod;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            return true;
        }

        public static void DebugObject(GameObject gameObject)
        {
            Debug.Log($"Debugging object: {gameObject}, Name: {gameObject.name}, Tag: {gameObject.tag}, Layer: {gameObject.layer}");
        }
    }
}