using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Attach to each tool GameObject (WateringCan, Scissors, Shovel, Fertilizer).
/// Supports two grab detection methods:
///   1. Oculus Grabbable.WhenPointerEventRaised (standard)
///   2. CanGrabNotifier polling (fallback for WateringCup which uses it)
/// Reports grab/release to PlantTaskManager.
/// </summary>
public class ToolSelector : MonoBehaviour
{
    [Header("Tool Identity")]
    public ToolType toolType = ToolType.WateringCan;

    [Header("Highlight when this tool is the required one")]
    public GameObject requiredHighlight;

    // ── internal ──────────────────────────────────────────────────────────
    private Grabbable        grabbable;
    private CanGrabNotifier  canGrabNotifier;
    private bool             isHeld        = false;
    private bool             wasHeldLastFrame = false;

    public bool IsHeld => isHeld;

    void Start()
    {
        // Try Grabbable first (on self, then children)
        grabbable = GetComponent<Grabbable>();
        if (grabbable == null)
            grabbable = GetComponentInChildren<Grabbable>();

        if (grabbable != null)
            grabbable.WhenPointerEventRaised += OnPointerEvent;

        // Also check for CanGrabNotifier (WateringCup uses this)
        canGrabNotifier = GetComponent<CanGrabNotifier>();
        if (canGrabNotifier == null)
            canGrabNotifier = GetComponentInChildren<CanGrabNotifier>();

        if (grabbable == null && canGrabNotifier == null)
            Debug.LogWarning($"[ToolSelector] No Grabbable or CanGrabNotifier found on {gameObject.name}");

        SetRequiredHighlight(false);
    }

    void Update()
    {
        // Poll CanGrabNotifier if no Grabbable event available
        if (canGrabNotifier != null)
        {
            bool grabbed = canGrabNotifier.IsGrabbed();
            if (grabbed && !wasHeldLastFrame)
            {
                isHeld = true;
                Debug.Log($"[ToolSelector] {toolType} grabbed (via CanGrabNotifier)");
                PlantTaskManager.Instance?.OnToolGrabbed(toolType);
            }
            else if (!grabbed && wasHeldLastFrame)
            {
                isHeld = false;
                Debug.Log($"[ToolSelector] {toolType} released (via CanGrabNotifier)");
                PlantTaskManager.Instance?.OnToolReleased(toolType);
            }
            wasHeldLastFrame = grabbed;
        }
    }

    void OnDestroy()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }

    void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                isHeld = true;
                wasHeldLastFrame = true;
                Debug.Log($"[ToolSelector] {toolType} grabbed");
                PlantTaskManager.Instance?.OnToolGrabbed(toolType);
                break;

            case PointerEventType.Unselect:
                isHeld = false;
                wasHeldLastFrame = false;
                Debug.Log($"[ToolSelector] {toolType} released");
                PlantTaskManager.Instance?.OnToolReleased(toolType);
                break;
        }
    }

    /// <summary>Show/hide the "this is the required tool" highlight.</summary>
    public void SetRequiredHighlight(bool active)
    {
        if (requiredHighlight != null)
            requiredHighlight.SetActive(active);
    }
}
