using System.Collections;
using UnityEngine;

public class LocationService : MonoBehaviour
{
    public static LocationService Instance { get; private set; }
    
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public bool IsRunning { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private float desiredAccuracy = 5f;
    
    [Header("Debug/Quest Mode")]
    [SerializeField] private bool useDebugLocation = false; // Auto-detect: true for Quest, false for iOS
    [SerializeField] private double debugLatitude = 37.7749;
    [SerializeField] private double debugLongitude = -122.4194;
    [SerializeField] private bool autoDetectPlatform = true;
    
    // Reference point for world coordinate conversion
    private double refLat;
    private double refLng;
    private bool hasReference;
    
    private const double EARTH_RADIUS = 6371000; // meters
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        // Auto-detect platform
        if (autoDetectPlatform)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Quest doesn't have GPS, use debug
            useDebugLocation = true;
            #elif UNITY_IOS && !UNITY_EDITOR
            // iOS has GPS, use real location
            useDebugLocation = false;
            #endif
        }
        
        if (useDebugLocation)
        {
            // Use debug coordinates (for Quest or testing)
            Latitude = debugLatitude;
            Longitude = debugLongitude;
            refLat = Latitude;
            refLng = Longitude;
            hasReference = true;
            IsRunning = true;
            Debug.Log($"[LocationService] Debug mode: {Latitude}, {Longitude}");
        }
        else
        {
            StartCoroutine(StartLocationService());
        }
    }
    
    IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("Location services disabled, using debug location");
            UseDebugFallback();
            yield break;
        }
        
        Input.location.Start(desiredAccuracy, desiredAccuracy);
        
        int timeout = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
        {
            yield return new WaitForSeconds(1);
            timeout--;
        }
        
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Location service failed, using debug location");
            UseDebugFallback();
            yield break;
        }
        
        IsRunning = true;
        StartCoroutine(UpdateLocation());
    }
    
    void UseDebugFallback()
    {
        Latitude = debugLatitude;
        Longitude = debugLongitude;
        refLat = Latitude;
        refLng = Longitude;
        hasReference = true;
        IsRunning = true;
    }
    
    IEnumerator UpdateLocation()
    {
        while (IsRunning)
        {
            var loc = Input.location.lastData;
            Latitude = loc.latitude;
            Longitude = loc.longitude;
            
            if (!hasReference)
            {
                refLat = Latitude;
                refLng = Longitude;
                hasReference = true;
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
    }
    
    public Vector3 GeoToWorld(double lat, double lng)
    {
        if (!hasReference)
        {
            refLat = lat;
            refLng = lng;
            hasReference = true;
        }
        
        double dLat = (lat - refLat) * Mathf.Deg2Rad;
        double dLng = (lng - refLng) * Mathf.Deg2Rad;
        
        float x = (float)(dLng * EARTH_RADIUS * System.Math.Cos(refLat * Mathf.Deg2Rad));
        float z = (float)(dLat * EARTH_RADIUS);
        
        return new Vector3(x, 0, z);
    }
    
    public void SetReferencePoint(double lat, double lng)
    {
        refLat = lat;
        refLng = lng;
        hasReference = true;
    }
    
    void OnDestroy()
    {
        if (Input.location.status == LocationServiceStatus.Running)
            Input.location.Stop();
    }
}
