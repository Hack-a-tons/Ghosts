using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }
    
    public enum Mode { Map, AR }
    public Mode CurrentMode { get; private set; } = Mode.Map;
    
    [Header("References")]
    [SerializeField] private GameObject mapUI;
    [SerializeField] private GameObject arSession;
    [SerializeField] private Camera arCamera;
    [SerializeField] private GhostManager ghostManager;
    
    [Header("Settings")]
    [SerializeField] private float arTriggerDistance = 50f;
    [SerializeField] private float arExitDistance = 75f;
    
    private GhostVisual targetGhost;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
    
    void Start()
    {
        // Start in map mode
        SetMode(Mode.Map);
    }
    
    void Update()
    {
        if (CurrentMode == Mode.Map)
            CheckForNearbyGhost();
        else
            CheckForExitAR();
    }
    
    void CheckForNearbyGhost()
    {
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        foreach (var ghost in ghosts)
        {
            if (ghost.DistanceToPlayer() < arTriggerDistance)
            {
                targetGhost = ghost;
                SetMode(Mode.AR);
                return;
            }
        }
    }
    
    void CheckForExitAR()
    {
        if (targetGhost == null || targetGhost.DistanceToPlayer() > arExitDistance)
        {
            SetMode(Mode.Map);
        }
    }
    
    public void SetMode(Mode mode)
    {
        CurrentMode = mode;
        
        bool isAR = mode == Mode.AR;
        
        if (mapUI != null) mapUI.SetActive(!isAR);
        if (arSession != null) arSession.SetActive(isAR);
        
        // Hide ghosts in map mode, show in AR
        var ghosts = FindObjectsByType<GhostVisual>(FindObjectsSortMode.None);
        foreach (var ghost in ghosts)
        {
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.enabled = isAR;
        }
        
        Debug.Log($"[GameModeManager] Switched to {mode} mode");
    }
    
    // Manual toggle for testing
    public void ToggleMode()
    {
        SetMode(CurrentMode == Mode.Map ? Mode.AR : Mode.Map);
    }
}
