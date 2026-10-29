using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Attach to each pickup-able painting tool (Paintbrush, PaintRoller).
/// Reports grab/release to PaintTaskManager via the Meta Building Block
/// Grabbable component, mirroring Garden's ToolSelector.
/// </summary>
public class PaintToolSelector : MonoBehaviour
{
    [Header("Tool Identity")]
    public PaintToolType toolType = PaintToolType.Brush;

    private Grabbable grabbable;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
        if (grabbable == null)
            grabbable = GetComponentInChildren<Grabbable>();

        if (grabbable != null)
            grabbable.WhenPointerEventRaised += OnPointerEvent;
        else
            Debug.LogWarning($"[PaintToolSelector] No Grabbable found on {gameObject.name}");
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
                Debug.Log($"[PaintToolSelector] {toolType} grabbed");
                PaintTaskManager.Instance?.OnToolGrabbed(toolType);
                break;

            case PointerEventType.Unselect:
                Debug.Log($"[PaintToolSelector] {toolType} released");
                PaintTaskManager.Instance?.OnToolReleased(toolType);
                break;
        }
    }
}
