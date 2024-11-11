using UnityModManagerNet;
using HarmonyLib;
using UnityEngine;
using IAmFish.Fish;
using IAmFish.Core.Bootstrap;
using System.Reflection;
using System;
using IAmFish.Core.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace HighlightObjects
{
    static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        public static bool highlightShallowWater = true;
        public static bool highlightLevelEndTriggers = true;
        public static bool highlightAlwaysBreakObjects = true;
        public static bool highlightNeverBreakObjects = true;
        public static bool highlightNoFlyZones = true;
        public static bool highlightWaterObjects = false;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            highlightShallowWater = GUILayout.Toggle(highlightShallowWater, "Highlight Shallow Water");
            highlightLevelEndTriggers = GUILayout.Toggle(highlightLevelEndTriggers, "Highlight Level End Triggers");
            highlightAlwaysBreakObjects = GUILayout.Toggle(highlightAlwaysBreakObjects, "Highlight Always Break Objects");
            highlightNeverBreakObjects = GUILayout.Toggle(highlightNeverBreakObjects, "Highlight Never Break Objects");
            highlightNoFlyZones = GUILayout.Toggle(highlightNoFlyZones, "Highlight No Fly Zones");
            highlightWaterObjects = GUILayout.Toggle(highlightWaterObjects, "Highlight Water Objects (Still In Progress)");
        }

        [HarmonyPatch(typeof(FishController), "Update")]
        static class FishControllerUpdatePatch
        {
            static void Postfix()
            {
                if (Main.enabled && Input.GetKeyDown(KeyCode.F1))
                {
                    if (highlightShallowWater)
                    {
                        HighlightShallowWater();
                    }

                    if (highlightLevelEndTriggers)
                    {
                        HighlightLevelEndTriggers();
                    }

                    if (highlightWaterObjects)
                    {
                        HighlightWaterObjects();
                    }

                    if (highlightAlwaysBreakObjects)
                    {
                        HighlightAlwaysBreakObjects();
                    }

                    if (highlightNeverBreakObjects)
                    {
                        HighlightNeverBreakObjects();
                    }

                    if (highlightNoFlyZones)
                    {
                        HighlightNoFlyZones();
                    }
                }
            }
        }

        public static void HighlightShallowWater()
        {
            var shallowWaters = GameObject.FindObjectsOfType<ShallowWater>();
            foreach (var shallowWater in shallowWaters)
            {
                Debug.Log($"Highlighting ShallowWater object: {shallowWater.gameObject.name}");
                CreateHighlightedCube(shallowWater.gameObject, Color.white);
            }
        }

        public static void HighlightLevelEndTriggers()
        {
            var levelEndTriggers = GameObject.FindGameObjectsWithTag("LevelEnd");
            foreach (var levelEndTrigger in levelEndTriggers)
            {
                Debug.Log($"Highlighting LevelEnd object: {levelEndTrigger.name}");
                CreateHighlightedCube(levelEndTrigger, Color.red);
            }
        }

        public static void HighlightAlwaysBreakObjects()
        {
            var objects = GameObject.FindGameObjectsWithTag("AlwaysBreak");
            foreach (var obj in objects)
            {
                Debug.Log($"Highlighting AlwaysBreak object: {obj.name}");
                HighlightObject(obj, Color.red);
            }
        }

        public static void HighlightNeverBreakObjects()
        {
            var objects = GameObject.FindGameObjectsWithTag("NeverBreak");
            foreach (var obj in objects)
            {
                Debug.Log($"Highlighting NeverBreak object: {obj.name}");
                HighlightObject(obj, Color.green);
            }
        }

        public static void HighlightNoFlyZones()
        {
            var objects = GameObject.FindGameObjectsWithTag("NoFlyZone");
            foreach (var obj in objects)
            {
                Debug.Log($"Highlighting NoFlyZone object: {obj.name}");
                CreateHighlightedCube(obj, Color.red);
            }
        }

        public static void HighlightWaterObjects()
        {
            int waterLayerMask = LayersCache.Instance.AllWaterLayerMask;

            foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if ((1 << obj.layer & waterLayerMask) != 0)
                {
                    Debug.Log("Found water surface: " + obj.name);

                    HighlightObject(obj, Color.blue);
                }
            }
        }

        public static void HighlightObject(GameObject gameObject, Color highlightColor)
        {
            Renderer component = gameObject.GetComponent<Renderer>();

            if (component == null)
            {
                component = gameObject.AddComponent<MeshRenderer>();
            }

            if (component != null)
            {
                Debug.Log("Applying highlight material to existing renderer");
                ApplyHighlightMaterial(component, highlightColor);
                return;
            }

            try
            {
                Debug.Log("Creating new renderer because we could not find one");
                component = gameObject.AddComponent<MeshRenderer>();
                ApplyHighlightMaterial(component, highlightColor);
            }
            catch
            {
                Debug.Log("Creating new cube because we could not add a renderer");
                CreateHighlightedCube(gameObject, highlightColor);
            }
        }

        private static void ApplyHighlightMaterial(Renderer renderer, Color highlightColor)
        {
            Material hdrpMaterial = new Material(Shader.Find("HDRP/Lit"));

            // Set base and emissive colors with transparency
            highlightColor.a = 0.5f; // Set alpha for semi-transparency (0.0f to 1.0f)
            hdrpMaterial.SetColor("_BaseColor", highlightColor);
            hdrpMaterial.SetColor("_EmissiveColor", highlightColor * 5f); // Enhance emission if needed

            // Set HDRP material properties for transparency
            hdrpMaterial.SetFloat("_SurfaceType", 1); // Transparent
            hdrpMaterial.SetFloat("_BlendMode", 0); // Alpha blend mode for transparency
            hdrpMaterial.SetFloat("_CullMode", 0); // Render both sides if needed
            hdrpMaterial.SetFloat("_Smoothness", 0f); // Adjust as needed
            hdrpMaterial.SetFloat("_Metallic", 0f); // Non-metallic appearance

            // Enable emission and transparency keywords
            hdrpMaterial.EnableKeyword("_EMISSION");
            hdrpMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // Assign the material to the renderer
            renderer.material = hdrpMaterial;
        }


        public static void CreateHighlightedCube(GameObject gameObject, Color highlightColor)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(cube.GetComponent<Collider>());

            // Start debug logging
            Debug.Log($"Creating highlighted cube for: {gameObject.name}");

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Debug.Log($"Found collider of type: {collider.GetType().Name} on {gameObject.name}");

                // Set position and rotation to match the collider's center and the GameObject's rotation
                cube.transform.SetPositionAndRotation(collider.bounds.center, gameObject.transform.rotation);
                Debug.Log($"Cube position set to collider bounds center: {collider.bounds.center}, rotation set to: {gameObject.transform.rotation}");

                // Adjust scale based on collider type and apply scaling in local space
                Vector3 scaleAdjustment;
                if (collider is BoxCollider boxCollider)
                {
                    scaleAdjustment = Vector3.Scale(boxCollider.size, gameObject.transform.localScale);
                    Debug.Log($"BoxCollider size: {boxCollider.size}, adjusted scale: {scaleAdjustment}");
                    cube.transform.localScale = scaleAdjustment;
                }
                else if (collider is SphereCollider sphereCollider)
                {
                    float diameter = sphereCollider.radius * 2f * Mathf.Max(gameObject.transform.localScale.x, gameObject.transform.localScale.y, gameObject.transform.localScale.z);
                    scaleAdjustment = new Vector3(diameter, diameter, diameter);
                    Debug.Log($"SphereCollider radius: {sphereCollider.radius}, adjusted diameter: {diameter}");
                    cube.transform.localScale = scaleAdjustment;
                }
                else if (collider is CapsuleCollider capsuleCollider)
                {
                    float heightScale = capsuleCollider.height * gameObject.transform.localScale.y;
                    float diameterScale = capsuleCollider.radius * 2f * Mathf.Max(gameObject.transform.localScale.x, gameObject.transform.localScale.z);
                    scaleAdjustment = new Vector3(diameterScale, heightScale, diameterScale);
                    Debug.Log($"CapsuleCollider height: {capsuleCollider.height}, radius: {capsuleCollider.radius}, adjusted scale: {scaleAdjustment}");
                    cube.transform.localScale = scaleAdjustment;
                }
                else if (collider is MeshCollider meshCollider)
                {
                    Debug.LogWarning($"Approximating cube size for MeshCollider on {gameObject.name}");
                    scaleAdjustment = Vector3.Scale(collider.bounds.size, gameObject.transform.localScale);
                    cube.transform.localScale = scaleAdjustment;
                    Debug.Log($"MeshCollider bounds size: {collider.bounds.size}, adjusted scale: {scaleAdjustment}");
                }
                else
                {
                    Debug.LogWarning($"Unsupported collider type on {gameObject.name}. Using collider bounds size.");
                    scaleAdjustment = Vector3.Scale(collider.bounds.size, gameObject.transform.localScale);
                    cube.transform.localScale = scaleAdjustment;
                    Debug.Log($"Fallback collider bounds size: {collider.bounds.size}, adjusted scale: {scaleAdjustment}");
                }
            }
            else
            {
                Debug.LogWarning($"No collider found on {gameObject.name}. Using GameObject transform.");
                cube.transform.SetPositionAndRotation(gameObject.transform.position, gameObject.transform.rotation);
                cube.transform.localScale = gameObject.transform.localScale;
                Debug.Log($"Cube position set to GameObject transform position: {gameObject.transform.position}, scale set to: {gameObject.transform.localScale}");
            }

            // Apply highlight material and log final cube properties
            Renderer renderer = cube.GetComponent<Renderer>();
            ApplyHighlightMaterial(renderer, highlightColor);
            Debug.Log($"Highlight applied to {gameObject.name} with color: {highlightColor}. Final cube position: {cube.transform.position}, rotation: {cube.transform.rotation}, scale: {cube.transform.localScale}");
        }
    }
}
