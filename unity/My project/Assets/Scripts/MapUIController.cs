using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Cross-platform map UI that works on Quest, iOS, and Android.
/// Shows a full-screen map view with ghost markers.
/// Tap on ghost marker to enter AR mode.
/// </summary>
public class MapUIController : MonoBehaviour
{
    public static MapUIController Instance { get; private set; }
    
    [Header("Mode")]
    public bool showMap = true;
    public bool isARMode = false;
    
    [Header("UI References (auto-created if null)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject mapContainer;
    [SerializeField] private RawImage mapImage;
    [SerializeField] private RectTransform markersContainer;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button arButton;
    
    [Header("Map Settings")]
    [SerializeField] private int mapZoom = 16;
    [SerializeField] private float metersPerPixel = 2.4f; // At zoom 16
    
    [Header("Marker Settings")]
    [SerializeField] private Color playerColor = Color.blue;
    [SerializeField] private Color ghostColor = new Color(0f, 1f, 1f, 0.9f);
    [SerializeField] private Color nearbyColor = Color.green;
    [SerializeField] private float nearbyThreshold = 100f;
    
    [Header("Test Ghost")]
    [SerializeField] private bool createTestGhostNearby = true;
    [SerializeField] private float testGhostDistance = 5f; // meters
    
    private RectTransform playerMarker;
    private Dictionary<int, RectTransform> ghostMarkers = new Dictionary<int, RectTransform>();
    private double mapCenterLat, mapCenterLng;
    private bool mapLoaded;
    private GhostVisual selectedGhost;
    
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
            var canvasObj = new GameObject("MapCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Map container
        mapContainer = new GameObject("MapContainer");
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
        bgImg.color = new Color(0.15f, 0.2f, 0.15f);
        
        // Map image
        var mapObj = new GameObject("MapImage");
        mapObj.transform.SetParent(mapContainer.transform, false);
        var mapRect = mapObj.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.02f, 0.1f);
        mapRect.anchorMax = new Vector2(0.98f, 0.85f);
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        mapImage = mapObj.AddComponent<RawImage>();
        mapImage.color = new Color(0.25f, 0.35f, 0.25f);
        
        // Markers container
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
        playerMarker.sizeDelta = new Vector2(30, 30);
        playerMarker.anchoredPosition = Vector2.zero;
        var playerImg = playerObj.AddComponent<Image>();
        playerImg.color = playerColor;
        
        // Status text (top)
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(mapContainer.transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.87f);
        statusRect.anchorMax = new Vector2(1, 0.98f);
        statusRect.offsetMin = new Vector2(10, 0);
        statusRect.offsetMax = new Vector2(-10, 0);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Initializing...";
        statusText.fontSize = 28;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        
        // AR Button (bottom)
        var btnObj = new GameObject("ARButton");
        btnObj.transform.SetParent(mapContainer.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.2f, 0.02f);
        btnRect.anchorMax = new Vector2(0.8f, 0.08f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f);
        arButton = btnObj.AddComponent<Button>();
        arButton.onClick.AddListener(OnARButtonClick);
        
        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "Enter AR Mode";
        btnText.fontSize = 24;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        
        Debug.Log("[MapUIController] UI created");
    }
    
    IEnumerator InitializeMap()
    {
        // Wait for location
        while (LocationService.Instance == null || !LocationService.Instance.IsRunning)
        {
            statusText.text = "Waiting for GPS...";
            yield return new WaitForSeconds(0.5f);
        }
        
        mapCenterLat = LocationService.Instance.Latitude;
        mapCenterLng = LocationService.Instance.Longitude;
        
        Debug.Log($"[MapUIController] Location: {mapCenterLat}, {mapCenterLng}");
        
        // Create test ghost nearby
        if (createTestGhostNearby)
        {
            CreateTestGhostNearby();
        }
        
        // Load map
        StartCoroutine(LoadMapTile());
    }
    
    void CreateTestGhostNearby()
    {
        // Calculate position ~5m north of player
        double testLat = mapCenterLat + (testGhostDistance / 111320.0);
        double testLng = mapCenterLng;
        
        var testData = new GhostData
        {
            id = 9999,
            name = "Nearby Test Ghost",
            personality = "Friendly test ghost",
            visibility_radius_m = 100,
            location = new GhostLocation { lat = (float)testLat, lng = (float)testLng },
            interaction = new GhostInteraction
            {
                type = "riddle_unlock",
                riddle = "What has hands but can't clap?",
                correct_answer = "clock",
                reward = new GhostReward { type = "points", value = "100" }
            }
        };
        
        // Spawn via GhostManager or directly
        var prefab = Resources.Load<GameObject>("GhostPrefab");
        if (prefab == null)
        {
            // Try to find existing ghost prefab
            var existingGhost = FindFirstObjectByType<GhostVisual>();
            if (existingGhost != null)
            {
                var newGhost = Instantiate(existingGhost.gameObject);
                newGhost.name = "TestGhost_Nearby";
                var visual = newGhost.GetComponent<GhostVisual>();
                visual.Initialize(testData);
                
                Vector3 worldPos = LocationService.Instance.GeoToWorld(testLat, testLng);
                worldPos.y = 1.5f;
                newGhost.transform.position = worldPos;
                
                Debug.Log($"[MapUIController] Created test ghost at {testLat}, {testLng} (world: {worldPos})");
            }
        }
    }
    
    IEnumerator LoadMapTile()
    {
        statusText.text = "Loading map...";
        
        // Try multiple map tile providers
        string[] urls = new string[]
        {
            // OpenStreetMap static
            $"https://staticmap.openstreetmap.de/staticmap.php?center={mapCenterLat},{mapCenterLng}&zoom={mapZoom}&size=600x600&maptype=mapnik",
            // Fallback: simple tile from OSM
            $"https://tile.openstreetmap.org/{mapZoom}/{LonToTileX(mapCenterLng, mapZoom)}/{LatToTileY(mapCenterLat, mapZoom)}.png"
        };
        
        foreach (string url in urls)
        {
            Debug.Log($"[MapUIController] Trying map URL: {url}");
            
            using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                www.timeout = 10;
                yield return www.SendWebRequest();
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    if (texture != null && mapImage != null)
                    {
                        mapImage.texture = texture;
                        mapImage.color = Color.white;
                        mapLoaded = true;
                        Debug.Log("[MapUIController] Map loaded successfully");
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[MapUIController] Map load failed: {www.error}");
                }
            }
        }
        
        if (!mapLoaded)
        {
            statusText.text = "Map unavailable - using grid view";
        }
    }
    
