using UnityEngine;
using TMPro;

public class DebugUIManager : MonoBehaviour
{
    [Header("Tracker Reference")]
    public ForearmRotationTracker rotationTracker;

    [Header("UI Text References")]
    public TextMeshProUGUI txt_Watering;
    public TextMeshProUGUI txt_Jerky;
    public TextMeshProUGUI txt_Stability;
    public TextMeshProUGUI txt_HoldProgress;
    public TextMeshProUGUI txt_Growth;
    public TextMeshProUGUI txt_Angle;

    [Header("Colors")]
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.white;
    public Color warningColor = Color.red;

    void Start()
    {
        if (rotationTracker == null)
            rotationTracker =
                FindFirstObjectByType<ForearmRotationTracker>();

        if (rotationTracker == null)
            Debug.LogError("ForearmRotationTracker not found!");
    }

    void Update()
    {
        if (rotationTracker == null) return;

        UpdateWateringText();
        UpdateJerkyText();
        UpdateStabilityText();
        UpdateHoldProgressText();
        UpdateGrowthText();
    }

    void UpdateWateringText()
    {
        if (txt_Watering == null) return;

        if (rotationTracker.isWatering)
        {
            txt_Watering.text = "Watering: YES";
            txt_Watering.color = activeColor;
        }
        else
        {
            txt_Watering.text = "Watering: NO";
            txt_Watering.color = inactiveColor;
        }
    }
    void UpdateAngleText()
{
    if (txt_Angle == null) return;
    // Shows raw roll angle for tuning
    txt_Angle.text = "Raw angle shown in logs";
    txt_Angle.color = inactiveColor;
}

    void UpdateJerkyText()
    {
        if (txt_Jerky == null) return;

        if (rotationTracker.isJerky)
        {
            txt_Jerky.text = "Movement: JERKY!";
            txt_Jerky.color = warningColor;
        }
        else
        {
            txt_Jerky.text = "Movement: Smooth";
            txt_Jerky.color = activeColor;
        }
    }

    void UpdateStabilityText()
    {
        if (txt_Stability == null) return;

        float stability = rotationTracker.stabilityValue;
        txt_Stability.text = "Stability: "
            + (stability * 100f).ToString("F0") + "%";

        // Color changes from red to green based on stability
        txt_Stability.color = Color.Lerp(
            warningColor,
            activeColor,
            stability
        );
    }

    void UpdateHoldProgressText()
    {
        if (txt_HoldProgress == null) return;

        float progress = rotationTracker.holdProgress;
        txt_HoldProgress.text = "Hold Progress: "
            + (progress * 100f).ToString("F0") + "%";

        txt_HoldProgress.color = Color.Lerp(
            inactiveColor,
            activeColor,
            progress
        );
    }

    void UpdateGrowthText()
    {
        if (txt_Growth == null) return;

        if (rotationTracker.triggerGrowth)
        {
            txt_Growth.text = "PLANT GROWING!";
            txt_Growth.color = activeColor;
        }
        else
        {
            txt_Growth.text = "Keep holding...";
            txt_Growth.color = inactiveColor;
        }
    }
}