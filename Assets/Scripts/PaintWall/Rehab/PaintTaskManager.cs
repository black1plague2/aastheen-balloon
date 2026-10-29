using UnityEngine;

public enum PaintPatternType { Horizontal, Vertical, Circular, Zigzag }
public enum PaintToolType { Brush, Roller }

/// <summary>
/// Central brain for the Paint the Wall rehab game. Tracks coverage of the
/// paintable surfaces, active tool, session timer, and reports completion
/// back to PaintWallGameManager. Mirrors Garden's PlantTaskManager.
/// </summary>
public class PaintTaskManager : MonoBehaviour
{
    public static PaintTaskManager Instance { get; private set; }

    [Header("Therapist Presets")]
    public float wallHeight          = 2.4f;
    public PaintPatternType pattern  = PaintPatternType.Horizontal;
    public Vector2 paintingAreaSize  = new Vector2(2.6f, 2.0f);
    public PaintToolType requiredTool = PaintToolType.Brush;
    public float sessionDuration     = 180f;

    [Header("Completion")]
    [Range(0f, 1f)] public float coverageTargetPercent = 0.85f;

    [Header("UI / Progress")]
    public PaintProgressManager progressManager;

    [HideInInspector] public PaintableSurface wallSurface;

    [Header("Feedback")]
    public AudioSource milestoneAudio;
    public AudioSource completeAudio;

    private bool  sessionActive;
    private float sessionStartTime;
    private float currentCoveragePercent;
    private int   strokesCompleted;
    private int   mistakes;
    private int   lastMilestone; // 0, 25, 50, 75 — the last coverage milestone crossed
    private PaintToolType heldTool = PaintToolType.Brush;
    private bool  toolHeld;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start() => StartSession();

    void Update()
    {
        if (!sessionActive) return;
        if (Time.time - sessionStartTime >= sessionDuration)
            CompleteSession();
    }

    public void StartSession()
    {
        sessionStartTime       = Time.time;
        currentCoveragePercent = 0f;
        strokesCompleted       = 0;
        mistakes               = 0;
        sessionActive          = true;
        lastMilestone          = 0;
        wallSurface?.DrawPatternGuide(pattern);
        progressManager?.BeginSession();
        progressManager?.SetPresetLabels($"{pattern} · {string.Join(", ", GetExerciseTags())}", $"Tool: {requiredTool}");
        Debug.Log($"[PaintTaskManager] Session started — pattern={pattern}, tool={requiredTool}, duration={sessionDuration}s");
    }

    public void RestartSession() => StartSession();

    // ── Called by PaintableSurface as paint accumulates ───────────────────
    public void ReportCoverage(float percent01)
    {
        if (!sessionActive) return;
        currentCoveragePercent = Mathf.Max(currentCoveragePercent, percent01);
        progressManager?.UpdateCoverage(currentCoveragePercent);
        CheckMilestone();

        if (currentCoveragePercent >= coverageTargetPercent)
            CompleteSession();
    }

    void CheckMilestone()
    {
        int milestone = currentCoveragePercent >= 0.75f ? 75
                       : currentCoveragePercent >= 0.50f ? 50
                       : currentCoveragePercent >= 0.25f ? 25
                       : 0;
        if (milestone <= lastMilestone) return;

        lastMilestone = milestone;
        milestoneAudio?.Play();
        PulseHaptics(0.3f);
        Debug.Log($"[PaintTaskManager] Coverage milestone reached: {milestone}%");
    }

    public void ReportStroke()
    {
        if (!sessionActive) return;
        strokesCompleted++;
        progressManager?.RecordStroke(strokesCompleted);
    }

    void PulseHaptics(float amplitude)
    {
        OVRInput.SetControllerVibration(amplitude, amplitude, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(amplitude, amplitude, OVRInput.Controller.RTouch);
        CancelInvoke(nameof(StopHaptics));
        Invoke(nameof(StopHaptics), 0.2f);
    }

    void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }

    public void ReportOffPatternContact()
    {
        if (!sessionActive) return;
        mistakes++;
        progressManager?.RecordMistake();
    }

    // ── Called by PaintToolSelector ────────────────────────────────────────
    public void OnToolGrabbed(PaintToolType tool)
    {
        heldTool = tool;
        toolHeld = true;
    }

    public void OnToolReleased(PaintToolType tool)
    {
        if (tool == heldTool) toolHeld = false;
    }

    public bool IsCorrectToolHeld() => toolHeld && heldTool == requiredTool;

    void CompleteSession()
    {
        if (!sessionActive) return;
        sessionActive = false;
        progressManager?.OnSessionComplete(currentCoveragePercent, strokesCompleted, Time.time - sessionStartTime);
        completeAudio?.Play();
        Debug.Log($"[PaintTaskManager] Session complete — coverage={currentCoveragePercent:P0}, strokes={strokesCompleted}");
    }

    public bool  IsSessionActive()     => sessionActive;
    public float GetCoveragePercent()  => currentCoveragePercent;
    public int   GetStrokesCompleted() => strokesCompleted;
    public int   GetMistakes()         => mistakes;
    public float GetSessionElapsed()   => Time.time - sessionStartTime;

    /// <summary>
    /// Maps the active painting pattern to the shoulder-rehab exercises it
    /// trains, per the Paint the Wall design brief. Endurance is always
    /// included since any full-duration session builds sustained activity.
    /// </summary>
    public string[] GetExerciseTags()
    {
        switch (pattern)
        {
            case PaintPatternType.Vertical:
                return new[] { "Shoulder Flexion", "Shoulder Endurance" };
            case PaintPatternType.Horizontal:
                return new[] { "Horizontal Abduction", "Horizontal Adduction", "Shoulder Endurance" };
            case PaintPatternType.Circular:
                return new[] { "Circular Shoulder Movements", "Shoulder Endurance" };
            case PaintPatternType.Zigzag:
                return new[] { "Motor Control", "Shoulder Endurance" };
            default:
                return new[] { "Shoulder Endurance" };
        }
    }
}
