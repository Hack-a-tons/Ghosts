using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GhostAPI : MonoBehaviour
{
    public static GhostAPI Instance { get; private set; }
    
    [SerializeField] private string apiUrl = "https://ghosts.api.app.hurated.com";
    [SerializeField] private float positionReportInterval = 5f;
    
    private string clientId;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        clientId = SystemInfo.deviceUniqueIdentifier;
    }
    
    void Start()
    {
        StartCoroutine(ReportPositionLoop());
    }
    
    public void GetNearbyGhosts(double lat, double lng, float radius, Action<GhostData[]> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(FetchGhosts(lat, lng, radius, onSuccess, onError));
    }
    
    IEnumerator FetchGhosts(double lat, double lng, float radius, Action<GhostData[]> onSuccess, Action<string> onError)
    {
        string url = $"{apiUrl}/api/ghosts?lat={lat}&lng={lng}&radius={radius}&client_id={clientId}";
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = "{\"ghosts\":" + req.downloadHandler.text + "}";
                var response = JsonUtility.FromJson<GhostListResponse>(json);
                onSuccess?.Invoke(response.ghosts);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }
    
    public void GetGhost(int id, Action<GhostData> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(FetchGhost(id, onSuccess, onError));
    }
    
    IEnumerator FetchGhost(int id, Action<GhostData> onSuccess, Action<string> onError)
    {
        string url = $"{apiUrl}/api/ghosts/{id}";
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                var ghost = JsonUtility.FromJson<GhostData>(req.downloadHandler.text);
                onSuccess?.Invoke(ghost);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }
    
    public void ReportInteraction(int ghostId, string action)
    {
        StartCoroutine(PostInteraction(ghostId, action));
    }
    
    IEnumerator PostInteraction(int ghostId, string action)
    {
        string url = $"{apiUrl}/api/ghosts/{ghostId}/interact";
        string json = $"{{\"action\":\"{action}\",\"client_id\":\"{clientId}\"}}";
        
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
        }
    }
    
    public void ReportPosition(double lat, double lng)
    {
        StartCoroutine(PostPosition(lat, lng));
    }
    
    IEnumerator PostPosition(double lat, double lng)
    {
        string url = $"{apiUrl}/api/players/position";
        string json = $"{{\"client_id\":\"{clientId}\",\"lat\":{lat},\"lng\":{lng}}}";
        
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
        }
    }
    
    IEnumerator ReportPositionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(positionReportInterval);
            
            var loc = LocationService.Instance;
            if (loc != null && loc.IsRunning)
            {
                ReportPosition(loc.Latitude, loc.Longitude);
            }
        }
    }
}
