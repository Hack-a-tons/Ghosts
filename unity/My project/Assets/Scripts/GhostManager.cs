using System.Collections.Generic;
using UnityEngine;

public class GhostManager : MonoBehaviour
{
    public static GhostManager Instance { get; private set; }
    
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private float fetchRadius = 1000f;
    [SerializeField] private float refreshInterval = 30f;
    
    private Dictionary<int, GhostVisual> activeGhosts = new Dictionary<int, GhostVisual>();
    private float lastFetchTime;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Start()
    {
        RefreshGhosts();
    }
    
    void Update()
    {
        if (Time.time - lastFetchTime > refreshInterval)
        {
            RefreshGhosts();
        }
    }
    
    public void RefreshGhosts()
    {
        lastFetchTime = Time.time;
        var loc = LocationService.Instance;
        if (loc == null || !loc.IsRunning) return;
        
        GhostAPI.Instance.GetNearbyGhosts(
            loc.Latitude, loc.Longitude, fetchRadius,
            OnGhostsReceived,
            error => Debug.LogError($"Failed to fetch ghosts: {error}")
        );
    }
    
    void OnGhostsReceived(GhostData[] ghosts)
    {
        HashSet<int> receivedIds = new HashSet<int>();
        
        foreach (var ghost in ghosts)
        {
            receivedIds.Add(ghost.id);
            
            if (!activeGhosts.ContainsKey(ghost.id))
            {
                SpawnGhost(ghost);
            }
            else
            {
                activeGhosts[ghost.id].UpdateData(ghost);
            }
        }
        
        // Remove ghosts no longer in range
        List<int> toRemove = new List<int>();
        foreach (var id in activeGhosts.Keys)
        {
            if (!receivedIds.Contains(id))
                toRemove.Add(id);
        }
        foreach (var id in toRemove)
        {
            Destroy(activeGhosts[id].gameObject);
            activeGhosts.Remove(id);
        }
    }
    
    void SpawnGhost(GhostData data)
    {
        if (ghostPrefab == null) return;
        
        Vector3 worldPos = LocationService.Instance.GeoToWorld(data.location.lat, data.location.lng);
        GameObject obj = Instantiate(ghostPrefab, worldPos, Quaternion.identity);
        
        var visual = obj.GetComponent<GhostVisual>();
        if (visual == null) visual = obj.AddComponent<GhostVisual>();
        
        visual.Initialize(data);
        activeGhosts[data.id] = visual;
    }
    
    public GhostVisual GetGhost(int id)
    {
        activeGhosts.TryGetValue(id, out var ghost);
        return ghost;
    }
}
