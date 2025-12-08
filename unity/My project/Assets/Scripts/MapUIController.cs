using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapUIController : MonoBehaviour
{
    public static MapUIController Instance { get; private set; }
    
    public bool isARMode = false;
    
    private Canvas canvas;
    private GameObject mapContainer;
    private RawImage mapImage;
    private RectTransform markersContainer;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI debugText;
    
    private int mapZoom = 16;
    private float metersPerPixel = 2.4f;
    
    [SerializeField] private bool createTestGhostNearby = true;
    [SerializeField] private float testGhostDistance = 25f;
    
    private RectTransform playerMarker;
    private Dictionary<int, RectTransform> ghostMarkers = new Dictionary<int, RectTransform>();
    private double mapCenterLat, mapCenterLng;
    private GhostVisual selectedGhost;
    private Button arButton;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
    
    void Start()
    {
        // Ensure EventSystem exists
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Debug.Log("[MapUI] Created EventSystem");
        }
        
        CreateUI();
        StartCoroutine(InitializeMap());
    }
    
    void CreateUI()
    {
        // Canvas with proper settings
        var canvasObj = new GameObject("MapCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Very high to be on top
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Container
        mapContainer = new GameObject("MapContainer");
        mapContainer.transform.SetParent(canvas.transform, false);
        var containerRect = mapContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // Background - clickable to dismiss debug
        var bg = new GameObject("Background");
        bg.transform.SetParent(mapContainer.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.08f, 0.05f);
        
        // Map area with grid pattern
        var mapObj = new GameObject("MapImage");
        mapObj.transform.SetParent(mapContainer.transform, false);
        var mapRect = mapObj.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.05f, 0.18f);
        mapRect.anchorMax = new Vector2(0.95f, 0.82f);
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        mapImage = mapObj.AddComponent<RawImage>();
        // Create grid texture as fallback
        mapImage.texture = CreateGridTexture();
        mapImage.color = Color.white;
        
        // Markers container
        var markersObj = new GameObject("Markers");
        markersObj.transform.SetParent(mapObj.transform, false);
        markersContainer = markersObj.AddComponent<RectTransform>();
        markersContainer.anchorMin = new Vector2(0.5f, 0.5f);
        markersContainer.anchorMax = new Vector2(0.5f, 0.5f);
        markersContainer.sizeDelta = Vector2.zero;
        
        // Player marker
        playerMarker = CreatePlayerMarker();
        
        // Status text (top)
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(mapContainer.transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.85f);
        statusRect.anchorMax = new Vector2(1, 0.95f);
        statusRect.offsetMin = new Vector2(20, 0);
        statusRect.offsetMax = new Vector2(-20, 0);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Initializing...";
        statusText.fontSize = 32;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        
        // Debug text (below status)
        var debugObj = new GameObject("DebugText");
        debugObj.transform.SetParent(mapContainer.transform, false);
        var debugRect = debugObj.AddComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0, 0.95f);
        debugRect.anchorMax = new Vector2(1, 1f);
        debugRect.offsetMin = new Vector2(10, 0);
        debugRect.offsetMax = new Vector2(-10, 0);
        debugText = debugObj.AddComponent<TextMeshProUGUI>();
        debugText.text = "";
        debugText.fontSize = 16;
        debugText.alignment = TextAlignmentOptions.Center;
        debugText.color = Color.yellow;
        
        // AR Button - BIG and centered
        CreateARButton();
        
        Debug.Log("[MapUI] UI created with EventSystem");
    }
    
    Texture2D CreateGridTexture()
    {
        int size = 512;
        var tex = new Texture2D(size, size);
        Color bgColor = new Color(0.15f, 0.22f, 0.15f);
        Color lineColor = new Color(0.25f, 0.35f, 0.25f);
        Color majorLine = new Color(0.35f, 0.45f, 0.35f);
        
        // Fill background
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = bgColor;
        
        // Draw grid
        int gridSize = 32;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (x % gridSize == 0 || y % gridSize == 0)
                    pixels[y * size + x] = lineColor;
                if (x % (gridSize * 4) == 0 || y % (gridSize * 4) == 0)
                    pixels[y * size + x] = majorLine;
            }
        }
        
        // Center crosshair
        int center = size / 2;
        for (int i = center - 20; i < center + 20; i++)
        {
            if (i >= 0 && i < size)
            {
                pixels[center * size + i] = Color.white;
                pixels[i * size + center] = Color.white;
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
    
    RectTransform CreatePlayerMarker()
    {
        var marker = new GameObject("PlayerMarker");
        marker.transform.SetParent(markersContainer, false);
        var rect = marker.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);
        rect.anchoredPosition = Vector2.zero;
        
        // Blue circle
        var circle = new GameObject("Circle");
        circle.transform.SetParent(marker.transform, false);
        var circleRect = circle.AddComponent<RectTransform>();
        circleRect.sizeDelta = new Vector2(40, 40);
        var circleImg = circle.AddComponent<Image>();
        circleImg.color = new Color(0.2f, 0.5f, 1f);
        
        // White border
        var border = new GameObject("Border");
        border.transform.SetParent(marker.transform, false);
        border.transform.SetAsFirstSibling();
        var borderRect = border.AddComponent<RectTransform>();
        borderRect.sizeDelta = new Vector2(48, 48);
        var borderImg = border.AddComponent<Image>();
        borderImg.color = Color.white;
        
        return rect;
    }
    
    void CreateARButton()
    {
        var btnObj = new GameObject("ARButton");
        btnObj.transform.SetParent(mapContainer.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.1f, 0.04f);
        btnRect.anchorMax = new Vector2(0.9f, 0.14f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.1f, 0.7f, 0.2f);
        btnImg.raycastTarget = true;
        
        arButton = btnObj.AddComponent<Button>();
        arButton.targetGraphic = btnImg;
        
        // Use explicit listener
        arButton.onClick.AddListener(() => {
            Debug.Log("[MapUI] *** AR BUTTON PRESSED ***");
            debugText.text = "Button pressed!";
            EnterARMode();
        });
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "TAP HERE FOR AR MODE";
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false; // Important: let clicks through to button
        
        Debug.Log("[MapUI] AR button created");
    }
    
    IEnumerator InitializeMap()
    {
        while (LocationService.Instance == null || !LocationService.Instance.IsRunning)
        {
            statusText.text = "Waiting for GPS...";
            yield return new WaitForSeconds(0.5f);
        }
        
        mapCenterLat = LocationService.Instance.Latitude;
        mapCenterLng = LocationService.Instance.Longitude;
        
        statusText.text = $"GPS: {mapCenterLat:F4}, {mapCenterLng:F4}";
        Debug.Log($"[MapUI] Location: {mapCenterLat}, {mapCenterLng}");
        
        if (createTestGhostNearby)
            CreateTestGhostNearby();
        
        // Try to load real map
        StartCoroutine(LoadMapTile());
    }
    
    void CreateTestGhostNearby()
    {
        double testLat = mapCenterLat + (testGhostDistance / 111320.0);
        double testLng = mapCenterLng + (testGhostDistance / (111320.0 * System.Math.Cos(mapCenterLat * Mathf.Deg2Rad)));
        
        var testData = new GhostData
        {
            id = 9999,
            name = "Test Ghost",
            personality = "Friendly",
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
            newGhost.GetComponent<GhostVisual>().Initialize(testData);
            Vector3 worldPos = LocationService.Instance.GeoToWorld(testLat, testLng);
            worldPos.y = 1.5f;
            newGhost.transform.position = worldPos;
            Debug.Log($"[MapUI] Test ghost at {testLat:F6}, {testLng:F6}");
        }
    }
    
    IEnumerator LoadMapTile()
    {
        string url = $"https://staticmap.openstreetmap.de/staticmap.php?center={mapCenterLat},{mapCenterLng}&zoom={mapZoom}&size=512x512&maptype=mapnik";
        Debug.Log($"[MapUI] Loading map: {url}");
        
        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            www.timeout = 15;
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (tex != null)
                {
                    mapImage.texture = tex;
                    Debug.Log("[MapUI] Map loaded!");
                    debugText.text = "Map loaded";
                }
            }
            else
            {
                Debug.LogWarning($"[MapUI] Map failed: {www.error}");
                debugText.text = $"Map: {www.error}";
            }
        }
    }
    
    void Update()
    {
        if (isARMode) return;
        
        UpdateGhostMarkers();
        UpdateStatus();
        
        // Manual touch detection as backup
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                debugText.text = $"Touch: {touch.position}";
                
                // Check if touch is in button area (bottom 15% of screen)
                if (touch.position.y < Screen.height * 0.15f)
                {
                    Debug.Log("[MapUI] Touch in button area - entering AR");
                    EnterARMode();
                }
            }
        }
    }
    
    void UpdateStatus()
    {
        if (statusText == null) return;
        
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        int count = 0;
        float nearest = float.MaxValue;
        
        foreach (var g in ghosts)
        {
            if (g.Data == null) continue;
            count++;
            float d = g.DistanceToPlayer();
            if (d < nearest) { nearest = d; selectedGhost = g; }
        }
        
        if (count == 0)
            statusText.text = "Searching for ghosts...";
        else
            statusText.text = $"Found {count} ghost(s) - Nearest: {nearest:F0}m\nTap ghost or button below";
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
            
            marker.anchoredPosition = GeoToMapPos(ghost.Data.location.lat, ghost.Data.location.lng);
            
            var label = marker.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{ghost.Data.name}\n{ghost.DistanceToPlayer():F0}m";
        }
        
        // Cleanup
        List<int> toRemove = new List<int>();
        foreach (var kvp in ghostMarkers)
            if (!activeIds.Contains(kvp.Key)) { Destroy(kvp.Value.gameObject); toRemove.Add(kvp.Key); }
        foreach (int id in toRemove)
            ghostMarkers.Remove(id);
        
        playerMarker.SetAsLastSibling();
    }
    
    RectTransform CreateGhostMarker(GhostVisual ghost)
    {
        var marker = new GameObject($"Ghost_{ghost.Data.id}");
        marker.transform.SetParent(markersContainer, false);
        var rect = marker.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(70, 90);
        
        // Cyan circle for ghost
        var circle = new GameObject("Circle");
        circle.transform.SetParent(marker.transform, false);
        var circleRect = circle.AddComponent<RectTransform>();
        circleRect.sizeDelta = new Vector2(40, 40);
        circleRect.anchoredPosition = new Vector2(0, 20);
        var circleImg = circle.AddComponent<Image>();
        circleImg.color = Color.cyan;
        
        // Tap area
        var tapImg = marker.AddComponent<Image>();
        tapImg.color = new Color(1, 1, 1, 0.01f); // Nearly invisible but raycastable
        tapImg.raycastTarget = true;
        var btn = marker.AddComponent<Button>();
        btn.targetGraphic = tapImg;
        btn.onClick.AddListener(() => {
            Debug.Log($"[MapUI] Ghost tapped: {ghost.Data.name}");
            debugText.text = $"Tapped: {ghost.Data.name}";
            selectedGhost = ghost;
            EnterARMode();
        });
        
        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(marker.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, -25);
        labelRect.sizeDelta = new Vector2(140, 50);
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = ghost.Data.name;
        label.fontSize = 14;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.cyan;
        label.raycastTarget = false;
        
        return rect;
    }
    
    public void EnterARMode()
    {
        Debug.Log("[MapUI] === ENTERING AR MODE ===");
        isARMode = true;
        
        if (mapContainer != null)
            mapContainer.SetActive(false);
        
        foreach (var g in FindObjectsByType<GhostVisual>(FindObjectsSortMode.None))
            foreach (var r in g.GetComponentsInChildren<Renderer>())
                r.enabled = true;
    }
    
    public void ExitARMode()
    {
        isARMode = false;
        if (mapContainer != null)
            mapContainer.SetActive(true);
    }
    
    Vector2 GeoToMapPos(double lat, double lng)
    {
        double dLat = lat - mapCenterLat;
        double dLng = lng - mapCenterLng;
        float x = (float)(dLng * 111320 * System.Math.Cos(mapCenterLat * Mathf.Deg2Rad) / metersPerPixel);
        float y = (float)(dLat * 111320 / metersPerPixel);
        return new Vector2(x, y);
    }
}
