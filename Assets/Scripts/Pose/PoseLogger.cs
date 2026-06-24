using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;

// Attach to any GameObject that has an XRHandPoseDetector.
// Fires OnPoseHeld after the pose is held continuously for holdDuration seconds.
// PoseGameManager listens to OnPoseHeld to count reps.
public class PoseLogger : MonoBehaviour
{
    [Header("Pose Identity")]
    public string poseName = "Custom Pose";
    public HandSide handSide = HandSide.Left;

    [Header("Hold Requirement")]
    [Tooltip("Seconds the pose must be held before it counts as a rep")]
    public float holdDuration = 3f;

    [Header("Visual Feedback")]
    public MeshRenderer visualFeedback;
    public Color detectedColor = Color.green;
    public Color holdingColor  = Color.yellow;
    public Color idleColor     = Color.white;

    public enum HandSide { Left, Right }

    // Fired when pose held for holdDuration — PoseGameManager subscribes
    public event System.Action<string> OnPoseHeld;

    private bool  _isHolding;
    private float _holdTimer;
    private bool  _alreadyFired;
    private Material _mat;

    void Start()
    {
        if (visualFeedback != null)
        {
            _mat = visualFeedback.material;
            _mat.color = idleColor;
        }
        Debug.Log($"[POSE] {poseName} ({handSide}) ready — hold {holdDuration}s to count");
    }

    // Called every frame by XRHandPoseDetector Unity Events
    public void OnPoseDetected()
    {
        if (!_isHolding)
        {
            _isHolding    = true;
            _holdTimer    = 0f;
            _alreadyFired = false;
            SetColor(holdingColor);
        }
    }

    public void OnPoseLost()
    {
        _isHolding = false;
        _holdTimer  = 0f;
        SetColor(idleColor);
    }

    public void UpdatePoseState(bool isDetected)
    {
        if (isDetected) OnPoseDetected(); else OnPoseLost();
    }

    void Update()
    {
        if (!_isHolding || _alreadyFired) return;

        _holdTimer += Time.deltaTime;
        if (_holdTimer >= holdDuration)
        {
            _alreadyFired = true;
            SetColor(detectedColor);
            Debug.Log($"[POSE] {poseName} held {holdDuration}s — COUNTED");
            OnPoseHeld?.Invoke(poseName);
        }
    }

    private void SetColor(Color c)
    {
        if (_mat != null) _mat.color = c;
    }
}
