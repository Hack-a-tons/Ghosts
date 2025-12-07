#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class GhostPrefabCreator : EditorWindow
{
    [MenuItem("GhostLayer/Create Ghost Prefab")]
    static void CreateGhostPrefab()
    {
        // Create root
        GameObject ghost = new GameObject("GhostPrefab");
        
        // Add visual (capsule)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(ghost.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.5f, 0.75f, 0.5f);
        
        // Remove collider from body
        Object.DestroyImmediate(body.GetComponent<Collider>());
        
        // Create ghost material
        Material mat = new Material(Shader.Find("Custom/Ghost"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.5f, 0.8f, 1f, 0.6f);
        body.GetComponent<Renderer>().material = mat;
        
        // Add glow sphere
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "Glow";
        glow.transform.SetParent(ghost.transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
        Object.DestroyImmediate(glow.GetComponent<Collider>());
        
        Material glowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        glowMat.color = new Color(0.7f, 0.9f, 1f, 0.2f);
        glowMat.SetFloat("_Surface", 1); // Transparent
        glow.GetComponent<Renderer>().material = glowMat;
        
        // Add name label
        GameObject labelObj = new GameObject("NameLabel");
        labelObj.transform.SetParent(ghost.transform);
        labelObj.transform.localPosition = new Vector3(0, 1.2f, 0);
        
        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = "Ghost Name";
        tmp.fontSize = 3;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        // Add interaction collider
        SphereCollider col = ghost.AddComponent<SphereCollider>();
        col.radius = 1f;
        col.isTrigger = true;
        
        // Add GhostVisual component
        GhostVisual visual = ghost.AddComponent<GhostVisual>();
        
        // Set serialized field via reflection
        var field = typeof(GhostVisual).GetField("nameLabel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(visual, tmp);
        
        // Save prefab
        string path = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        
        // Save material
        AssetDatabase.CreateAsset(mat, $"{path}/GhostMaterial.mat");
        AssetDatabase.CreateAsset(glowMat, $"{path}/GhostGlowMaterial.mat");
        
        // Save prefab
        string prefabPath = $"{path}/GhostPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(ghost, prefabPath);
        
        // Cleanup scene object
        DestroyImmediate(ghost);
        
        // Select the created prefab
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        Debug.Log($"Ghost prefab created at {prefabPath}");
    }
    
    [MenuItem("GhostLayer/Setup Scene")]
    static void SetupScene()
    {
        // Create managers object
        GameObject managers = new GameObject("GhostManagers");
        managers.AddComponent<LocationService>();
        managers.AddComponent<GhostAPI>();
        managers.AddComponent<GhostManager>();
        managers.AddComponent<GhostInteractor>();
        
        // Try to assign prefab
        var ghostManager = managers.GetComponent<GhostManager>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GhostPrefab.prefab");
        if (prefab != null)
        {
            var field = typeof(GhostManager).GetField("ghostPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(ghostManager, prefab);
        }
        
        Selection.activeObject = managers;
        Debug.Log("Ghost managers added to scene");
    }
}
#endif
