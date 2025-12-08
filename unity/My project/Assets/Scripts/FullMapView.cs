using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FullMapView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage mapImage;
    [SerializeField] private RectTransform markersContainer;
    [SerializeField] private GameObject ghostMarkerPrefab;
    [SerializeField] private GameObject playerMarkerPrefab;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button refreshButton;
    
    [Header("Map Settings")]
    [SerializeField] private int mapZoom = 15;
    [SerializeField] private int mapWidth = 640;
    [SerializeField] private int mapHeight = 640;
    [SerializeField] private float metersPerPixel = 2f; // Approximate at zoom 15
    
    [Header("Colors")]
    [SerializeField] private Color playerColor = Color.blue;
    [SerializeField] private Color ghostColor = new Color(0, 1, 1, 0.8f);
    [SerializeField] private Color nearbyColor = Color.green;
    [SerializeField] private float nearbyThreshold = 100f;
    
    private RectTransform playerMarker;
    private Dictionary<int, RectTransform> ghostMarkers = new Dictionary<int, RectTransform>();
    private double mapCenterLat, mapCenterLng;
    
    void Start()
    {
        CreatePlayerMarker();
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshMap);
        
        StartCoroutine(InitializeMap());
    }
    
    IEnumerator InitializeMap()
    {
        // Wait for location
        while (LocationService.Instance == null || !LocationService.Instance.IsRunning)
            yield return new WaitForSeconds(0.5f);
        
        RefreshMap();
    }
    
    void CreatePlayerMarker()
    {
        if (markersContainer == null) return;
        
        GameObject marker;
        if (playerMarkerPrefab != null)
        {
            marker = Instantiate(playerMarkerPrefab, markersContainer);
        }
        else
        {
            marker = new GameObject("PlayerMarker");
            marker.transform.SetParent(markersContainer, false);
            var img = marker.AddComponent<Image>();
            img.color = playerColor;
        }
        
        playerMarker = marker.GetComponent<RectTransform>();
        playerMarker.sizeDelta = new Vector2(24, 24);
        playerMarker.anchoredPosition = Vector2.zero; // Center
    }
    
    public void RefreshMap()
    {
        if (LocationService.Instance == null || !LocationService.Instance.IsRunning) return;
        
        mapCenterLat = LocationService.Instance.Latitude;
        mapCenterLng = LocationService.Instance.Longitude;
        
        UpdateStatus($"Location: {mapCenterLat:F4}, {mapCenterLng:F4}");
        
        // Load map tile (using OpenStreetMap static tiles)
        StartCoroutine(LoadMapTile());
    }
    
    IEnumerator LoadMapTile()
    {
        // OpenStreetMap static tile URL
        string url = $"https://staticmap.openstreetmap.de/staticmap.php?center={mapCenterLat},{mapCenterLng}&zoom={mapZoom}&size={mapWidth}x{mapHeight}&maptype=mapnik";
        
        using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (mapImage != null)
                    mapImage.texture = texture;
            }
            else
            {
                Debug.LogWarning($"[FullMapView] Failed to load map: {www.error}");
                // Use solid color fallback
                if (mapImage != null)
                    mapImage.color = new Color(0.2f, 0.3f, 0.2f);
            }
        }
    }
    
    void Update()
    {
        UpdateGhostMarkers();
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
            
            // Convert geo to screen position
            Vector2 screenPos = GeoToScreenPos(ghost.Data.location.lat, ghost.Data.location.lng);
            marker.anchoredPosition = screenPos;
            
            // Update color based on distance
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
        GameObject marker;
        if (ghostMarkerPrefab != null)
        {
            marker = Instantiate(ghostMarkerPrefab, markersContainer);
        }
        else
        {
            marker = new GameObject($"Ghost_{name}");
            marker.transform.SetParent(markersContainer, false);
            
            var img = marker.AddComponent<Image>();
            img.color = ghostColor;
            
            // Add label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(marker.transform, false);
            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = name;
            label.fontSize = 12;
            label.alignment = TextAlignmentOptions.Center;
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -20);
            labelRect.sizeDelta = new Vector2(100, 30);
        }
        
        var rect = marker.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20, 20);
        return rect;
    }
    
    Vector2 GeoToScreenPos(double lat, double lng)
    {
        // Convert lat/lng offset to pixels
        double dLat = lat - mapCenterLat;
        double dLng = lng - mapCenterLng;
        
        // Approximate conversion (varies by latitude)
        double metersPerDegreeLat = 111320;
        double metersPerDegreeLng = 111320 * System.Math.Cos(mapCenterLat * Mathf.Deg2Rad);
        
        float x = (float)(dLng * metersPerDegreeLng / metersPerPixel);
        float y = (float)(dLat * metersPerDegreeLat / metersPerPixel);
        
        return new Vector2(x, y);
    }
    
    void UpdateStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}
