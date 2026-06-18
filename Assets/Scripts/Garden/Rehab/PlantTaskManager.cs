using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GardenGameMode
{
    Sequenced,  // one task at a time, patient must do them in order
    FreeOrder   // all plants active simultaneously, patient picks any order
}

/// <summary>
/// Central brain for the rehabilitation garden game.
/// Manages task sequencing OR free-order mode, memory mode with full
/// session overview, condition-visual assignment, hold timer, and scoring.
/// </summary>
public class PlantTaskManager : MonoBehaviour
{
    public static PlantTaskManager Instance { get; private set; }

    // ── Game Mode ─────────────────────────────────────────────────────────
    [Header("Game Mode")]
    public GardenGameMode gameMode = GardenGameMode.Sequenced;

    [Header("Intro")]
    [Tooltip("Set false so GardenGameManager can show the intro first, then call StartSession() manually")]
    public bool autoStartSession = true;

    // ── Difficulty / Session ──────────────────────────────────────────────
    [Header("Difficulty")]
    public int tasksPerSession    = 2;
    public int maxTasksPerSession = 5;
    public int sessionsToLevelUp  = 2;

    // ── Memory Mode ───────────────────────────────────────────────────────
    [Header("Memory Mode")]
    [Tooltip("Show ALL tasks in overview panel at session start, then hide them")]
    public bool  showSessionOverviewFirst = false;
    [Tooltip("Hide each task's instruction after N seconds during play")]
    public bool  memoryModeEnabled        = false;
    public float instructionDisplayTime   = 4f;

    // ── Plants & Tools ────────────────────────────────────────────────────
    [Header("Plants in scene")]
    public List<PlantInteractable> allPlants = new List<PlantInteractable>();

    [Header("Tools in scene")]
    public List<ToolSelector> allTools = new List<ToolSelector>();

    // ── UI ────────────────────────────────────────────────────────────────
    [Header("UI")]
    public TaskInstructionUI    instructionUI;
    public RehabProgressManager progressManager;

    // ── Hold durations per tool ───────────────────────────────────────────
    private static readonly Dictionary<ToolType, float> ToolHoldDuration =
        new Dictionary<ToolType, float>
        {
            { ToolType.WateringCan, 3f   },
            { ToolType.Scissors,    2f   },
            { ToolType.Shovel,      2.5f },
            { ToolType.Fertilizer,  2f   }
        };

    // ── Runtime state ─────────────────────────────────────────────────────
    private List<PlantTask> sessionTasks      = new List<PlantTask>();
    private List<PlantTask> activeTasks       = new List<PlantTask>(); // for FreeOrder
    private int             currentTaskIndex  = 0;
    private ToolType        heldTool          = ToolType.None;
    private PlantInteractable toolAtPlant     = null;
    private bool            sessionActive     = false;
    private int             consecutiveCorrect = 0;

    // Hold timer
    private float holdTimer     = 0f;
    private bool  holdActive    = false;
    private bool  holdCompleted = false;

