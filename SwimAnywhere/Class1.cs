using UnityModManagerNet;
using HarmonyLib;
using System.Reflection;
using IAmFish.Fish;
using UnityEngine;

namespace SwimAnywhere {
    static class Main {
        public static bool enabled;
        public static bool alwaysInWater = false;
        
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

        public static void ToggleAlwaysInWater()
        {
            alwaysInWater = !alwaysInWater;
            Debug.Log("Setting always in water to " + alwaysInWater.ToString());
        }
    }

    [HarmonyPatch(typeof(FishController), "Update")]
    static class FishControllerUpdatePatch {
        static void Postfix()
        {
            if (!Main.enabled)
                return;

            if (Input.GetKeyDown(KeyCode.F2))
                Main.ToggleAlwaysInWater();

            return;
        }
    }

    [HarmonyPatch(typeof(FishController), "InWater", MethodType.Getter)]
    static class FishControllerInWaterPatch {
        static void Postfix(ref bool __result)
        {
            if (!Main.enabled)
                return;

            if (!Main.alwaysInWater)
                return;

            try
            {
                __result = true;
            }
            catch (System.Exception e)
            {
             
            }
        }
    }
}