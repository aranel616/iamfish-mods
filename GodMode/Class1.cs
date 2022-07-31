using UnityModManagerNet;
using HarmonyLib;
using System.Reflection;
using IAmFish.Fish;
using UnityEngine;

namespace GodMode {
    static class Main {
        public static bool enabled;
        public static bool godMode = false;
        
        public static UnityModManager.ModEntry mod;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;
            modEntry.OnToggle = OnToggle;

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;

            return true;
        }

        public static void ToggleGodMode()
        {
            godMode = !godMode;
        }
    }

    [HarmonyPatch(typeof(FishSuffocation), "Update")]
    static class FishSuffocationUpdatePatch {
        static bool Prefix()
        {
            if (!Main.enabled)
                return true;

            if (Input.GetKeyDown(KeyCode.F1))
                Main.ToggleGodMode();

            if (Main.godMode)
                return false;

            return true;
        }
    }
}