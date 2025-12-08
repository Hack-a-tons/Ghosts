#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class QuestSceneSetup : EditorWindow
{
    [MenuItem("GhostLayer/Create Quest Scene")]
    static void CreateQuestScene()
    {
        // Create new scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Add OVRCameraRig
        GameObject cameraRig = null;
        string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                cameraRig = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                cameraRig.name = "OVRCameraRig";
                Debug.Log("Added OVRCameraRig from: " + path);
                
                // Enable passthrough on OVRManager
                var ovrManager = cameraRig.GetComponent<OVRManager>();
                if (ovrManager != null)
                {
                    ovrManager.isInsightPassthroughEnabled = true;
                    Debug.Log("Enabled Insight Passthrough on OVRManager");
                }
                
                // Add OVRPassthroughLayer
                var ptLayer = cameraRig.AddComponent<OVRPassthroughLayer>();
                ptLayer.overlayType = OVROverlay.OverlayType.Underlay;
                ptLayer.compositionDepth = 0;
                Debug.Log("Added OVRPassthroughLayer");
                
                // Set camera to clear with transparent
                var centerCam = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
                if (centerCam != null)
                {
                    var cam = centerCam.GetComponent<Camera>();
                    if (cam != null)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0, 0, 0, 0);
                        Debug.Log("Set CenterEyeAnchor camera to transparent");
                    }
                }
            }
        }
        
        // Fallback camera if no OVRCameraRig
        if (cameraRig == null)
        {
            cameraRig = new GameObject("MainCamera");
            Camera cam = cameraRig.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cameraRig.tag = "MainCamera";
            cameraRig.transform.position = new Vector3(0, 1.6f, 0);
            Debug.LogWarning("OVRCameraRig not found - using basic camera");
        }
        
        // Add directional light
        GameObject light = new GameObject("Directional Light");
        Light l = light.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1f;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        
        // Add Debug Logger
        GameObject debugObj = new GameObject("DebugLogger");
        debugObj.AddComponent<DebugLogger>();
        
        // Add Passthrough Enabler (runtime backup)
        debugObj.AddComponent<PassthroughEnabler>();
        
        // Add GhostManagers
        GameObject managers = new GameObject("GhostManagers");
        managers.AddComponent<LocationService>();
        managers.AddComponent<GhostAPI>();
        managers.AddComponent<GhostManager>();
        managers.AddComponent<GhostInteractor>();
        managers.AddComponent<MapUIController>(); // Add map UI
        
        // Assign ghost prefab
        var ghostManager = managers.GetComponent<GhostManager>();
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GhostPrefab.prefab");
        if (prefabAsset != null)
        {
            var field = typeof(GhostManager).GetField("ghostPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(ghostManager, prefabAsset);
            Debug.Log("Ghost prefab assigned");
        }
        
        // Add test ghost 2m in front
        if (prefabAsset != null)
        {
            GameObject testGhost = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            testGhost.name = "TestGhost";
            testGhost.transform.position = new Vector3(0, 1.5f, 2f);
            
            var visual = testGhost.GetComponent<GhostVisual>();
            if (visual != null)
            {
                var testData = new GhostData
                {
                    id = 999,
                    name = "Test Ghost",
                    personality = "Friendly",
                    visibility_radius_m = 100,
                    location = new GhostLocation { lat = 0, lng = 0 }
                };
                // Can't call Initialize in editor, will happen at runtime
            }
        }
        
        // Save scene
        string scenePath = "Assets/Scenes/QuestScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        
        // Set as ONLY build scene
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        
        Debug.Log($"Quest scene created with passthrough at {scenePath}");
    }
    
    [MenuItem("GhostLayer/Check Build Settings")]
    static void CheckBuildSettings()
    {
        Debug.Log("=== BUILD SETTINGS ===");
        foreach (var scene in EditorBuildSettings.scenes)
        {
            Debug.Log($"  [{(scene.enabled ? "X" : " ")}] {scene.path}");
        }
        Debug.Log($"Active scene: {SceneManager.GetActiveScene().path}");
    }
}
#endif
