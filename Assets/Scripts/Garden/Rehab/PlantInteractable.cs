using System.Collections;
using UnityEngine;

public class PlantInteractable : MonoBehaviour
{
    [Header("Identity")]
    public string displayName = "Plant";

    [Header("State")]
    public PlantState currentState = PlantState.Idle;

    [Header("Visual Feedback")]
    public float thrivingEmissionIntensity = 2.5f;
    public float wiltingEmissionIntensity  = 0.1f;
    public float idleEmissionIntensity     = 0.8f;

    [Header("Wilt Settings")]
    public float wiltTiltAngle = 25f;
    public float wiltAnimSpeed = 2f;

    [Header("Grow Settings")]
    public float thriveScaleBoost = 0.15f;

    [Header("Particles")]
    public ParticleSystem correctParticles;
    public ParticleSystem wrongParticles;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   correctClip;
    public AudioClip   wrongClip;

    [Header("Highlight (optional)")]
    public GameObject highlightObject;

    // Condition set by PlantTaskManager after task list is built
    [HideInInspector] public ToolType conditionTool = ToolType.None;

    // ── internal ──────────────────────────────────────────────────────────
    private MeshRenderer meshRenderer;
    private PlantGrowth  plantGrowth;
    private Vector3      originalRotation;
    private Vector3      originalScale;
    private Vector3      targetRotation;
    private bool         isAnimatingRot = false;

    void Awake()
    {
        meshRenderer     = GetComponentInChildren<MeshRenderer>();
        plantGrowth      = GetComponentInChildren<PlantGrowth>();
        originalRotation = transform.localEulerAngles;
        originalScale    = transform.localScale;
        targetRotation   = originalRotation;

        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    void Update()
    {
        if (isAnimatingRot)
        {
            transform.localEulerAngles = Vector3.Lerp(
                transform.localEulerAngles, targetRotation,
                Time.deltaTime * wiltAnimSpeed);

            if (Vector3.Distance(transform.localEulerAngles, targetRotation) < 0.5f)
            {
                transform.localEulerAngles = targetRotation;
                isAnimatingRot = false;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void SetAsTarget(bool active)
    {
        if (highlightObject != null)
            highlightObject.SetActive(active);

        // Keep condition visual visible while this plant is a target
        if (active && conditionTool != ToolType.None)
            ShowConditionState();
    }

    public void ApplyToolResult(bool correct)
    {
        if (currentState == PlantState.Complete) return;

        if (correct)
            StartCoroutine(DoThrive());
        else
            StartCoroutine(DoWilt());
    }

    public void ResetToIdle()
    {
        currentState   = PlantState.Idle;
        isAnimatingRot = false;
        transform.localEulerAngles = originalRotation;
        transform.localScale       = originalScale;
        targetRotation = originalRotation;
        SetAsTarget(false);

        // Re-apply condition visual if a condition is assigned
        if (conditionTool != ToolType.None)
            ShowConditionState();
        else
            SetEmission(idleEmissionIntensity, Color.white);
    }

    /// <summary>
    /// Drives the plant's visual to show what condition it's in —
    /// droopy (needs water), overgrown (prune), weedy base (shovel),
    /// yellowing (fertilizer). Called after task list is built so the
    /// patient can observe the garden before deciding / recalling tasks.
    /// </summary>
    public void ShowConditionState()
    {
        switch (conditionTool)
        {
            case ToolType.WateringCan:
                // Thirsty — droop + dry grey-blue tint
                SetEmission(0.35f, new Color(0.5f, 0.55f, 0.7f));
                targetRotation = new Vector3(
                    originalRotation.x + 14f, originalRotation.y, originalRotation.z);
                isAnimatingRot = true;
                transform.localScale = originalScale;
                break;

            case ToolType.Scissors:
                // Overgrown — puff up scale + bright lush green
                SetEmission(1.0f, new Color(0.1f, 0.85f, 0.2f));
                transform.localScale = originalScale * 1.14f;
                targetRotation       = originalRotation;
                isAnimatingRot       = true;
                break;

            case ToolType.Shovel:
                // Weedy base — muddy warm-brown + slight lean
                SetEmission(0.4f, new Color(0.55f, 0.38f, 0.12f));
                targetRotation = new Vector3(
                    originalRotation.x + 7f,
                    originalRotation.y + 5f,
                    originalRotation.z + 4f);
                isAnimatingRot = true;
                transform.localScale = originalScale;
                break;

            case ToolType.Fertilizer:
                // Nutrient-deficient — yellow-pale
                SetEmission(0.55f, new Color(0.9f, 0.82f, 0.1f));
                targetRotation = originalRotation;
                isAnimatingRot = true;
                transform.localScale = originalScale;
                break;

            default:
                SetEmission(idleEmissionIntensity, Color.white);
                break;
        }
    }

    /// <summary>Clear the condition visual back to neutral idle.</summary>
    public void ClearConditionState()
    {
        conditionTool  = ToolType.None;
        transform.localScale = originalScale;
        targetRotation = originalRotation;
        isAnimatingRot = true;
        SetEmission(idleEmissionIntensity, Color.white);
    }

    // ── Private coroutines ────────────────────────────────────────────────

    IEnumerator DoThrive()
    {
        currentState = PlantState.Thriving;

        ClearConditionState();

        if (plantGrowth != null) plantGrowth.TriggerGrowth();

        SetEmission(thrivingEmissionIntensity, new Color(0.2f, 1f, 0.3f));

        if (correctParticles != null) correctParticles.Play();

        PlayClip(correctClip);

        targetRotation = originalRotation;
        isAnimatingRot = true;

        yield return new WaitForSeconds(1.5f);

        currentState = PlantState.Complete;
        SetAsTarget(false);
    }

    IEnumerator DoWilt()
    {
        currentState = PlantState.Wilting;

        SetEmission(wiltingEmissionIntensity, new Color(0.6f, 0.35f, 0.1f));

        targetRotation = new Vector3(
            originalRotation.x + wiltTiltAngle,
            originalRotation.y, originalRotation.z);
        isAnimatingRot = true;

        if (wrongParticles != null) wrongParticles.Play();

        PlayClip(wrongClip);

        yield return new WaitForSeconds(2f);

        ResetToIdle();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void SetEmission(float intensity, Color color)
    {
        if (meshRenderer == null) return;

        Material[] mats = meshRenderer.materials;
        foreach (Material m in mats)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * intensity);
        }
        meshRenderer.materials = mats;
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
