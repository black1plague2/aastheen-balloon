using UnityEngine;

/// <summary>
/// Attach to GameManager. Runs at Awake (before PlantTaskManager.Start)
/// and wires all rehab components by finding objects by path at runtime.
/// Also adds PlantProximityTrigger to each plant so tools can interact.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RehabAutoWire : MonoBehaviour
{
    void Awake()
    {
        PlantTaskManager ptm = GetComponent<PlantTaskManager>();
        if (ptm == null)
        {
            Debug.LogError("[RehabAutoWire] PlantTaskManager not found on GameManager!");
            return;
        }

        RehabProgressManager rpm = GetComponent<RehabProgressManager>();

        // ── Wire plants ───────────────────────────────────────────────────
        var plantDefs = new (string path, string displayName)[]
        {
            ("TulipYellow/TulipYellow",     "Tulip Yellow A"),
            ("TulipYellow (1)/TulipYellow", "Tulip Yellow B"),
            ("Hydrangea/Hydrangea",         "Hydrangea"),
        };

        ptm.allPlants.Clear();
        foreach (var (path, name) in plantDefs)
        {
            GameObject go = FindByPath(path);
            if (go == null) { Debug.LogWarning($"[RehabAutoWire] Plant not found: {path}"); continue; }

            // PlantInteractable
            PlantInteractable pi = go.GetComponent<PlantInteractable>();
            if (pi == null) pi = go.AddComponent<PlantInteractable>();
            pi.displayName = name;

            // AudioSource for feedback
            AudioSource src = go.GetComponent<AudioSource>();
            if (src == null) src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 1f;
            src.maxDistance  = 5f;
            pi.audioSource   = src;

            // Load audio clips from Resources
            pi.correctClip = Resources.Load<AudioClip>("GardenAudio/correct");
            pi.wrongClip   = Resources.Load<AudioClip>("GardenAudio/wrong");

            // PlantProximityTrigger — detects tool entering plant radius
            PlantProximityTrigger ppt = go.GetComponent<PlantProximityTrigger>();
            if (ppt == null) ppt = go.AddComponent<PlantProximityTrigger>();
            ppt.detectionRadius = 0.35f;

            ptm.allPlants.Add(pi);
        }

        // ── Wire tools ────────────────────────────────────────────────────
        var toolDefs = new (string path, ToolType type)[]
        {
            ("WateringCup/WateringCup", ToolType.WateringCan),
            ("Side1/Pruner/Pruner",     ToolType.Scissors),
            ("RehabTools/Shovel",       ToolType.Shovel),
            ("RehabTools/Fertilizer",   ToolType.Fertilizer),
        };

        ptm.allTools.Clear();
        foreach (var (path, toolType) in toolDefs)
        {
            GameObject go = FindByPath(path);
            if (go == null) { Debug.LogWarning($"[RehabAutoWire] Tool not found: {path}"); continue; }

            ToolSelector ts = go.GetComponent<ToolSelector>();
            if (ts == null) ts = go.AddComponent<ToolSelector>();
            ts.toolType = toolType;

            ptm.allTools.Add(ts);
        }

        // ── Wire UI ───────────────────────────────────────────────────────
        GameObject rehabCanvas = GameObject.Find("RehabCanvas");
        if (rehabCanvas != null)
        {
            TaskInstructionUI tui = rehabCanvas.GetComponentInChildren<TaskInstructionUI>(true);
            if (tui != null) ptm.instructionUI = tui;
        }

        if (rpm != null) ptm.progressManager = rpm;

        Debug.Log($"[RehabAutoWire] Wired {ptm.allPlants.Count} plants, {ptm.allTools.Count} tools.");
    }

    static GameObject FindByPath(string path)
    {
        string[] parts = path.Split('/');
        if (parts.Length == 0) return null;

        GameObject current = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == parts[0]) { current = root; break; }
        }
        if (current == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            Transform child = current.transform.Find(parts[i]);
            if (child == null) return null;
            current = child.gameObject;
        }
        return current;
    }
}
