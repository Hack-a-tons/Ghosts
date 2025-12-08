using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cross-platform map UI that works on Quest, iOS, and Android.
/// Shows a full-screen map view with ghost markers.
/// Auto-creates UI if not present in scene.
/// </summary>
public class MapUIController : MonoBehaviour
{
    public static MapUIController Instance { get; private set; }
    
    [Header("Mode")]
    public bool showMap = true;
    
    [Header("UI References (auto-created if null)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RawImage mapImage;
    [SerializeField] private RectTransform markersContainer;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI instructionsText;
    
    [Header("Map Settings")]
    [SerializeField] private int mapZoom = 15;
    [SerializeField] private float metersPerPixel = 4.8f; // At zoom 15
    
    [Header("Marker Settings")]
    [SerializeField] private Color playerColor = Color.blue;
    [SerializeField] private Color ghostColor = new Color(0f, 1f, 1f, 0.9f);
    [SerializeField] private Color nearbyColor = Color.green;
    [SerializeField] private float nearbyThreshold = 100f;
    
    [Header("AR Transition")]
    [SerializeField] private float arTriggerDistance = 50f;
    
    private RectTransform playerMarker;
    private Dictionary<int, RectTransform> ghostMarkers = new Dictionary<int, RectTransform>();
    private double mapCenterLat, mapCenterLng;
    private bool mapLoaded;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
    
    void Start()
    {
        CreateUI();
        StartCoroutine(InitializeMap());
    }
    
    void CreateUI()
    {
        // Find or create canvas
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("MapCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // On top
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }
        
        // Create map container
        var mapContainer = new GameObject("MapContainer");
        mapContainer.transform.SetParent(canvas.transform, false);
        var containerRect = mapContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(mapContainer.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.15f, 0.1f);
        
        // Map image
        var mapObj = new GameObject("MapImage");
        mapObj.transform.SetParent(mapContainer.transform, false);
        var mapRect = mapObj.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.05f, 0.12f);
        mapRect.anchorMax = new Vector2(0.95f, 0.88f);
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        mapImage = mapObj.AddComponent<RawImage>();
        mapImage.color = new Color(0.2f, 0.3f, 0.2f);
        
        // Markers container (centered on map)
        var markersObj = new GameObject("Markers");
        markersObj.transform.SetParent(mapObj.transform, false);
        markersContainer = markersObj.AddComponent<RectTransform>();
        markersContainer.anchorMin = new Vector2(0.5f, 0.5f);
        markersContainer.anchorMax = new Vector2(0.5f, 0.5f);
        markersContainer.sizeDelta = Vector2.zero;
        
        // Player marker
        var playerObj = new GameObject("PlayerMarker");
        playerObj.transform.SetParent(markersContainer, false);
        playerMarker = playerObj.AddComponent<RectTransform>();
        playerMarker.sizeDelta = new Vector2(24, 24);
        playerMarker.anchoredPosition = Vector2.zero;
        var playerImg = playerObj.AddComponent<Image>();
        playerImg.color = playerColor;
        
        // Status text
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(mapContainer.transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.9f);
        statusRect.anchorMax = new Vector2(1, 1);
        statusRect.offsetMin = new Vector2(10, 0);
        statusRect.offsetMax = new Vector2(-10, -5);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Initializing location...";
        statusText.fontSize = 24;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        
        // Instructions text
        var instrObj = new GameObject("Instructions");
        instrObj.transform.SetParent(mapContainer.transform, false);
        var instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0, 0);
        instrRect.anchorMax = new Vector2(1, 0.1f);
        instrRect.offsetMin = new Vector2(10, 5);
        instrRect.offsetMax = new Vector2(-10, 0);
        instructionsText = instrObj.AddComponent<TextMeshProUGUI>();
        instructionsText.text = "Walk towards a ghost to interact";
        instructionsText.fontSize = 20;
        instructionsText.alignment = TextAlignmentOptions.Center;
        instructionsText.color = new Color(0.7f, 0.7f, 0.7f);
        
        Debug.Log("[MapUIController] UI created");
    }
    
    IEnumerator InitializeMap()
    {
        // Wait for location service
        while (LocationService.Instance == null || !LocationService.Instance.IsRunning)
        {
            if (statusText != null)
                statusText.text = "Waiting for GPS...";
            yield return new WaitForSeconds(0.5f);
        }
        
        mapCenterLat = LocationService.Instance.Latitude;
        mapCenterLng = LocationService.Instance.Longitude;
        
        UpdateStatus();
        StartCoroutine(LoadMapTile());
    }
    
