using UnityEngine;

public class DebugRotation : MonoBehaviour
{
    public ForearmRotationTracker tracker;

    void Start()
    {
        if (tracker == null)
            tracker = GetComponent<ForearmRotationTracker>();
    }

    void OnGUI()
    {
        if (tracker == null) return;

        // Shows debug info on screen inside headset
        GUIStyle style = new GUIStyle();
        style.fontSize = 40;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 600, 60),
            "Watering: " + tracker.isWatering, style);

        GUI.Label(new Rect(10, 70, 600, 60),
            "Jerky: " + tracker.isJerky, style);

        GUI.Label(new Rect(10, 130, 600, 60),
            "Stability: " + tracker.stabilityValue.ToString("F2"),
            style);

        GUI.Label(new Rect(10, 190, 600, 60),
            "Hold Progress: " +
            (tracker.holdProgress * 100f).ToString("F0") + "%",
            style);

        GUI.Label(new Rect(10, 250, 600, 60),
            "Growth: " + tracker.triggerGrowth, style);
    }
}