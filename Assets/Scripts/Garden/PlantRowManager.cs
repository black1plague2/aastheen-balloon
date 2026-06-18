using UnityEngine;

public class PlantRowManager : MonoBehaviour
{
    [Header("Plants in order left to right")]
    public PlantGrowth[] plants;

    [Header("References")]
    public ForearmRotationTracker rotationTracker;
    public WaterController waterController;
    public Transform waterSpout;

    private PlantGrowth lastWateredPlant;
    private bool wasGrowthTriggered = false;

    void Start()
    {
        if (rotationTracker == null)
            rotationTracker =
                FindFirstObjectByType<ForearmRotationTracker>();

        if (waterController == null)
            waterController =
                FindFirstObjectByType<WaterController>();
    }

    void Update()
    {
        if (!rotationTracker.isWatering) return;

        // Find closest plant to water stream
        PlantGrowth targetPlant = GetClosestPlant();

        if (targetPlant == null) return;

        // Trigger growth on closest plant
        if (rotationTracker.triggerGrowth
            && !wasGrowthTriggered)
        {
            targetPlant.TriggerGrowth();
            wasGrowthTriggered = true;
        }
        else if (!rotationTracker.triggerGrowth)
        {
            wasGrowthTriggered = false;
        }
    }

    PlantGrowth GetClosestPlant()
    {
        if (plants == null || plants.Length == 0)
            return null;

        PlantGrowth closest = null;
        float closestDistance = float.MaxValue;

        // Use water spout position or watering can position
        Vector3 sourcePosition = waterSpout != null
            ? waterSpout.position
            : transform.position;

        foreach (PlantGrowth plant in plants)
        {
            if (plant == null) continue;
            if (plant.IsFullyGrown()) continue;

            float dist = Vector3.Distance(
                sourcePosition,
                plant.transform.position
            );

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = plant;
            }
        }

        return closest;
    }

    // Check if all plants are fully grown
    public bool AllPlantsGrown()
    {
        foreach (PlantGrowth plant in plants)
        {
            if (plant != null && !plant.IsFullyGrown())
                return false;
        }
        return true;
    }
}