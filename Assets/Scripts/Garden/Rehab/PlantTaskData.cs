using UnityEngine;

/// <summary>
/// All tool types the patient can pick up and use.
/// </summary>
public enum ToolType
{
    None,
    WateringCan,
    Scissors,   // pruning
    Shovel,     // weed removal
    Fertilizer  // fertilizer bottle/bag
}

/// <summary>
/// Visual/health state of a plant.
/// </summary>
public enum PlantState
{
    Idle,       // neutral, waiting for action
    Thriving,   // correct action applied — grows, brightens
    Wilting,    // wrong action applied — droops, dims
    Complete    // fully done for this session
}

/// <summary>
/// One task assigned to one plant: "Water Plant A", "Prune Plant B", etc.
/// </summary>
[System.Serializable]
public class PlantTask
{
    public string plantName;            // display name, e.g. "Tulip A"
    public PlantInteractable plant;     // reference to the plant in scene
    public ToolType requiredTool;       // what tool must be used
    public bool isCompleted = false;

    public PlantTask(PlantInteractable p, ToolType tool)
    {
        plant        = p;
        plantName    = p != null ? p.displayName : "Plant";
        requiredTool = tool;
        isCompleted  = false;
    }
}