    // OSM tile coordinate helpers
    int LonToTileX(double lon, int zoom)
    {
        return (int)((lon + 180.0) / 360.0 * (1 << zoom));
    }
    
    int LatToTileY(double lat, int zoom)
    {
        return (int)((1.0 - System.Math.Log(System.Math.Tan(lat * System.Math.PI / 180.0) + 
            1.0 / System.Math.Cos(lat * System.Math.PI / 180.0)) / System.Math.PI) / 2.0 * (1 << zoom));
    }
    
    void Update()
    {
        if (!showMap || isARMode) return;
        
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
                selectedGhost = ghost;
            }
        }
        
        if (ghostCount == 0)
        {
            statusText.text = "Searching for ghosts...";
            statusText.color = Color.white;
        }
        else
        {
            statusText.text = $"👻 {ghostCount} ghost(s) found\nNearest: {nearestName} ({nearestDist:F0}m)";
            statusText.color = nearestDist < 50 ? nearbyColor : Color.white;
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
                marker = CreateGhostMarker(ghost);
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
                label.text = $"{ghost.Data.name}\n{dist:F0}m\n<size=12>TAP to visit</size>";
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
    
    RectTransform CreateGhostMarker(GhostVisual ghost)
    {
        var marker = new GameObject($"Ghost_{ghost.Data.name}");
        marker.transform.SetParent(markersContainer, false);
        
        var rect = marker.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40, 40);
        
        var img = marker.AddComponent<Image>();
        img.color = ghostColor;
        
        // Make it clickable
        var btn = marker.AddComponent<Button>();
        btn.onClick.AddListener(() => OnGhostMarkerClick(ghost));
        
        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(marker.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, -35);
        labelRect.sizeDelta = new Vector2(150, 60);
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = ghost.Data.name;
        label.fontSize = 14;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        
        return rect;
    }
    
    void OnGhostMarkerClick(GhostVisual ghost)
    {
        Debug.Log($"[MapUIController] Clicked on ghost: {ghost.Data.name}");
        selectedGhost = ghost;
        EnterARMode();
    }
    
    void OnARButtonClick()
    {
        if (selectedGhost != null)
        {
            EnterARMode();
        }
        else
        {
            statusText.text = "No ghost selected - tap a ghost marker first";
        }
    }
    
    public void EnterARMode()
    {
        isARMode = true;
        if (mapContainer != null)
            mapContainer.SetActive(false);
        
        // Show ghosts in 3D
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        foreach (var ghost in ghosts)
        {
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        }
        
        Debug.Log("[MapUIController] Entered AR mode");
    }
    
    public void ExitARMode()
    {
        isARMode = false;
        if (mapContainer != null)
            mapContainer.SetActive(true);
        
        // Hide ghosts in 3D (optional - keep them visible)
        Debug.Log("[MapUIController] Exited AR mode");
    }
    
    Vector2 GeoToMapPos(double lat, double lng)
    {
        double dLat = lat - mapCenterLat;
        double dLng = lng - mapCenterLng;
        
        double metersPerDegreeLat = 111320;
        double metersPerDegreeLng = 111320 * System.Math.Cos(mapCenterLat * Mathf.Deg2Rad);
        
        float x = (float)(dLng * metersPerDegreeLng / metersPerPixel);
        float y = (float)(dLat * metersPerDegreeLat / metersPerPixel);
        
        return new Vector2(x, y);
    }
}