    // ── Unity ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (autoStartSession)
            StartSession();
    }

    void Update()
    {
        if (!sessionActive) return;
        HandleHoldTimer();
    }

    // ── Session control ───────────────────────────────────────────────────

    public void StartSession()
    {
        sessionActive    = false;
        currentTaskIndex = 0;
        holdTimer        = 0f;
        holdActive       = false;
        holdCompleted    = false;
        heldTool         = ToolType.None;
        toolAtPlant      = null;

        foreach (var p in allPlants) if (p != null) p.ResetToIdle();
        foreach (var t in allTools)  if (t != null) t.SetRequiredHighlight(false);

        sessionTasks = BuildTaskList();
        activeTasks  = new List<PlantTask>(sessionTasks);

        if (sessionTasks.Count == 0)
        {
            Debug.LogWarning("[PlantTaskManager] No plants assigned!");
            return;
        }

        // Assign condition visuals so plants look like they need care
        AssignConditions(sessionTasks);

        sessionActive = true;
        progressManager?.BeginSession();

        if (showSessionOverviewFirst)
        {
            // Show all tasks in overview, then start gameplay
            StartCoroutine(ShowOverviewThenBegin());
        }
        else
        {
            BeginFirstTask();
        }

        Debug.Log($"[PlantTaskManager] Session started — {sessionTasks.Count} tasks, " +
                  $"Mode={gameMode}, MemMode={memoryModeEnabled}, Overview={showSessionOverviewFirst}");
    }

    public void RestartSession() => StartSession();

    // ── Tool grab events (called by ToolSelector) ─────────────────────────

    public void OnToolGrabbed(ToolType tool)
    {
        heldTool      = tool;
        holdTimer     = 0f;
        holdCompleted = false;
        holdActive    = false;

        if (!sessionActive) return;

        PlantTask current = GetCurrentActiveTask();
        if (current == null) return;

        bool correct = (tool == current.requiredTool);
        if (!correct)
            instructionUI?.FlashWrongTool(tool, current.requiredTool);
    }

    public void OnToolReleased(ToolType tool)
    {
        if (tool != heldTool) return;
        heldTool      = ToolType.None;
        holdActive    = false;
        holdTimer     = 0f;
        toolAtPlant   = null;
        instructionUI?.ShowHoldProgress(false);
    }

    // ── Proximity events (called by PlantProximityTrigger) ────────────────

    public void OnToolAtPlant(ToolType tool, PlantInteractable plant)
    {
        if (!sessionActive || heldTool == ToolType.None) return;

        PlantTask matchedTask = FindActiveTaskForPlant(plant);
        if (matchedTask == null) return;

        bool correctTool = (tool == matchedTask.requiredTool);

        if (correctTool)
        {
            toolAtPlant   = plant;
            holdActive    = true;
            holdCompleted = false;
            holdTimer     = 0f;
            instructionUI?.ShowHoldProgress(true);
            Debug.Log($"[PlantTaskManager] Correct tool at {plant.displayName} — hold timer started");
        }
        else
        {
            // In sequenced mode only penalise if it's also the wrong plant order
            matchedTask.plant?.ApplyToolResult(false);
            progressManager?.RecordMistake();
            instructionUI?.FlashWrongTool(tool, matchedTask.requiredTool);
            Debug.Log($"[PlantTaskManager] Wrong tool at plant — mistake recorded");
        }
    }

    public void OnToolLeftPlant(ToolType tool, PlantInteractable plant)
    {
        if (plant != toolAtPlant) return;
        toolAtPlant = null;
        holdActive  = false;
        holdTimer   = 0f;
        instructionUI?.ShowHoldProgress(false);
    }

    // ── Hold timer ────────────────────────────────────────────────────────

    void HandleHoldTimer()
    {
        if (!holdActive || holdCompleted) return;

        float required = ToolHoldDuration.ContainsKey(heldTool)
            ? ToolHoldDuration[heldTool] : 2f;

        holdTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(holdTimer / required);
        instructionUI?.UpdateHoldProgress(progress);

        if (holdTimer >= required)
        {
            holdCompleted = true;
            PlantTask task = FindActiveTaskForPlant(toolAtPlant);
            if (task != null) OnTaskCompleted(task);
        }
    }

    void OnTaskCompleted(PlantTask task)
    {
        Debug.Log($"[PlantTaskManager] Task complete: {task.plantName} with {task.requiredTool}");

        task.isCompleted = true;
        task.plant?.ApplyToolResult(true);
        progressManager?.RecordCorrect();

        activeTasks.Remove(task);

        OVRInput.SetControllerVibration(0.4f, 0.4f, OVRInput.Controller.RTouch);
        Invoke(nameof(StopHaptic), 0.5f);

        StartCoroutine(AdvanceTask(task));
    }

    IEnumerator AdvanceTask(PlantTask completedTask)
    {
        task_SetHighlight(completedTask, false);
        foreach (var t in allTools) t?.SetRequiredHighlight(false);

        yield return new WaitForSeconds(1.8f);

        holdTimer     = 0f;
        holdActive    = false;
        holdCompleted = false;
        toolAtPlant   = null;

        if (gameMode == GardenGameMode.Sequenced)
        {
            currentTaskIndex++;
            if (currentTaskIndex >= sessionTasks.Count)
                OnSessionComplete();
            else
            {
                ShowCurrentTaskInstruction(memoryModeEnabled);
                ActivateCurrentTask();
            }
        }
        else // FreeOrder
        {
            if (activeTasks.Count == 0)
                OnSessionComplete();
            else
                ActivateAllActiveTasks();
        }
    }

    // ── Activate tasks ────────────────────────────────────────────────────

    void BeginFirstTask()
    {
        if (gameMode == GardenGameMode.Sequenced)
        {
            ShowCurrentTaskInstruction(memoryModeEnabled);
            ActivateCurrentTask();
        }
        else
        {
            // Show hint for first task only, then activate all
            if (!memoryModeEnabled)
                ShowCurrentTaskInstruction(false);
            ActivateAllActiveTasks();
        }
    }

    void ActivateCurrentTask()
    {
        if (currentTaskIndex >= sessionTasks.Count) return;
        PlantTask task = sessionTasks[currentTaskIndex];
        task.plant?.SetAsTarget(true);
        HighlightRequiredTool(task.requiredTool);
    }

    void ActivateAllActiveTasks()
    {
        foreach (var task in activeTasks)
            task.plant?.SetAsTarget(true);

        // In FreeOrder: highlight the required tool for the closest active task
        // (just highlight all required tools to aid tool selection training)
        var toolsNeeded = new HashSet<ToolType>();
        foreach (var task in activeTasks) toolsNeeded.Add(task.requiredTool);
        foreach (var t in allTools)
            if (t != null) t.SetRequiredHighlight(toolsNeeded.Contains(t.toolType));
    }

    void HighlightRequiredTool(ToolType required)
    {
        foreach (var t in allTools)
            if (t != null) t.SetRequiredHighlight(t.toolType == required);
    }

    void task_SetHighlight(PlantTask task, bool on)
    {
        task.plant?.SetAsTarget(on);
    }

    // ── Session complete ──────────────────────────────────────────────────

    void OnSessionComplete()
    {
        sessionActive = false;
        consecutiveCorrect++;

        if (consecutiveCorrect >= sessionsToLevelUp)
        {
            consecutiveCorrect = 0;
            tasksPerSession = Mathf.Min(tasksPerSession + 1, maxTasksPerSession);
            Debug.Log($"[PlantTaskManager] Difficulty up! Tasks: {tasksPerSession}");
        }

        progressManager?.OnSessionComplete(sessionTasks);
        instructionUI?.ShowSessionComplete(tasksPerSession);
        Debug.Log("[PlantTaskManager] Session complete!");
    }

    // ── Instruction display ───────────────────────────────────────────────

    void ShowCurrentTaskInstruction(bool memoryMode)
    {
        if (instructionUI == null) return;
        PlantTask task = GetCurrentActiveTask();
        if (task == null) return;

        int idx   = gameMode == GardenGameMode.Sequenced ? currentTaskIndex + 1 : (sessionTasks.Count - activeTasks.Count + 1);
        int total = sessionTasks.Count;
        instructionUI.ShowTask(task, idx, total);

        if (memoryMode) StartCoroutine(HideInstructionAfterDelay());
    }

    IEnumerator HideInstructionAfterDelay()
    {
        yield return new WaitForSeconds(instructionDisplayTime);
        instructionUI?.HideInstruction();
    }

    IEnumerator ShowOverviewThenBegin()
    {
        instructionUI?.ShowSessionOverview(sessionTasks);
        yield return new WaitForSeconds(instructionDisplayTime);
        instructionUI?.HideOverview();
        BeginFirstTask();
    }

    // ── Condition assignment ──────────────────────────────────────────────

    void AssignConditions(List<PlantTask> tasks)
    {
        // Clear conditions from all plants first
        foreach (var p in allPlants)
            if (p != null) { p.conditionTool = ToolType.None; }

        // Assign each plant's condition to match its required tool
        foreach (var task in tasks)
        {
            if (task.plant == null) continue;
            task.plant.conditionTool = task.requiredTool;
            task.plant.ShowConditionState();
        }
    }

    // ── Task list builder ─────────────────────────────────────────────────

    List<PlantTask> BuildTaskList()
    {
        var tasks = new List<PlantTask>();
        if (allPlants.Count == 0) return tasks;

        // Shuffle plants
        List<PlantInteractable> shuffled = new List<PlantInteractable>(allPlants);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Assign a different tool to each plant — cycle through all 4
        ToolType[] toolPool = { ToolType.WateringCan, ToolType.Scissors, ToolType.Shovel, ToolType.Fertilizer };

        // Shuffle tool pool too so the assignment is varied each session
        for (int i = toolPool.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (toolPool[i], toolPool[j]) = (toolPool[j], toolPool[i]);
        }

        int count = Mathf.Min(tasksPerSession, shuffled.Count);
        for (int i = 0; i < count; i++)
            tasks.Add(new PlantTask(shuffled[i], toolPool[i % toolPool.Length]));

        return tasks;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    PlantTask GetCurrentActiveTask()
    {
        if (gameMode == GardenGameMode.Sequenced)
            return currentTaskIndex < sessionTasks.Count ? sessionTasks[currentTaskIndex] : null;
        else
            return activeTasks.Count > 0 ? activeTasks[0] : null;
    }

    PlantTask FindActiveTaskForPlant(PlantInteractable plant)
    {
        if (plant == null) return null;

        if (gameMode == GardenGameMode.Sequenced)
        {
            // In sequenced mode, only the current task's plant is valid
            PlantTask cur = GetCurrentActiveTask();
            return (cur != null && cur.plant == plant) ? cur : null;
        }
        else
        {
            // In free-order mode, any active task's plant is valid
            foreach (var t in activeTasks)
                if (t.plant == plant) return t;
            return null;
        }
    }

    void StopHaptic() =>
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);

    public PlantTask GetCurrentTask()      => GetCurrentActiveTask();
    public int       GetCurrentTaskIndex() => currentTaskIndex;
    public int       GetTotalTasks()       => sessionTasks.Count;
    public int       GetRemainingTasks()   => gameMode == GardenGameMode.Sequenced
                                               ? sessionTasks.Count - currentTaskIndex
                                               : activeTasks.Count;
    public bool      IsSessionActive()     => sessionActive;
    public float     GetHoldProgress()     => holdCompleted ? 1f :
        (ToolHoldDuration.ContainsKey(heldTool)
            ? Mathf.Clamp01(holdTimer / ToolHoldDuration[heldTool]) : 0f);
}
