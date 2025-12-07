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
        
        // Add OVRCameraRig if available, otherwise basic camera
        GameObject cameraRig = null;
        
        // Try to find OVRCameraRig prefab
        string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                cameraRig = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                cameraRig.name = "OVRCameraRig";
                Debug.Log("Added OVRCameraRig");
            }
        }
        
        // Fallback to basic camera
        if (cameraRig == null)
        {
            cameraRig = new GameObject("MainCamera");
            Camera cam = cameraRig.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cameraRig.tag = "MainCamera";
            Debug.Log("Added basic camera (OVRCameraRig not found)");
        }
        
        // Add directional light
        GameObject light = new GameObject("Directional Light");
        Light l = light.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1f;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        
        // Add GhostManagers
        GameObject managers = new GameObject("GhostManagers");
        managers.AddComponent<LocationService>();
        managers.AddComponent<GhostAPI>();
        managers.AddComponent<GhostManager>();
        managers.AddComponent<GhostInteractor>();
        
        // Try to assign ghost prefab
        var ghostManager = managers.GetComponent<GhostManager>();
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GhostPrefab.prefab");
        if (prefabAsset != null)
        {
            var field = typeof(GhostManager).GetField("ghostPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(ghostManager, prefabAsset);
        }
        
        // Add a test ghost directly in scene for debugging
        GameObject testGhost = null;
        if (prefabAsset != null)
        {
            testGhost = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            testGhost.name = "TestGhost";
            testGhost.transform.position = new Vector3(0, 1.5f, 3f); // 3m in front
            
            // Initialize with test data
            var visual = testGhost.GetComponent<GhostVisual>();
            if (visual != null)
            {
                var testData = new GhostData
                {
                    id = 999,
                    name = "Test Ghost",
                    personality = "Friendly test ghost",
                    visibility_radius_m = 100,
                    location = new GhostLocation { lat = 0, lng = 0 }
                };
                visual.Initialize(testData);
            }
        }
        
        // Save scene
        string scenePath = "Assets/Scenes/QuestScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        
        // Add to build settings
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < buildScenes.Count; i++)
        {
            if (buildScenes[i].path == scenePath)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenePath, true);
                found = true;
                // Move to top
                var s = buildScenes[i];
                buildScenes.RemoveAt(i);
                buildScenes.Insert(0, s);
                break;
            }
        }
        if (!found)
        {
            buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        
        Debug.Log($"Quest scene created at {scenePath} and set as first build scene");
        
        Selection.activeObject = managers;
    }
}
#endif
