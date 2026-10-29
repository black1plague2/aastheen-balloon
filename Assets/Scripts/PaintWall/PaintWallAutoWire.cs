using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to GameManager. Runs before PaintTaskManager's Start() and wires
/// paintable surfaces, tool selectors, tool tip-contact triggers, and the
/// HUD canvas by scene path, mirroring Garden's RehabAutoWire.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PaintWallAutoWire : MonoBehaviour
{
    void Awake()
    {
        PaintTaskManager ptm = GetComponent<PaintTaskManager>();
        if (ptm == null)
        {
            Debug.LogError("[PaintWallAutoWire] PaintTaskManager not found on GameManager!");
            return;
        }

        PaintProgressManager ppm = GetComponent<PaintProgressManager>();
        if (ppm != null) ptm.progressManager = ppm;

        // ── Wire paintable surfaces ───────────────────────────────────────
        WireSurface("PaintWallRoom/Wall_Front", ptm);
        WireSurface("PaintWallRoom/Ceiling", ptm);

        // ── Wire tools ────────────────────────────────────────────────────
        WireTool("PaintTools/Paintbrush", PaintToolType.Brush);
        WireTool("PaintTools/PaintRoller", PaintToolType.Roller);

        // ── Wire HUD ──────────────────────────────────────────────────────
        WireHud(ppm);

        // ── Wire feedback audio ───────────────────────────────────────────
        Transform milestoneSfx = transform.Find("SFX_Milestone");
        if (milestoneSfx != null) ptm.milestoneAudio = milestoneSfx.GetComponent<AudioSource>();

        Transform completeSfx = transform.Find("SFX_Complete");
        if (completeSfx != null) ptm.completeAudio = completeSfx.GetComponent<AudioSource>();

        Debug.Log("[PaintWallAutoWire] Wired paintable surfaces, tools, HUD, and feedback audio.");
    }

    void WireHud(PaintProgressManager ppm)
    {
        if (ppm == null) return;

        GameObject canvas = GameObject.Find("HUD_Canvas");
        if (canvas == null) { Debug.LogWarning("[PaintWallAutoWire] HUD_Canvas not found."); return; }

        ppm.txt_PatternExercise = FindTmp(canvas, "Txt_PatternExercise");
        ppm.txt_Tool            = FindTmp(canvas, "Txt_Tool");
        ppm.txt_Coverage        = FindTmp(canvas, "Txt_Coverage");
        ppm.txt_Strokes         = FindTmp(canvas, "Txt_Strokes");
        ppm.txt_Mistakes        = FindTmp(canvas, "Txt_Mistakes");
        ppm.txt_SessionTime     = FindTmp(canvas, "Txt_SessionTime");

        Transform slider = canvas.transform.Find("CoverageSlider");
        if (slider != null) ppm.coverageSlider = slider.GetComponent<Slider>();

        Transform panel = canvas.transform.Find("SessionCompletePanel");
        if (panel != null)
        {
            ppm.sessionCompletePanel = panel.gameObject;
            ppm.txt_SummaryTitle = FindTmp(panel.gameObject, "Txt_SummaryTitle");
            ppm.txt_SummaryBody  = FindTmp(panel.gameObject, "Txt_SummaryBody");
        }
    }

    static TextMeshProUGUI FindTmp(GameObject root, string childName)
    {
        Transform t = root.transform.Find(childName);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    void WireSurface(string path, PaintTaskManager ptm)
    {
        GameObject go = FindByPath(path);
        if (go == null) { Debug.LogWarning($"[PaintWallAutoWire] Surface not found: {path}"); return; }

        PaintableSurface surface = go.GetComponent<PaintableSurface>();
        if (surface == null) surface = go.AddComponent<PaintableSurface>();
        surface.taskManager = ptm;

        if (path == "PaintWallRoom/Wall_Front") ptm.wallSurface = surface;
    }

    void WireTool(string path, PaintToolType type)
    {
        GameObject go = FindByPath(path);
        if (go == null) { Debug.LogWarning($"[PaintWallAutoWire] Tool not found: {path}"); return; }

        PaintToolSelector selector = go.GetComponent<PaintToolSelector>();
        if (selector == null) selector = go.AddComponent<PaintToolSelector>();
        selector.toolType = type;

        AddTipTrigger(go);
    }

    /// <summary>
    /// Adds a trigger child positioned toward the "working end" of the tool
    /// (away from the handle), sized generously from the tool's own render
    /// bounds so contact registers reliably regardless of exact grip pose.
    /// </summary>
    void AddTipTrigger(GameObject tool)
    {
        if (tool.transform.Find("TipTrigger") != null) return;

        var renderers = tool.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Debug.LogWarning($"[PaintWallAutoWire] No renderers on {tool.name}, skipping tip trigger."); return; }

        Bounds worldBounds = renderers[0].bounds;
        foreach (var r in renderers) worldBounds.Encapsulate(r.bounds);

        // Working end is the lower half of the tool (opposite the handle-top), pushed forward to
        // the bristle/roller face — both tool prefabs are thinnest along their own forward axis
        // (the brush/roller head is a flat/cylindrical face there), so centering the trigger in
        // that thickness left it sitting inside the tool, requiring the hand to push through the
        // wall before the sphere's edge ever reached the wall collider.
        Vector3 worldTipPos = worldBounds.center
            + Vector3.down * (worldBounds.size.y * 0.35f)
            + tool.transform.forward * (worldBounds.size.z * 0.5f + 0.01f);
        // Small relative to the tool's footprint — a wide trigger registers contact against
        // whatever it overlaps, not specifically the bristle/roller edge, so paint lands wherever
        // ClosestPoint happens to fall within that oversized volume instead of where the player aims.
        float worldRadius = Mathf.Max(worldBounds.size.x, worldBounds.size.z) * 0.15f;

        var tip = new GameObject("TipTrigger");
        tip.transform.SetParent(tool.transform, false);
        tip.transform.position = worldTipPos;

        var scale = tool.transform.lossyScale;
        float avgScale = (Mathf.Abs(scale.x) + Mathf.Abs(scale.y) + Mathf.Abs(scale.z)) / 3f;
        var sphere = tip.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = avgScale > 0.0001f ? worldRadius / avgScale : worldRadius;

        tip.AddComponent<PaintTipContact>();
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
