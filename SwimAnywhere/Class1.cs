using UnityModManagerNet;
using HarmonyLib;
using System.Reflection;
using IAmFish.Fish;
using UnityEngine;
using IAmFish.Core.Bootstrap; // To access Shell.Instance
using IAmFish.Player; // To access FishManager
using System;

namespace SwimAnywhere
{
    // Settings class to store mod configuration
    [Serializable]
    public class Settings : UnityModManager.ModSettings
    {
        // Option to apply the mod to NPC fish
        public bool ApplyToNPCFish = true;

        // Save method to persist settings
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    static class Main
    {
        public static bool enabled;
        public static bool alwaysInWater = false;

        public static UnityModManager.ModEntry mod;

        // Instance of the Settings class
        public static Settings settings;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;

            // Load settings
            settings = Settings.Load<Settings>(modEntry);
            settings.Save(modEntry); // Ensure settings file exists

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            Debug.Log("[SwimAnywhere] SwimAnywhere Mod loaded successfully.");
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            Debug.Log($"[SwimAnywhere] SwimAnywhere Mod {(enabled ? "enabled" : "disabled")}.");
            return true;
        }

        // OnGUI method to display settings in UMM interface
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            // Toggle for ApplyToNPCFish
            settings.ApplyToNPCFish = GUILayout.Toggle(settings.ApplyToNPCFish, "Apply to NPC Fish");
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
            Debug.Log("[SwimAnywhere] Settings have been saved.");
        }

        // Method to toggle the AlwaysInWater state
        public static void ToggleAlwaysInWater()
        {
            alwaysInWater = !alwaysInWater;
            Debug.Log($"[SwimAnywhere] Setting AlwaysInWater to {alwaysInWater}.");
        }
    }

    // Harmony patch to FishController.Update to detect key presses
    [HarmonyPatch(typeof(FishController), "Update")]
    static class FishControllerUpdatePatch
    {
        static void Postfix(FishController __instance)
        {
            if (!Main.enabled)
                return;

            // Access the FishManager via Shell.Instance.LevelService.FishManager
            FishManager fishManager = Shell.Instance.LevelService.FishManager;

            if (fishManager == null)
            {
                Debug.LogWarning("[SwimAnywhere] FishManager is not available. Skipping key detection.");
                return;
            }

            // Get the player-controlled fish from FishManager
            FishController playerFish = fishManager.CurrentFish;

            if (playerFish == null)
            {
                Debug.LogWarning("[SwimAnywhere] Player's CurrentFish is null. Skipping key detection.");
                return;
            }

            // Check if the current FishController instance is the player fish
            if (__instance != playerFish)
            {
                // It's an NPC fish; do not detect key presses
                return;
            }

            // Now, __instance is the player fish; proceed with key detection
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log("[SwimAnywhere] F2 pressed: Toggling AlwaysInWater.");
                Main.ToggleAlwaysInWater();
            }
        }
    }

    // Harmony patch to FishController.InWater getter to override the water state
    [HarmonyPatch(typeof(FishController), "InWater", MethodType.Getter)]
    static class FishControllerInWaterPatch
    {
        static void Postfix(FishController __instance, ref bool __result)
        {
            if (!Main.enabled)
                return;

            // Access the FishManager via Shell.Instance.LevelService.FishManager
            FishManager fishManager = Shell.Instance.LevelService.FishManager;

            if (fishManager == null)
            {
                // FishManager is not available; skip overriding InWater
                return;
            }

            // Get the player-controlled fish from FishManager
            FishController playerFish = fishManager.CurrentFish;

            if (playerFish == null)
            {
                // Player's CurrentFish is null; skip overriding InWater
                return;
            }

            // Check if the current FishController instance is the player fish
            if (__instance != playerFish && !Main.settings.ApplyToNPCFish)
            {
                // It's an NPC fish; do not override InWater
                return;
            }

            // Now, __instance is the player fish; proceed with InWater override
            if (Main.alwaysInWater)
            {
                __result = true;
            }
        }
    }
}
