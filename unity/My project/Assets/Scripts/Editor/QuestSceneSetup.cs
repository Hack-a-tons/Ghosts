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
                Debug.Log("Added OVRCameraRig from: " + path);
            }
        }
        
        // Fallback to basic camera
        if (cameraRig == null)
        {
            cameraRig = new GameObject("MainCamera");
            Camera cam = cameraRig.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
            cameraRig.tag = "MainCamera";
            cameraRig.transform.position = new Vector3(0, 1.6f, 0);
            Debug.Log("Added basic camera (OVRCameraRig not found)");
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
            Debug.Log("Ghost prefab assigned");
        }
        else
        {
            Debug.LogWarning("Ghost prefab not found at Assets/Prefabs/GhostPrefab.prefab");
        }
        
        // Add a visible test cube so we know scene loaded
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "TestCube_DeleteMe";
        testCube.transform.position = new Vector3(0, 1.5f, 3f);
        testCube.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        var cubeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        cubeMat.color = Color.red;
        testCube.GetComponent<Renderer>().material = cubeMat;
        
        // Add test ghost
        if (prefabAsset != null)
        {
            GameObject testGhost = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            testGhost.name = "TestGhost";
            testGhost.transform.position = new Vector3(1f, 1.5f, 3f);
        }
        
        // Save scene
        string scenePath = "Assets/Scenes/QuestScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        
        // Set as ONLY build scene
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        
        Debug.Log($"Quest scene created at {scenePath}");
        Debug.Log("Build settings updated - QuestScene is now the ONLY scene");
        
        Selection.activeObject = managers;
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
