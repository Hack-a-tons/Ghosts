using UnityEngine;
using TMPro;

public class GhostVisual : MonoBehaviour
{
    public GhostData Data { get; private set; }
    
    [SerializeField] private TextMeshPro nameLabel;
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float baseAlpha = 0.4f; // More transparent
    
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
        
        // Set initial transparency
        SetAlpha(baseAlpha);
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
        
        // Pulse transparency
        float pulse = baseAlpha + 0.1f * Mathf.Sin(Time.time * 2f);
        SetAlpha(pulse);
    }
    
    void SetAlpha(float alpha)
    {
        if (renderers == null) return;
        
        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_Color"))
            {
                Color c = r.material.color;
                c.a = alpha;
                r.material.color = c;
            }
            if (r.material.HasProperty("_BaseColor"))
            {
                Color c = r.material.GetColor("_BaseColor");
                c.a = alpha;
                r.material.SetColor("_BaseColor", c);
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
