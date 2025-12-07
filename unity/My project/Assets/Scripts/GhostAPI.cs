using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GhostAPI : MonoBehaviour
{
    public static GhostAPI Instance { get; private set; }
    
    [SerializeField] private string apiUrl = "https://ghosts.api.app.hurated.com";
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void GetNearbyGhosts(double lat, double lng, float radius, Action<GhostData[]> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(FetchGhosts(lat, lng, radius, onSuccess, onError));
    }
    
    IEnumerator FetchGhosts(double lat, double lng, float radius, Action<GhostData[]> onSuccess, Action<string> onError)
    {
        string url = $"{apiUrl}/api/ghosts?lat={lat}&lng={lng}&radius={radius}";
        
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
}
