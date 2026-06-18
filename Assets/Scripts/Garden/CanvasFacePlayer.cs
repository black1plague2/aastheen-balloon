using UnityEngine;

public class CanvasFacePlayer : MonoBehaviour
{
    private Transform playerCamera;

    void Start()
    {
        // Find the VR center eye camera
        GameObject cam =
            GameObject.Find("CenterEyeAnchor");

        if (cam != null)
            playerCamera = cam.transform;
        else
            Debug.LogWarning(
                "CenterEyeAnchor not found!"
            );
    }

    void Update()
    {
        if (playerCamera == null) return;

        transform.LookAt(
            transform.position
            + playerCamera.rotation * Vector3.forward,
            playerCamera.rotation * Vector3.up
        );
    }
}