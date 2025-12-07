using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapUI : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private RectTransform mapContainer;
    [SerializeField] private float mapScale = 0.01f; // meters to pixels
    [SerializeField] private float mapRadius = 500f; // meters shown on map
    
    [Header("Markers")]
    [SerializeField] private GameObject playerMarkerPrefab;
    [SerializeField] private GameObject ghostMarkerPrefab;
    [SerializeField] private Color playerColor = Color.blue;
    [SerializeField] private Color ghostColor = Color.cyan;
    [SerializeField] private Color nearbyGhostColor = Color.green;
    
    [Header("Interaction")]
    [SerializeField] private float proximityThreshold = 50f; // Switch to AR mode
    
    private RectTransform playerMarker;
    private Dictionary<int, RectTransform> ghostMarkers = new Dictionary<int, RectTransform>();
    private bool isARMode = false;
    
    public bool IsARMode => isARMode;
    public System.Action<GhostVisual> OnEnterARMode;
    public System.Action OnExitARMode;
    
    void Start()
    {
        CreatePlayerMarker();
    }
    
    void CreatePlayerMarker()
    {
        if (mapContainer == null) return;
        
        GameObject marker = playerMarkerPrefab != null 
            ? Instantiate(playerMarkerPrefab, mapContainer) 
            : CreateDefaultMarker(playerColor);
        
        playerMarker = marker.GetComponent<RectTransform>();
        playerMarker.anchoredPosition = Vector2.zero; // Player always at center
    }
    
    GameObject CreateDefaultMarker(Color color)
    {
        GameObject obj = new GameObject("Marker");
        obj.transform.SetParent(mapContainer, false);
        
        Image img = obj.AddComponent<Image>();
        img.color = color;
        
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20, 20);
        
        return obj;
    }
    
    void Update()
    {
        UpdateGhostMarkers();
        CheckProximity();
    }
    
    void UpdateGhostMarkers()
    {
        if (mapContainer == null) return;
        
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        HashSet<int> activeIds = new HashSet<int>();
        
        Vector3 playerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        
        foreach (var ghost in ghosts)
        {
            if (ghost.Data == null) continue;
            
            int id = ghost.Data.id;
            activeIds.Add(id);
            
            // Get or create marker
            if (!ghostMarkers.TryGetValue(id, out RectTransform marker))
            {
                GameObject obj = ghostMarkerPrefab != null
                    ? Instantiate(ghostMarkerPrefab, mapContainer)
                    : CreateDefaultMarker(ghostColor);
                marker = obj.GetComponent<RectTransform>();
                ghostMarkers[id] = marker;
            }
            
            // Position on map (relative to player)
            Vector3 offset = ghost.transform.position - playerPos;
            float dist = offset.magnitude;
            
            if (dist < mapRadius)
            {
                marker.gameObject.SetActive(true);
                Vector2 mapPos = new Vector2(offset.x, offset.z) * mapScale * (mapContainer.rect.width / 2f) / mapRadius;
                marker.anchoredPosition = mapPos;
                
                // Color based on proximity
                Image img = marker.GetComponent<Image>();
                if (img != null)
                    img.color = dist < proximityThreshold ? nearbyGhostColor : ghostColor;
            }
            else
            {
                marker.gameObject.SetActive(false);
            }
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
    
    void CheckProximity()
    {
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        GhostVisual nearest = null;
        float nearestDist = float.MaxValue;
        
        foreach (var ghost in ghosts)
        {
            float dist = ghost.DistanceToPlayer();
            if (dist < nearestDist)
            {
                nearest = ghost;
                nearestDist = dist;
            }
        }
        
        // Auto switch to AR mode when close
        if (!isARMode && nearestDist < proximityThreshold)
        {
            isARMode = true;
            OnEnterARMode?.Invoke(nearest);
        }
        else if (isARMode && nearestDist > proximityThreshold * 1.5f)
        {
            isARMode = false;
            OnExitARMode?.Invoke();
        }
    }
    
    public void SetMapVisible(bool visible)
    {
        if (mapContainer != null)
            mapContainer.gameObject.SetActive(visible);
    }
}
