using UnityEngine;

public class PinRotationDriver : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Transform rightHandAnchor;   // OVRCameraRig → TrackingSpace → RightHandAnchor

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (rightHandAnchor == null) return;

        transform.position      = rightHandAnchor.position;
        transform.localRotation = Quaternion.Euler(0f, 0f, GameManager.Instance.latestRotation);
    }
}
