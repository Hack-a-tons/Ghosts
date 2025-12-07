using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugLogger : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[GHOST_DEBUG] ========== APP STARTING ==========");
        Debug.Log($"[GHOST_DEBUG] Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"[GHOST_DEBUG] Scene path: {SceneManager.GetActiveScene().path}");
        Debug.Log($"[GHOST_DEBUG] Device: {SystemInfo.deviceModel}");
        Debug.Log($"[GHOST_DEBUG] OS: {SystemInfo.operatingSystem}");
        
        // List all root objects in scene
        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        Debug.Log($"[GHOST_DEBUG] Root objects in scene: {rootObjects.Length}");
        foreach (var obj in rootObjects)
        {
            Debug.Log($"[GHOST_DEBUG]   - {obj.name} (active: {obj.activeSelf})");
        }
        
        // Check for key components
        Debug.Log($"[GHOST_DEBUG] Camera.main: {(Camera.main != null ? Camera.main.name : "NULL")}");
        Debug.Log($"[GHOST_DEBUG] LocationService: {(FindFirstObjectByType<LocationService>() != null ? "Found" : "NOT FOUND")}");
        Debug.Log($"[GHOST_DEBUG] GhostAPI: {(FindFirstObjectByType<GhostAPI>() != null ? "Found" : "NOT FOUND")}");
        Debug.Log($"[GHOST_DEBUG] GhostManager: {(FindFirstObjectByType<GhostManager>() != null ? "Found" : "NOT FOUND")}");
    }
    
    void Start()
    {
        Debug.Log("[GHOST_DEBUG] DebugLogger Start() called");
        InvokeRepeating(nameof(LogStatus), 2f, 5f);
    }
    
    void LogStatus()
    {
        var loc = LocationService.Instance;
        var api = GhostAPI.Instance;
        var mgr = GhostManager.Instance;
        
        Debug.Log("[GHOST_DEBUG] ----- STATUS CHECK -----");
        Debug.Log($"[GHOST_DEBUG] LocationService.Instance: {(loc != null ? "OK" : "NULL")}");
        if (loc != null)
        {
            Debug.Log($"[GHOST_DEBUG]   IsRunning: {loc.IsRunning}");
            Debug.Log($"[GHOST_DEBUG]   Position: {loc.Latitude}, {loc.Longitude}");
        }
        Debug.Log($"[GHOST_DEBUG] GhostAPI.Instance: {(api != null ? "OK" : "NULL")}");
        Debug.Log($"[GHOST_DEBUG] GhostManager.Instance: {(mgr != null ? "OK" : "NULL")}");
        
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        Debug.Log($"[GHOST_DEBUG] GhostVisual objects in scene: {ghosts.Length}");
    }
}
