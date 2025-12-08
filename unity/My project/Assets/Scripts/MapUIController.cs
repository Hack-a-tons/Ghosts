using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    
    private int mapZoom = 16;
    private float metersPerPixel = 2.4f;
    
    [SerializeField] private bool createTestGhostNearby = true;
    [SerializeField] private float testGhostDistance = 20f;
    
    private RectTransform playerMarker;
    private Dictionary<int, RectTransform> ghostMarkers = new Dictionary<int, RectTransform>();
    private double mapCenterLat, mapCenterLng;
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
        // Canvas
        var canvasObj = new GameObject("MapCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
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
        bg.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.08f);
        
        // Map area
        var mapObj = new GameObject("MapImage");
        mapObj.transform.SetParent(mapContainer.transform, false);
        var mapRect = mapObj.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.05f, 0.15f);
        mapRect.anchorMax = new Vector2(0.95f, 0.85f);
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        mapImage = mapObj.AddComponent<RawImage>();
        mapImage.color = new Color(0.15f, 0.2f, 0.15f);
        
        // Markers container
        var markersObj = new GameObject("Markers");
        markersObj.transform.SetParent(mapObj.transform, false);
        markersContainer = markersObj.AddComponent<RectTransform>();
        markersContainer.anchorMin = new Vector2(0.5f, 0.5f);
        markersContainer.anchorMax = new Vector2(0.5f, 0.5f);
        markersContainer.sizeDelta = Vector2.zero;
        
        // Player marker (blue circle with white border)
        playerMarker = CreatePlayerMarker();
        
        // Status text
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(mapContainer.transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.87f);
        statusRect.anchorMax = new Vector2(1, 0.97f);
        statusRect.offsetMin = new Vector2(20, 0);
        statusRect.offsetMax = new Vector2(-20, 0);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Initializing...";
        statusText.fontSize = 28;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        
        // AR Button
        CreateARButton();
        
        Debug.Log("[MapUI] UI created");
    }
    
    RectTransform CreatePlayerMarker()
    {
        var marker = new GameObject("PlayerMarker");
        marker.transform.SetParent(markersContainer, false);
        var rect = marker.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40, 40);
        rect.anchoredPosition = Vector2.zero;
        
        // White border circle
        var border = new GameObject("Border");
        border.transform.SetParent(marker.transform, false);
        var borderRect = border.AddComponent<RectTransform>();
        borderRect.sizeDelta = new Vector2(40, 40);
        var borderImg = border.AddComponent<Image>();
        borderImg.color = Color.white;
        
        // Blue inner circle
        var inner = new GameObject("Inner");
        inner.transform.SetParent(marker.transform, false);
        var innerRect = inner.AddComponent<RectTransform>();
        innerRect.sizeDelta = new Vector2(30, 30);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.2f, 0.4f, 1f);
        
        // "You" label
        var label = new GameObject("Label");
        label.transform.SetParent(marker.transform, false);
        var labelRect = label.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, -30);
        labelRect.sizeDelta = new Vector2(60, 25);
        var labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.text = "You";
        labelText.fontSize = 16;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        
        return rect;
    }
    
    void CreateARButton()
    {
        var btnObj = new GameObject("ARButton");
        btnObj.transform.SetParent(mapContainer.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.1f, 0.03f);
        btnRect.anchorMax = new Vector2(0.9f, 0.12f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.1f, 0.6f, 0.1f);
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(OnARButtonClick);
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "ENTER AR MODE";
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        
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
        
        statusText.text = $"Location: {mapCenterLat:F4}, {mapCenterLng:F4}";
        Debug.Log($"[MapUI] Got location: {mapCenterLat}, {mapCenterLng}");
        
        if (createTestGhostNearby)
            CreateTestGhostNearby();
        
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
            Debug.Log($"[MapUI] Test ghost created at {testLat:F6}, {testLng:F6}");
        }
    }
    
    IEnumerator LoadMapTile()
    {
        statusText.text = "Loading map...";
        
        string url = $"https://staticmap.openstreetmap.de/staticmap.php?center={mapCenterLat},{mapCenterLng}&zoom={mapZoom}&size=600x600&maptype=mapnik";
        Debug.Log($"[MapUI] Loading: {url}");
        
        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            www.timeout = 20;
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (tex != null)
                {
                    mapImage.texture = tex;
                    mapImage.color = Color.white;
                    Debug.Log("[MapUI] Map loaded OK");
                }
            }
            else
            {
                Debug.LogError($"[MapUI] Map FAILED: {www.error}");
                statusText.text = $"Map error: {www.error}";
            }
        }
    }
    
    void Update()
    {
        if (isARMode) return;
        UpdateGhostMarkers();
        UpdateStatus();
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
            statusText.text = $"Found {count} ghost(s) - Nearest: {nearest:F0}m";
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
            
            var label = marker.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{ghost.Data.name}\n{ghost.DistanceToPlayer():F0}m";
        }
        
        // Cleanup old
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
        rect.sizeDelta = new Vector2(50, 70);
        
        // Cyan circle
        var circle = new GameObject("Circle");
        circle.transform.SetParent(marker.transform, false);
        var circleRect = circle.AddComponent<RectTransform>();
        circleRect.sizeDelta = new Vector2(36, 36);
        circleRect.anchoredPosition = new Vector2(0, 15);
        var circleImg = circle.AddComponent<Image>();
        circleImg.color = Color.cyan;
        
        // Tap button (covers whole marker)
        var tapImg = marker.AddComponent<Image>();
        tapImg.color = new Color(0, 0, 0, 0);
        var btn = marker.AddComponent<Button>();
        btn.targetGraphic = tapImg;
        btn.onClick.AddListener(() => {
            Debug.Log($"[MapUI] Tapped ghost: {ghost.Data.name}");
            selectedGhost = ghost;
            EnterARMode();
        });
        
        // Label
        var label = new GameObject("Label");
        label.transform.SetParent(marker.transform, false);
        var labelRect = label.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, -20);
        labelRect.sizeDelta = new Vector2(120, 40);
        var labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.text = ghost.Data.name;
        labelText.fontSize = 12;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.cyan;
        
        return rect;
    }
    
    void OnARButtonClick()
    {
        Debug.Log("[MapUI] AR BUTTON CLICKED!");
        EnterARMode();
    }
    
    public void EnterARMode()
    {
        Debug.Log("[MapUI] ENTERING AR MODE");
        isARMode = true;
        mapContainer.SetActive(false);
        
        foreach (var g in FindObjectsByType<GhostVisual>(FindObjectsSortMode.None))
            foreach (var r in g.GetComponentsInChildren<Renderer>())
                r.enabled = true;
    }
    
    public void ExitARMode()
    {
        isARMode = false;
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
