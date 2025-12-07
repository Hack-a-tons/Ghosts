using UnityEngine;

public class PassthroughEnabler : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[GHOST_DEBUG] PassthroughEnabler starting...");
        
        // Try to find and enable OVRPassthroughLayer
        var passthroughLayer = FindFirstObjectByType<OVRPassthroughLayer>();
        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = true;
            Debug.Log("[GHOST_DEBUG] OVRPassthroughLayer found and enabled");
        }
        else
        {
            Debug.Log("[GHOST_DEBUG] OVRPassthroughLayer not found, trying to add...");
            
            // Find OVRCameraRig and add passthrough
            var cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null)
            {
                var pt = cameraRig.gameObject.AddComponent<OVRPassthroughLayer>();
                pt.overlayType = OVROverlay.OverlayType.Underlay;
                pt.compositionDepth = 0;
                Debug.Log("[GHOST_DEBUG] Added OVRPassthroughLayer to OVRCameraRig");
            }
        }
        
        // Set camera background to transparent/clear
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            Debug.Log("[GHOST_DEBUG] Camera set to transparent background");
        }
        
        // Enable passthrough via OVRManager
        var ovrManager = FindFirstObjectByType<OVRManager>();
        if (ovrManager != null)
        {
            ovrManager.isInsightPassthroughEnabled = true;
            Debug.Log("[GHOST_DEBUG] OVRManager passthrough enabled");
        }
    }
}
