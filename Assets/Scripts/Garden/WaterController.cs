using UnityEngine;

public class WaterController : MonoBehaviour
{
    [Header("References")]
    public ForearmRotationTracker rotationTracker;
    public ParticleSystem waterParticles;
    public ParticleSystem spillParticles;

    [Header("Haptic Settings")]
    public float spillHapticStrength = 1f;
    public float spillHapticDuration = 0.15f;
    public float growthHapticStrength = 0.3f;
    public float growthHapticDuration = 0.6f;

    private bool wasJerky = false;
    private bool wasGrowth = false;

    void Start()
    {
        if (rotationTracker == null)
            rotationTracker =
                FindFirstObjectByType<ForearmRotationTracker>();

        // Stop both particles at start
        if (waterParticles != null) waterParticles.Stop();
        if (spillParticles != null) spillParticles.Stop();
    }

    void Update()
    {
        if (rotationTracker == null) return;

        HandleWater();
        HandleSpill();
        HandleGrowthHaptic();
    }

    void HandleWater()
    {
        if (waterParticles == null) return;

        // Water flows when watering and not jerky
        bool shouldFlow = rotationTracker.isWatering
                          && !rotationTracker.isJerky;

        if (shouldFlow && !waterParticles.isPlaying)
        {
            waterParticles.Play();
        }
        else if (!shouldFlow && waterParticles.isPlaying)
        {
            waterParticles.Stop();
        }
    }

    void HandleSpill()
    {
        if (spillParticles == null) return;

        // Spill when watering but jerky
        bool isSpilling = rotationTracker.isWatering
                          && rotationTracker.isJerky;

        // Just became jerky
        if (isSpilling && !wasJerky)
        {
            spillParticles.Play();
            TriggerHaptic(
                spillHapticStrength,
                spillHapticDuration
            );
            Debug.Log("Spill triggered!");
        }
        // Just stopped being jerky
        else if (!isSpilling && wasJerky)
        {
            spillParticles.Stop();
        }

        wasJerky = isSpilling;
    }

    void HandleGrowthHaptic()
    {
        // Gentle haptic when growth triggers
        if (rotationTracker.triggerGrowth && !wasGrowth)
        {
            TriggerHaptic(
                growthHapticStrength,
                growthHapticDuration
            );
            Debug.Log("Growth haptic!");
        }

        wasGrowth = rotationTracker.triggerGrowth;
    }

    void TriggerHaptic(float strength, float duration)
    {
        OVRInput.SetControllerVibration(
            strength, strength, OVRInput.Controller.RTouch
        );
        Invoke("StopHaptic", duration);
    }

    void StopHaptic()
    {
        OVRInput.SetControllerVibration(
            0f, 0f, OVRInput.Controller.RTouch
        );
    }
}