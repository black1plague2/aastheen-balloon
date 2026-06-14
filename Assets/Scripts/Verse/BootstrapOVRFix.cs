using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Injected at Bootstrap scene start to fix missing controller/hand rendering
/// and ensure OVRRaycaster has a valid camera. Attach to the _Bootstrap GameObject.
/// </summary>
[DefaultExecutionOrder(-100)]
public class BootstrapOVRFix : MonoBehaviour
{
    void Awake()
    {
        FixCanvasCamera();
        AddControllerHelpers();
    }

    // World-space canvas needs a camera for depth sorting and OVRRaycaster to work.
    void FixCanvasCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            // OVRCameraRig's center eye camera isn't tagged MainCamera by default.
            // Find it by name instead.
            var go = GameObject.Find("CenterEyeAnchor");
            if (go != null) cam = go.GetComponent<Camera>();
        }
        if (cam == null) return;

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                canvas.worldCamera = cam;
                Debug.Log($"[BootstrapOVRFix] Set canvas camera: {canvas.name}");
            }
        }
    }

    // Add OVRControllerHelper to left/right controller anchors so the
    // 3D controller models appear and the laser pointer is visible.
    void AddControllerHelpers()
    {
        TryAddHelper("LeftControllerAnchor",  OVRInput.Controller.LTouch);
        TryAddHelper("RightControllerAnchor", OVRInput.Controller.RTouch);
    }

    void TryAddHelper(string anchorName, OVRInput.Controller controller)
    {
        var go = GameObject.Find(anchorName);
        if (go == null)
        {
            Debug.LogWarning($"[BootstrapOVRFix] {anchorName} not found");
            return;
        }

        // Don't add twice
        if (go.GetComponent<OVRControllerHelper>() != null) return;

        var helper = go.AddComponent<OVRControllerHelper>();
        // m_controller is a public field on OVRControllerHelper
        helper.m_controller = controller;
        Debug.Log($"[BootstrapOVRFix] Added OVRControllerHelper to {anchorName}");
    }
}
