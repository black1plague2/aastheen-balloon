using UnityEngine;

public class ForearmRotationTracker : MonoBehaviour
{
    [Header("Settings")]
    [Range(0f, 90f)]
    public float pronationThreshold = 45f;
    public float jerkyVelocityThreshold = 25f;
    public float smoothHoldDuration = 3f;

    // Public states other scripts read
    [HideInInspector] public bool isWatering = false;
    [HideInInspector] public bool isJerky = false;
    [HideInInspector] public bool triggerGrowth = false;
    [HideInInspector] public float stabilityValue = 1f;
    [HideInInspector] public float holdProgress = 0f;

    private CanGrabNotifier grabNotifier;
    private OVRHand rightHand;

    private float previousAngle = 0f;
    private float angularVelocity = 0f;
    private float holdTimer = 0f;
    private bool growthTriggeredThisHold = false;

    // Calibration — saved at moment of grab
    private Quaternion grabReferenceRotation;
    private bool isCalibrated = false;
    private bool wasGrabbedLastFrame = false;

    void Start()
    {
        grabNotifier = FindFirstObjectByType<CanGrabNotifier>();

        // Find right hand by name
        OVRHand[] hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
        foreach (OVRHand h in hands)
        {
            if (h.name.ToLower().Contains("right"))
            {
                rightHand = h;
                break;
            }
        }

        if (grabNotifier == null)
            Debug.LogError("CanGrabNotifier not found!");
        if (rightHand == null)
            Debug.LogError("Right OVRHand not found!");
    }

    void Update()
    {
        bool isGrabbed = grabNotifier != null
                         && grabNotifier.IsGrabbed();

        // Detect the exact moment of grabbing
        // Calibrate reference rotation at that moment
        if (isGrabbed && !wasGrabbedLastFrame)
        {
            CalibrateGrabRotation();
        }

        // Reset when released
        if (!isGrabbed && wasGrabbedLastFrame)
        {
            ResetAllStates();
            isCalibrated = false;
        }

        wasGrabbedLastFrame = isGrabbed;

        // Only track when grabbed and calibrated
        if (!isGrabbed || !isCalibrated) return;

        TrackForearmRotation();
    }

    void CalibrateGrabRotation()
    {
        if (rightHand == null) return;

        // Save the hand rotation at the moment of grab
        // This becomes our neutral reference point
        grabReferenceRotation = rightHand.transform.rotation;
        isCalibrated = true;
        previousAngle = 0f;
        angularVelocity = 0f;

        Debug.Log("Grab calibrated! Reference rotation saved.");
    }

    void TrackForearmRotation()
    {
        if (rightHand == null) return;

        // Get rotation RELATIVE to grab pose
        // This means 0 = exactly how you grabbed it
        Quaternion currentRotation = rightHand.transform.rotation;
        Quaternion relativeRotation = Quaternion.Inverse(
            grabReferenceRotation) * currentRotation;

        // Extract the twist/roll around the forearm axis
        // This is the pronation supination angle
        Vector3 relativeEuler = relativeRotation.eulerAngles;

        // Convert euler Z angle to -180 to 180 range
        float rollAngle = relativeEuler.z;
        if (rollAngle > 180f) rollAngle -= 360f;

        // Calculate angular velocity
        float rawVelocity = Mathf.Abs(rollAngle - previousAngle)
                            / Time.deltaTime;

        // Clamp to avoid huge spikes on angle wrap
        rawVelocity = Mathf.Min(rawVelocity, 500f);

        // Smooth the velocity
        angularVelocity = Mathf.Lerp(
            angularVelocity,
            rawVelocity,
            Time.deltaTime * 8f
        );

        previousAngle = rollAngle;

        // Stability 1 = smooth 0 = jerky
        stabilityValue = 1f - Mathf.Clamp01(
            angularVelocity / 80f
        );

        // Jerky check
        isJerky = angularVelocity > jerkyVelocityThreshold;

        // Pronation = rolling wrist inward (negative Z roll)
        // Supination = rolling wrist outward (positive Z roll)
        // Tipping can DOWN to water = pronation
        isWatering = rollAngle < -pronationThreshold;

        // Update debug canvas text with angle
        Debug.Log("Roll angle: " + rollAngle.ToString("F1"));

        HandleHoldTimer();
    }

    void HandleHoldTimer()
    {
        if (isWatering && !isJerky)
        {
            holdTimer += Time.deltaTime;
            holdProgress = Mathf.Clamp01(
                holdTimer / smoothHoldDuration
            );

            if (holdTimer >= smoothHoldDuration
                && !growthTriggeredThisHold)
            {
                triggerGrowth = true;
                growthTriggeredThisHold = true;
                Debug.Log("Growth triggered!");
            }
            else
            {
                triggerGrowth = false;
            }
        }
        else
        {
            float drainSpeed = isJerky ? 1.5f : 3f;
            holdTimer = Mathf.Max(
                0f,
                holdTimer - Time.deltaTime * drainSpeed
            );
            holdProgress = Mathf.Clamp01(
                holdTimer / smoothHoldDuration
            );
            triggerGrowth = false;

            if (holdTimer <= 0f)
                growthTriggeredThisHold = false;
        }
    }

    void ResetAllStates()
    {
        isWatering = false;
        isJerky = false;
        triggerGrowth = false;
        stabilityValue = 1f;
        holdProgress = 0f;
        holdTimer = 0f;
        growthTriggeredThisHold = false;
        angularVelocity = 0f;
    }

    public float GetAngularVelocity() => angularVelocity;
    public float GetStability() => stabilityValue;
    public float GetHoldProgress() => holdProgress;
}