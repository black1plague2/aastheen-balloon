using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Attach to UI_Canvas. On Start, converts to WorldSpace and positions in front of the player.
// Fixes the 'UI too high / invisible' issue with EyeLevel tracking origin.
[RequireComponent(typeof(Canvas))]
public class HUDCanvasSetup : MonoBehaviour
{
    public float forwardDistance = 2.5f;
    public float verticalOffset  = 0.4f;
    public float canvasScale     = 0.002f;

    private void Start()
    {
        StartCoroutine(SetupNextFrame());
    }

    private IEnumerator SetupNextFrame()
    {
        yield return null;

        Camera cam = Camera.main;
        if (cam == null) yield break;

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        Vector3 flat = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z);
        if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
        flat.Normalize();

        transform.position = cam.transform.position
            + flat * forwardDistance
            + Vector3.down * verticalOffset;

        transform.rotation = Quaternion.LookRotation(
            transform.position - cam.transform.position);

        transform.localScale = Vector3.one * canvasScale;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null) scaler.enabled = false;
    }
}
