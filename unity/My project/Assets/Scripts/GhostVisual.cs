using UnityEngine;
using TMPro;

public class GhostVisual : MonoBehaviour
{
    public GhostData Data { get; private set; }
    
    [SerializeField] private TextMeshPro nameLabel;
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float fadeDistance = 5f;
    
    private Vector3 startPos;
    private Renderer[] renderers;
    private Transform cameraTransform;
    
    public void Initialize(GhostData data)
    {
        Data = data;
        startPos = transform.position;
        renderers = GetComponentsInChildren<Renderer>();
        cameraTransform = Camera.main?.transform;
        
        if (nameLabel != null)
            nameLabel.text = data.name;
        
        gameObject.name = $"Ghost_{data.id}_{data.name}";
    }
    
    public void UpdateData(GhostData data)
    {
        Data = data;
        if (nameLabel != null)
            nameLabel.text = data.name;
    }
    
    void Update()
    {
        // Floating animation
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        
        // Face camera
        if (cameraTransform != null)
        {
            Vector3 lookDir = cameraTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(-lookDir);
        }
        
        // Distance-based fade
        UpdateFade();
    }
    
    void UpdateFade()
    {
        if (cameraTransform == null || renderers == null) return;
        
        float dist = Vector3.Distance(transform.position, cameraTransform.position);
        float alpha = Mathf.Clamp01(1f - (dist - Data.visibility_radius_m) / fadeDistance);
        
        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_Color"))
            {
                Color c = r.material.color;
                c.a = alpha;
                r.material.color = c;
            }
        }
    }
    
    public float DistanceToPlayer()
    {
        if (cameraTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, cameraTransform.position);
    }
    
    public bool IsInRange()
    {
        return DistanceToPlayer() <= Data.visibility_radius_m;
    }
}
