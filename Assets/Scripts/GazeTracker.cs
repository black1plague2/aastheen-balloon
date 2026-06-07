using UnityEngine;

public class GazeTracker : MonoBehaviour
{
    public static GazeTracker Instance;

    private int  leftFrames   = 0;
    private int  centerFrames = 0;
    private int  rightFrames  = 0;
    private int  totalFrames  = 0;
    private bool isTracking   = false;

    public float LeftGazeFraction
    {
        get { return totalFrames > 0 ? (float)leftFrames   / totalFrames : 0f; }
    }
    public float CenterGazeFraction
    {
        get { return totalFrames > 0 ? (float)centerFrames / totalFrames : 0f; }
    }
    public float RightGazeFraction
    {
        get { return totalFrames > 0 ? (float)rightFrames  / totalFrames : 0f; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartTracking()
    {
        leftFrames = centerFrames = rightFrames = totalFrames = 0;
        isTracking = true;
    }

    public void StopTracking()
    {
        isTracking = false;
    }

    private void Update()
    {
        if (!isTracking || Camera.main == null) return;

        Vector3 forward = Camera.main.transform.forward;
        Vector3 flat    = new Vector3(forward.x, 0f, forward.z);
        if (flat.sqrMagnitude < 0.001f) return;

        float angle = Vector3.SignedAngle(Vector3.forward, flat.normalized, Vector3.up);
        totalFrames++;
        if      (angle < -15f) leftFrames++;
        else if (angle >  15f) rightFrames++;
        else                   centerFrames++;
    }

    public void ResetTracking()
    {
        isTracking = false;
        leftFrames = centerFrames = rightFrames = totalFrames = 0;
    }
}
