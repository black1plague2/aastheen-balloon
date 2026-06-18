using UnityEngine;
using System.Collections;

public class PlantGrowth : MonoBehaviour
{
    [Header("Growth Settings")]
    public int maxGrowthStages = 3;
    public float growthScaleAmount = 0.25f;
    public float growthAnimationSpeed = 2f;

    [Header("Visual Feedback")]
    public ParticleSystem growthParticles;

    private ForearmRotationTracker rotationTracker;
    private WaterController waterController;

    private int currentStage = 0;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isAnimating = false;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        rotationTracker =
            FindFirstObjectByType<ForearmRotationTracker>();
        waterController =
            FindFirstObjectByType<WaterController>();
    }

    void Update()
    {
        // Smoothly animate toward target scale
        if (isAnimating)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * growthAnimationSpeed
            );

            // Stop animating when close enough
            if (Vector3.Distance(
                transform.localScale, targetScale) < 0.001f)
            {
                transform.localScale = targetScale;
                isAnimating = false;
            }
        }
    }

    // Call this from outside to trigger growth
    public void TriggerGrowth()
    {
        if (currentStage >= maxGrowthStages) return;

        currentStage++;
        float scaleFactor = 1f +
            (currentStage * growthScaleAmount);
        targetScale = originalScale * scaleFactor;
        isAnimating = true;

        // Play growth particles if assigned
        if (growthParticles != null)
            growthParticles.Play();

        Debug.Log(gameObject.name + " grew to stage "
            + currentStage);
    }

    public bool IsFullyGrown() =>
        currentStage >= maxGrowthStages;

    public int GetGrowthStage() => currentStage;
}