using UnityEngine;

/// <summary>
/// Attach to each plant (alongside PlantInteractable).
/// Uses a sphere trigger to detect when the player brings a held tool
/// close to the plant. When the correct tool enters the radius while held,
/// it notifies PlantTaskManager to start/complete the hold timer.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class PlantProximityTrigger : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Radius in metres within which a tool is considered 'at' this plant")]
    public float detectionRadius = 0.35f;

    private SphereCollider triggerCol;
    private PlantInteractable plantInteractable;
    private bool toolInRange = false;

    void Awake()
    {
        triggerCol = GetComponent<SphereCollider>();
        triggerCol.isTrigger = true;
        triggerCol.radius    = detectionRadius;

        plantInteractable = GetComponent<PlantInteractable>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (toolInRange) return;

        // Check if the entering object (or its parent) is a held tool
        ToolSelector tool = other.GetComponent<ToolSelector>();
        if (tool == null) tool = other.GetComponentInParent<ToolSelector>();
        if (tool == null) return;
        if (!tool.IsHeld) return;

        toolInRange = true;

        // Tell PlantTaskManager a tool arrived at this plant
        PlantTaskManager.Instance?.OnToolAtPlant(tool.toolType, plantInteractable);
        Debug.Log($"[PlantProximityTrigger] {tool.toolType} entered range of {gameObject.name}");
    }

    void OnTriggerExit(Collider other)
    {
        ToolSelector tool = other.GetComponent<ToolSelector>();
        if (tool == null) tool = other.GetComponentInParent<ToolSelector>();
        if (tool == null) return;

        toolInRange = false;

        PlantTaskManager.Instance?.OnToolLeftPlant(tool.toolType, plantInteractable);
        Debug.Log($"[PlantProximityTrigger] {tool.toolType} left range of {gameObject.name}");
    }

    public bool IsToolInRange() => toolInRange;
}
