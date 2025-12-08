using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class MapSceneSetup : Editor
{
    [MenuItem("GhostLayer/Setup Map-First Scene")]
    public static void SetupMapFirstScene()
    {
        // Create root objects
        var mapUI = CreateMapUI();
        var managers = CreateManagers();
        
        Debug.Log("[MapSceneSetup] Map-first scene setup complete!");
        Debug.Log("1. Assign GhostPrefab to GhostManager");
        Debug.Log("2. Set your debug coordinates in LocationService");
        Debug.Log("3. Build and run on iOS");
    }
    
    static GameObject CreateMapUI()
    {
        // Find or create Canvas
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("MapCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create MapUI container
        var mapUI = new GameObject("MapUI");
        mapUI.transform.SetParent(canvas.transform, false);
        var mapRect = mapUI.AddComponent<RectTransform>();
        mapRect.anchorMin = Vector2.zero;
        mapRect.anchorMax = Vector2.one;
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        
        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(mapUI.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.2f, 0.15f);
        
        // Map image
        var mapImg = new GameObject("MapImage");
        mapImg.transform.SetParent(mapUI.transform, false);
        var mapImgRect = mapImg.AddComponent<RectTransform>();
        mapImgRect.anchorMin = new Vector2(0.05f, 0.15f);
        mapImgRect.anchorMax = new Vector2(0.95f, 0.85f);
        mapImgRect.offsetMin = Vector2.zero;
        mapImgRect.offsetMax = Vector2.zero;
        var rawImg = mapImg.AddComponent<RawImage>();
        rawImg.color = new Color(0.3f, 0.4f, 0.3f);
        
        // Markers container
        var markers = new GameObject("Markers");
        markers.transform.SetParent(mapImg.transform, false);
        var markersRect = markers.AddComponent<RectTransform>();
        markersRect.anchorMin = new Vector2(0.5f, 0.5f);
        markersRect.anchorMax = new Vector2(0.5f, 0.5f);
        markersRect.sizeDelta = Vector2.zero;
        
        // Status text
        var status = new GameObject("StatusText");
        status.transform.SetParent(mapUI.transform, false);
        var statusRect = status.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.9f);
        statusRect.anchorMax = new Vector2(1, 1);
        statusRect.offsetMin = new Vector2(10, 0);
        statusRect.offsetMax = new Vector2(-10, -10);
        var statusTmp = status.AddComponent<TextMeshProUGUI>();
        statusTmp.text = "Loading location...";
        statusTmp.fontSize = 18;
        statusTmp.alignment = TextAlignmentOptions.Center;
        
        // Instructions
        var instructions = new GameObject("Instructions");
        instructions.transform.SetParent(mapUI.transform, false);
        var instrRect = instructions.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0, 0);
        instrRect.anchorMax = new Vector2(1, 0.12f);
        instrRect.offsetMin = new Vector2(10, 10);
        instrRect.offsetMax = new Vector2(-10, 0);
        var instrTmp = instructions.AddComponent<TextMeshProUGUI>();
        instrTmp.text = "Walk towards a ghost marker to enter AR mode";
        instrTmp.fontSize = 16;
        instrTmp.alignment = TextAlignmentOptions.Center;
        instrTmp.color = new Color(0.7f, 0.7f, 0.7f);
        
        // Add FullMapView component
        var fullMapView = mapUI.AddComponent<FullMapView>();
        
        // Use SerializedObject to set private fields
        var so = new SerializedObject(fullMapView);
        so.FindProperty("mapImage").objectReferenceValue = rawImg;
        so.FindProperty("markersContainer").objectReferenceValue = markersRect;
        so.FindProperty("statusText").objectReferenceValue = statusTmp;
        so.ApplyModifiedProperties();
        
        return mapUI;
    }
    
    static GameObject CreateManagers()
    {
        // Find or create managers object
        var managers = GameObject.Find("Managers");
        if (managers == null)
            managers = new GameObject("Managers");
        
        // LocationService
        if (FindFirstObjectByType<LocationService>() == null)
        {
            var locService = managers.AddComponent<LocationService>();
            var so = new SerializedObject(locService);
            so.FindProperty("useDebugLocation").boolValue = false; // Use real GPS on iOS
            so.ApplyModifiedProperties();
        }
        
        // GhostAPI
        if (FindFirstObjectByType<GhostAPI>() == null)
            managers.AddComponent<GhostAPI>();
        
        // GhostManager
        if (FindFirstObjectByType<GhostManager>() == null)
            managers.AddComponent<GhostManager>();
        
        // GameModeManager
        if (FindFirstObjectByType<GameModeManager>() == null)
        {
            var gmm = managers.AddComponent<GameModeManager>();
            var mapUI = GameObject.Find("MapUI");
            if (mapUI != null)
            {
                var so = new SerializedObject(gmm);
                so.FindProperty("mapUI").objectReferenceValue = mapUI;
                so.ApplyModifiedProperties();
            }
        }
        
        return managers;
    }
}