    IEnumerator LoadMapTile()
    {
        if (statusText != null)
            statusText.text = "Loading map...";
        
        // OpenStreetMap static tile
        string url = $"https://staticmap.openstreetmap.de/staticmap.php?center={mapCenterLat},{mapCenterLng}&zoom={mapZoom}&size=640x640&maptype=mapnik";
        
        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (mapImage != null)
                {
                    mapImage.texture = texture;
                    mapImage.color = Color.white;
                }
                mapLoaded = true;
                Debug.Log("[MapUIController] Map loaded");
            }
            else
            {
                Debug.LogWarning($"[MapUIController] Map load failed: {www.error}");
            }
        }
        
        UpdateStatus();
    }
    
    void Update()
    {
        if (!showMap) return;
        
        UpdateGhostMarkers();
        UpdateStatus();
    }
    
    void UpdateStatus()
    {
        if (statusText == null || LocationService.Instance == null) return;
        
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        int ghostCount = 0;
        float nearestDist = float.MaxValue;
        string nearestName = "";
        
        foreach (var ghost in ghosts)
        {
            if (ghost.Data == null) continue;
            ghostCount++;
            float dist = ghost.DistanceToPlayer();
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestName = ghost.Data.name;
            }
        }
        
        if (ghostCount == 0)
        {
            statusText.text = $"Searching for ghosts...\n({LocationService.Instance.Latitude:F4}, {LocationService.Instance.Longitude:F4})";
        }
        else if (nearestDist < arTriggerDistance)
        {
            statusText.text = $"👻 {nearestName} is nearby! ({nearestDist:F0}m)";
            statusText.color = nearbyColor;
        }
        else
        {
            statusText.text = $"Found {ghostCount} ghost(s) - Nearest: {nearestDist:F0}m";
            statusText.color = Color.white;
        }
    }
    
    void UpdateGhostMarkers()
    {
        if (markersContainer == null) return;
        
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        HashSet<int> activeIds = new HashSet<int>();
        
        foreach (var ghost in ghosts)
        {
            if (ghost.Data == null) continue;
            
            int id = ghost.Data.id;
            activeIds.Add(id);
            
            if (!ghostMarkers.TryGetValue(id, out RectTransform marker))
            {
                marker = CreateGhostMarker(ghost.Data.name);
                ghostMarkers[id] = marker;
            }
            
            // Position on map
            Vector2 screenPos = GeoToMapPos(ghost.Data.location.lat, ghost.Data.location.lng);
            marker.anchoredPosition = screenPos;
            
            // Color based on distance
            float dist = ghost.DistanceToPlayer();
            var img = marker.GetComponent<Image>();
            if (img != null)
                img.color = dist < nearbyThreshold ? nearbyColor : ghostColor;
            
            // Update label
            var label = marker.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{ghost.Data.name}\n{dist:F0}m";
        }
        
        // Remove old markers
        List<int> toRemove = new List<int>();
        foreach (var kvp in ghostMarkers)
        {
            if (!activeIds.Contains(kvp.Key))
            {
                Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (int id in toRemove)
            ghostMarkers.Remove(id);
    }
    
    RectTransform CreateGhostMarker(string name)
    {
        var marker = new GameObject($"Ghost_{name}");
        marker.transform.SetParent(markersContainer, false);
        
        var rect = marker.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(30, 30);
        
        var img = marker.AddComponent<Image>();
        img.color = ghostColor;
        
        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(marker.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, -25);
        labelRect.sizeDelta = new Vector2(120, 40);
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = name;
        label.fontSize = 14;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        
        return rect;
    }
    
    Vector2 GeoToMapPos(double lat, double lng)
    {
        double dLat = lat - mapCenterLat;
        double dLng = lng - mapCenterLng;
        
        // Meters per degree
        double metersPerDegreeLat = 111320;
        double metersPerDegreeLng = 111320 * System.Math.Cos(mapCenterLat * Mathf.Deg2Rad);
        
        // Convert to pixels
        float x = (float)(dLng * metersPerDegreeLng / metersPerPixel);
        float y = (float)(dLat * metersPerDegreeLat / metersPerPixel);
        
        return new Vector2(x, y);
    }
    
    public void SetMapVisible(bool visible)
    {
        showMap = visible;
        if (canvas != null)
            canvas.gameObject.SetActive(visible);
    }
}
