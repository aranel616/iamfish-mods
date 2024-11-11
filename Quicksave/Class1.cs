using UnityModManagerNet;
using HarmonyLib;
using System.Reflection;
using IAmFish.Fish;
using IAmFish.Vehicles;
using IAmFish.Player; // To access FishManager
using IAmFish.Core.Bootstrap; // To access Shell.Instance
using UnityEngine;
using System;

namespace Quicksave
{
    static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        // Struct to store the saved state without velocity data
        [Serializable]
        public struct SavedState
        {
            // Fish state
            public Vector3 FishPosition;
            public Quaternion FishRotation;

            // Vehicle state
            public bool IsInVehicle;
            [NonSerialized] // Prevent serialization of the reference
            public AFishVehicleController VehicleReference;
            public Vector3 VehiclePosition;
            public Quaternion VehicleRotation;
        }

        // Variable to hold the saved state
        private static SavedState? quicksaveState = null;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;

            Debug.Log("[Quicksave] Quicksave Mod loaded successfully.");
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            Debug.Log($"[Quicksave] Quicksave Mod {(enabled ? "enabled" : "disabled")}.");
            return true;
        }

        // Method to save the current state
        public static void SaveQuicksave(FishController playerFish)
        {
            if (playerFish == null)
            {
                Debug.LogError("[Quicksave] Player FishController instance is null. Quicksave failed.");
                return;
            }

            if (playerFish.gameObject == null)
            {
                Debug.LogError("[Quicksave] Player FishController's GameObject is null. Quicksave failed.");
                return;
            }

            // Ensure RootRigidbody is not null
            Rigidbody rootRigidbody = playerFish.RootRigidbody;
            if (rootRigidbody == null)
            {
                Debug.LogWarning("[Quicksave] Player FishController's RootRigidbody is null. Velocity data will not be saved.");
            }

            SavedState state = new SavedState
            {
                FishPosition = playerFish.transform.position,
                FishRotation = playerFish.transform.rotation,
                IsInVehicle = playerFish.InVehicle
            };

            if (state.IsInVehicle && playerFish.CurrentVehicle != null)
            {
                AFishVehicleController vehicle = playerFish.CurrentVehicle;

                if (vehicle.gameObject == null)
                {
                    Debug.LogWarning("[Quicksave] Player's CurrentVehicle's GameObject is null. Skipping vehicle state.");
                    state.VehicleReference = null;
                    state.VehiclePosition = Vector3.zero;
                    state.VehicleRotation = Quaternion.identity;
                }
                else
                {
                    state.VehicleReference = vehicle;
                    state.VehiclePosition = vehicle.transform.position;
                    state.VehicleRotation = vehicle.transform.rotation;
                }
            }
            else
            {
                state.VehicleReference = null;
                state.VehiclePosition = Vector3.zero;
                state.VehicleRotation = Quaternion.identity;
            }

            quicksaveState = state;
            Debug.Log("[Quicksave] Quicksave created successfully.");
        }

        // Method to load the saved state
        public static void LoadQuicksave(FishController playerFish)
        {
            if (playerFish == null)
            {
                Debug.LogError("[Quicksave] Player FishController instance is null. Quicksave load failed.");
                return;
            }

            if (quicksaveState == null)
            {
                Debug.LogWarning("[Quicksave] No quicksave found. Please create a quicksave first by pressing F6.");
                return;
            }

            SavedState state = quicksaveState.Value;

            // Restore fish state
            if (playerFish != null && playerFish.gameObject != null)
            {
                playerFish.TeleportFish(state.FishPosition, state.FishRotation);

                if (playerFish.RootRigidbody != null)
                {
                    // Set velocities to zero as per requirement
                    playerFish.RootRigidbody.velocity = Vector3.zero;
                    playerFish.RootRigidbody.angularVelocity = Vector3.zero;
                    Debug.Log("[Quicksave] Player FishController's velocity and angular velocity set to zero.");
                }
                else
                {
                    Debug.LogWarning("[Quicksave] Player FishController's RootRigidbody is null. Skipping velocity restoration.");
                }
            }
            else
            {
                Debug.LogError("[Quicksave] Player FishController or its GameObject is null. Cannot restore fish state.");
                return;
            }

            // Restore vehicle state if necessary
            if (state.IsInVehicle)
            {
                if (state.VehicleReference != null)
                {
                    AFishVehicleController vehicle = state.VehicleReference;

                    if (vehicle.gameObject == null)
                    {
                        Debug.LogWarning("[Quicksave] VehicleReference's GameObject is null. Cannot restore vehicle state.");
                        return;
                    }

                    // Restore vehicle state using TeleportVehicle
                    vehicle.TeleportVehicle(state.VehiclePosition, state.VehicleRotation);

                    // Assign fish to vehicle
                    AssignFishToVehicle(playerFish, vehicle);
                    Debug.Log("[Quicksave] Quicksave vehicle state restored successfully.");
                }
                else
                {
                    Debug.LogWarning("[Quicksave] VehicleReference is null. Cannot restore vehicle state.");
                }
            }
            else
            {
                // Ensure fish is not in a vehicle
                if (playerFish.InVehicle && playerFish.CurrentVehicle != null)
                {
                    playerFish.UnsetVehicle();
                    Debug.Log("[Quicksave] Player was in a vehicle. Vehicle association removed.");
                }
            }

            Debug.Log("[Quicksave] Quicksave loaded successfully.");
        }

        // Helper method to assign fish to vehicle
        private static void AssignFishToVehicle(FishController playerFish, AFishVehicleController vehicle)
        {
            if (vehicle != null)
            {
                vehicle.AssignFish(playerFish);
                playerFish.SetVehicle(vehicle);
                Debug.Log("[Quicksave] Player fish assigned to vehicle successfully.");
            }
            else
            {
                Debug.LogError("[Quicksave] Attempted to assign player fish to a null vehicle.");
            }
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
                Debug.LogWarning("[Quicksave] FishManager is not available. Skipping quicksave/load.");
                return;
            }

            // Get the player-controlled fish from FishManager
            FishController playerFish = fishManager.CurrentFish;

            if (playerFish == null)
            {
                Debug.LogWarning("[Quicksave] Player's CurrentFish is null. Skipping quicksave/load.");
                return;
            }

            // Check if the current FishController instance is the player fish
            if (__instance != playerFish)
            {
                // It's an NPC fish; do not perform quicksave/load
                return;
            }

            // Now, __instance is the player fish; proceed with quicksave/load
            if (Input.GetKeyDown(KeyCode.F6))
            {
                Debug.Log("[Quicksave] F6 pressed: Attempting to save quicksave.");
                Main.SaveQuicksave(__instance);
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                Debug.Log("[Quicksave] F9 pressed: Attempting to load quicksave.");
                Main.LoadQuicksave(__instance);
            }
        }
    }
}
