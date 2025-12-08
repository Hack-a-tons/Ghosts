using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
        // Ensure EventSystem exists with NEW Input System module
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>(); // NEW Input System!
            Debug.Log("[MapUI] Created EventSystem with InputSystemUIInputModule");
        }
        else
        {
            // Check if existing EventSystem has correct module
            var existingES = FindFirstObjectByType<EventSystem>();
            if (existingES.GetComponent<InputSystemUIInputModule>() == null)
            {
                // Remove old module if present
                var oldModule = existingES.GetComponent<StandaloneInputModule>();
                if (oldModule != null) Destroy(oldModule);
                existingES.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[MapUI] Added InputSystemUIInputModule to existing EventSystem");
            }
        }
        
        CreateUI();
        StartCoroutine(InitializeMap());
    }
    
    void CreateUI()
    {
        // Canvas
        var canvasObj = new GameObject("MapCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
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
        
        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(mapContainer.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.05f);
        
        // Map area
        var mapObj = new GameObject("MapImage");
        mapObj.transform.SetParent(mapContainer.transform, false);
        var mapRect = mapObj.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.05f, 0.18f);
        mapRect.anchorMax = new Vector2(0.95f, 0.82f);
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        mapImage = mapObj.AddComponent<RawImage>();
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
        
        // Status text
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
        
        // Debug text
        var debugObj = new GameObject("DebugText");
        debugObj.transform.SetParent(mapContainer.transform, false);
        var debugRect = debugObj.AddComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0, 0.95f);
        debugRect.anchorMax = new Vector2(1, 1f);
        debugRect.offsetMin = new Vector2(10, 0);
        debugRect.offsetMax = new Vector2(-10, 0);
        debugText = debugObj.AddComponent<TextMeshProUGUI>();
        debugText.text = "Tap the green button below";
        debugText.fontSize = 18;
        debugText.alignment = TextAlignmentOptions.Center;
        debugText.color = Color.yellow;
        
        // AR Button
        CreateARButton();
        
        Debug.Log("[MapUI] UI created");
    }
    
    Texture2D CreateGridTexture()
    {
        int size = 512;
        var tex = new Texture2D(size, size);
        Color bgColor = new Color(0.15f, 0.22f, 0.15f);
        Color lineColor = new Color(0.25f, 0.35f, 0.25f);
        
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;
        
        int gridSize = 32;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (x % gridSize == 0 || y % gridSize == 0)
                    pixels[y * size + x] = lineColor;
        
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
        
        var border = new GameObject("Border");
        border.transform.SetParent(marker.transform, false);
        border.AddComponent<RectTransform>().sizeDelta = new Vector2(48, 48);
        border.AddComponent<Image>().color = Color.white;
        
        var circle = new GameObject("Circle");
        circle.transform.SetParent(marker.transform, false);
        circle.AddComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
        circle.AddComponent<Image>().color = new Color(0.2f, 0.5f, 1f);
        
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
        arButton.onClick.AddListener(OnARButtonPressed);
        
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
        text.raycastTarget = false;
    }
    
    void OnARButtonPressed()
    {
        Debug.Log("[MapUI] *** BUTTON PRESSED ***");
        if (debugText != null) debugText.text = "Button pressed!";
        EnterARMode();
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
        
        if (createTestGhostNearby) CreateTestGhostNearby();
        StartCoroutine(LoadMapTile());
    }
    
    void CreateTestGhostNearby()
    {
        double testLat = mapCenterLat + (testGhostDistance / 111320.0);
        double testLng = mapCenterLng + (testGhostDistance / (111320.0 * System.Math.Cos(mapCenterLat * Mathf.Deg2Rad)));
        
        var testData = new GhostData
        {
            id = 9999, name = "Test Ghost", personality = "Friendly", visibility_radius_m = 100,
            location = new GhostLocation { lat = (float)testLat, lng = (float)testLng },
            interaction = new GhostInteraction { type = "riddle_unlock", riddle = "What has hands but can't clap?", correct_answer = "clock", reward = new GhostReward { type = "points", value = "100" } }
        };
        
        var existingGhost = FindFirstObjectByType<GhostVisual>();
        if (existingGhost != null)
        {
            var newGhost = Instantiate(existingGhost.gameObject);
            newGhost.name = "TestGhost_Nearby";
            newGhost.GetComponent<GhostVisual>().Initialize(testData);
            newGhost.transform.position = new Vector3(LocationService.Instance.GeoToWorld(testLat, testLng).x, 1.5f, LocationService.Instance.GeoToWorld(testLat, testLng).z);
        }
    }
    
    IEnumerator LoadMapTile()
    {
        string url = $"https://staticmap.openstreetmap.de/staticmap.php?center={mapCenterLat},{mapCenterLng}&zoom={mapZoom}&size=512x512&maptype=mapnik";
        
        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            www.timeout = 15;
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (tex != null) { mapImage.texture = tex; debugText.text = "Map loaded"; }
            }
            else
            {
                debugText.text = $"Map: {www.error}";
            }
        }
    }
    
    void Update()
    {
        if (isARMode) return;
        UpdateGhostMarkers();
        UpdateStatus();
        
        // New Input System touch detection
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 pos = Touchscreen.current.primaryTouch.position.ReadValue();
            debugText.text = $"Touch: {pos.x:F0}, {pos.y:F0}";
            
            // Bottom 15% = AR button area
            if (pos.y < Screen.height * 0.15f)
            {
                Debug.Log("[MapUI] Touch in button area");
                EnterARMode();
            }
        }
    }
    
    void UpdateStatus()
    {
        if (statusText == null) return;
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        int count = 0; float nearest = float.MaxValue;
        foreach (var g in ghosts)
        {
            if (g.Data == null) continue;
            count++;
            float d = g.DistanceToPlayer();
            if (d < nearest) { nearest = d; selectedGhost = g; }
        }
        statusText.text = count == 0 ? "Searching..." : $"{count} ghost(s) - Nearest: {nearest:F0}m";
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
            if (label != null) label.text = $"{ghost.Data.name}\n{ghost.DistanceToPlayer():F0}m";
        }
        
        List<int> toRemove = new List<int>();
        foreach (var kvp in ghostMarkers)
            if (!activeIds.Contains(kvp.Key)) { Destroy(kvp.Value.gameObject); toRemove.Add(kvp.Key); }
        foreach (int id in toRemove) ghostMarkers.Remove(id);
        playerMarker.SetAsLastSibling();
    }
    
    RectTransform CreateGhostMarker(GhostVisual ghost)
    {
        var marker = new GameObject($"Ghost_{ghost.Data.id}");
        marker.transform.SetParent(markersContainer, false);
        var rect = marker.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(70, 90);
        
        var circle = new GameObject("Circle");
        circle.transform.SetParent(marker.transform, false);
        var cRect = circle.AddComponent<RectTransform>();
        cRect.sizeDelta = new Vector2(40, 40);
        cRect.anchoredPosition = new Vector2(0, 20);
        circle.AddComponent<Image>().color = Color.cyan;
        
        var tapImg = marker.AddComponent<Image>();
        tapImg.color = new Color(1, 1, 1, 0.01f);
        tapImg.raycastTarget = true;
        var btn = marker.AddComponent<Button>();
        btn.targetGraphic = tapImg;
        btn.onClick.AddListener(() => { debugText.text = $"Tapped: {ghost.Data.name}"; selectedGhost = ghost; EnterARMode(); });
        
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(marker.transform, false);
        var lRect = labelObj.AddComponent<RectTransform>();
        lRect.anchoredPosition = new Vector2(0, -25);
        lRect.sizeDelta = new Vector2(140, 50);
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
        if (mapContainer != null) mapContainer.SetActive(false);
        foreach (var g in FindObjectsByType<GhostVisual>(FindObjectsSortMode.None))
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.enabled = true;
    }
    
    public void ExitARMode()
    {
        isARMode = false;
        if (mapContainer != null) mapContainer.SetActive(true);
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
