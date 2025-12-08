using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapUIController : MonoBehaviour
{
    public static MapUIController Instance { get; private set; }
    
    [Header("Mode")]
    public bool showMap = true;
    public bool isARMode = false;
    
    [Header("UI References")]
    private Canvas canvas;
    private GameObject mapContainer;
    private RawImage mapImage;
    private RectTransform markersContainer;
    private TextMeshProUGUI statusText;
    private Button arButton;
    
    [Header("Map Settings")]
    private int mapZoom = 16;
    private float metersPerPixel = 2.4f;
    
    [Header("Test Ghost")]
    [SerializeField] private bool createTestGhostNearby = true;
    [SerializeField] private float testGhostDistance = 15f; // meters - offset so not hidden
    
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
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        
        CreateUI();
        StartCoroutine(InitializeMap());
    }
    
    void CreateUI()
    {
        var canvasObj = new GameObject("MapCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Map container - full screen
        mapContainer = new GameObject("MapContainer");
        mapContainer.transform.SetParent(canvas.transform, false);
        var containerRect = mapContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // Dark background
        var bg = new GameObject("Background");
        bg.transform.SetParent(mapContainer.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.1f);
        
        // Map image - square, centered, with padding
        var mapObj = new GameObject("MapImage");
        mapObj.transform.SetParent(mapContainer.transform, false);
        var mapRect = mapObj.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta = new Vector2(600, 600); // Fixed square size
        mapRect.anchoredPosition = new Vector2(0, 30); // Slight offset up
        mapImage = mapObj.AddComponent<RawImage>();
        mapImage.color = new Color(0.2f, 0.25f, 0.2f);
        var mapAspect = mapObj.AddComponent<AspectRatioFitter>();
        mapAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        mapAspect.aspectRatio = 1f;
        
        // Markers container - centered on map
        var markersObj = new GameObject("Markers");
        markersObj.transform.SetParent(mapObj.transform, false);
        markersContainer = markersObj.AddComponent<RectTransform>();
        markersContainer.anchorMin = new Vector2(0.5f, 0.5f);
        markersContainer.anchorMax = new Vector2(0.5f, 0.5f);
        markersContainer.sizeDelta = Vector2.zero;
        
        // Player marker - emoji style
        playerMarker = CreateEmojiMarker("📍", "You", markersContainer, Color.white);
        playerMarker.anchoredPosition = Vector2.zero;
        playerMarker.SetAsLastSibling(); // On top
        
        // Status text (top)
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(mapContainer.transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.88f);
        statusRect.anchorMax = new Vector2(1, 0.98f);
        statusRect.offsetMin = new Vector2(20, 0);
        statusRect.offsetMax = new Vector2(-20, -20);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Initializing...";
        statusText.fontSize = 32;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        
        // AR Button (bottom) - larger, more padding from edge
        var btnObj = new GameObject("ARButton");
        btnObj.transform.SetParent(mapContainer.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.15f, 0.03f);
        btnRect.anchorMax = new Vector2(0.85f, 0.1f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.1f, 0.5f, 0.1f);
        arButton = btnObj.AddComponent<Button>();
        arButton.onClick.AddListener(OnARButtonClick);
        var btnColors = arButton.colors;
        btnColors.highlightedColor = new Color(0.2f, 0.7f, 0.2f);
        btnColors.pressedColor = new Color(0.3f, 0.8f, 0.3f);
        arButton.colors = btnColors;
        
        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "👻 Enter AR Mode";
        btnText.fontSize = 32;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        
        Debug.Log("[MapUIController] UI created");
    }
    
    RectTransform CreateEmojiMarker(string emoji, string label, Transform parent, Color labelColor)
    {
        var marker = new GameObject($"Marker_{label}");
        marker.transform.SetParent(parent, false);
        var rect = marker.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, 80);
        
        // Emoji
        var emojiObj = new GameObject("Emoji");
        emojiObj.transform.SetParent(marker.transform, false);
        var emojiRect = emojiObj.AddComponent<RectTransform>();
        emojiRect.anchoredPosition = new Vector2(0, 15);
        emojiRect.sizeDelta = new Vector2(50, 50);
        var emojiText = emojiObj.AddComponent<TextMeshProUGUI>();
        emojiText.text = emoji;
        emojiText.fontSize = 40;
        emojiText.alignment = TextAlignmentOptions.Center;
        
        // Label below
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(marker.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, -25);
        labelRect.sizeDelta = new Vector2(120, 30);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 14;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = labelColor;
        
        return rect;
    }
    
    IEnumerator InitializeMap()
    {
        while (LocationService.Instance == null || !LocationService.Instance.IsRunning)
        {
            statusText.text = "📡 Waiting for GPS...";
            yield return new WaitForSeconds(0.5f);
        }
        
        mapCenterLat = LocationService.Instance.Latitude;
        mapCenterLng = LocationService.Instance.Longitude;
        
        Debug.Log($"[MapUIController] Location: {mapCenterLat}, {mapCenterLng}");
        
        if (createTestGhostNearby)
            CreateTestGhostNearby();
        
        StartCoroutine(LoadMapTile());
    }
    
    void CreateTestGhostNearby()
    {
        // Position NE of player so not hidden
        double testLat = mapCenterLat + (testGhostDistance / 111320.0);
        double testLng = mapCenterLng + (testGhostDistance / (111320.0 * System.Math.Cos(mapCenterLat * Mathf.Deg2Rad)));
        
        var testData = new GhostData
        {
            id = 9999,
            name = "Friendly Ghost",
            personality = "Helpful test ghost",
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
            
            Debug.Log($"[MapUIController] Created test ghost at {testLat:F6}, {testLng:F6}");
        }
    }
    
    IEnumerator LoadMapTile()
    {
        statusText.text = "🗺️ Loading map...";
        
        string url = $"https://staticmap.openstreetmap.de/staticmap.php?center={mapCenterLat},{mapCenterLng}&zoom={mapZoom}&size=600x600&maptype=mapnik";
        
        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            www.timeout = 15;
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (texture != null && mapImage != null)
                {
                    mapImage.texture = texture;
                    mapImage.color = Color.white;
                    mapLoaded = true;
                    Debug.Log("[MapUIController] Map loaded");
                }
            }
            else
            {
                Debug.LogWarning($"[MapUIController] Map failed: {www.error}");
                statusText.text = "Map unavailable";
            }
        }
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
        int count = 0;
        float nearestDist = float.MaxValue;
        string nearestName = "";
        
        foreach (var ghost in ghosts)
        {
            if (ghost.Data == null) continue;
            count++;
            float dist = ghost.DistanceToPlayer();
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestName = ghost.Data.name;
                selectedGhost = ghost;
            }
        }
        
        if (count == 0)
            statusText.text = "🔍 Searching for ghosts...";
        else
            statusText.text = $"👻 {count} ghost(s) • Nearest: {nearestDist:F0}m";
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
            
            Vector2 pos = GeoToMapPos(ghost.Data.location.lat, ghost.Data.location.lng);
            marker.anchoredPosition = pos;
            
            float dist = ghost.DistanceToPlayer();
            var label = marker.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{ghost.Data.name}\n{dist:F0}m";
            
            // Bring close ghosts to front (but behind player)
            marker.SetSiblingIndex(dist < 50 ? markersContainer.childCount - 2 : 0);
        }
        
        // Cleanup
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
        
        // Keep player on top
        playerMarker.SetAsLastSibling();
    }
    
    RectTransform CreateGhostMarker(GhostVisual ghost)
    {
        var marker = new GameObject($"Ghost_{ghost.Data.id}");
        marker.transform.SetParent(markersContainer, false);
        var rect = marker.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 100);
        
        // Ghost emoji
        var emojiObj = new GameObject("Emoji");
        emojiObj.transform.SetParent(marker.transform, false);
        var emojiRect = emojiObj.AddComponent<RectTransform>();
        emojiRect.anchoredPosition = new Vector2(0, 20);
        emojiRect.sizeDelta = new Vector2(60, 60);
        var emojiText = emojiObj.AddComponent<TextMeshProUGUI>();
        emojiText.text = "👻";
        emojiText.fontSize = 48;
        emojiText.alignment = TextAlignmentOptions.Center;
        
        // Tap area (invisible, larger)
        var tapArea = new GameObject("TapArea");
        tapArea.transform.SetParent(marker.transform, false);
        var tapRect = tapArea.AddComponent<RectTransform>();
        tapRect.sizeDelta = new Vector2(100, 120);
        var tapImg = tapArea.AddComponent<Image>();
        tapImg.color = new Color(0, 0, 0, 0); // Invisible
        var btn = tapArea.AddComponent<Button>();
        btn.onClick.AddListener(() => OnGhostMarkerClick(ghost));
        
        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(marker.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, -30);
        labelRect.sizeDelta = new Vector2(140, 50);
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = ghost.Data.name;
        label.fontSize = 14;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.cyan;
        
        return rect;
    }
    
    void OnGhostMarkerClick(GhostVisual ghost)
    {
        Debug.Log($"[MapUIController] Tapped: {ghost.Data.name}");
        selectedGhost = ghost;
        EnterARMode();
    }
    
    void OnARButtonClick()
    {
        Debug.Log("[MapUIController] AR button clicked");
        EnterARMode();
    }
    
    public void EnterARMode()
    {
        isARMode = true;
        if (mapContainer != null)
            mapContainer.SetActive(false);
        
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        foreach (var ghost in ghosts)
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        
        Debug.Log("[MapUIController] Entered AR mode");
    }
    
    public void ExitARMode()
    {
        isARMode = false;
        if (mapContainer != null)
            mapContainer.SetActive(true);
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
