using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// World-space UI panel that shows task instructions.
/// Supports:
///   - Per-task display (sequenced mode)
///   - Session overview panel (shows ALL tasks at once — memory mode)
///   - Hold progress bar
///   - Wrong-tool flash
///   - Session complete screen
/// </summary>
public class TaskInstructionUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject taskPanel;
    public GameObject overviewPanel;
    public GameObject completePanel;

    [Header("Task Panel Text")]
    public TextMeshProUGUI txt_TaskNumber;
    public TextMeshProUGUI txt_PlantName;
    public TextMeshProUGUI txt_Action;
    public TextMeshProUGUI txt_ToolName;
    public TextMeshProUGUI txt_MemoryHint;

    [Header("Session Overview")]
    [Tooltip("One TMP text per overview row — up to maxTasksPerSession rows")]
    public TextMeshProUGUI[] txt_OverviewRows;
    public TextMeshProUGUI   txt_OverviewHeader;

    [Header("Hold Progress Bar")]
    public GameObject holdProgressRoot;
    public Image      holdProgressFill;

    [Header("Wrong Tool Flash")]
    public TextMeshProUGUI txt_WrongTool;
    public Color wrongFlashColor = new Color(1f, 0.25f, 0.25f);
    public float wrongFlashTime  = 1.5f;

    [Header("Complete Panel Text")]
    public TextMeshProUGUI txt_CompleteMessage;

    [Header("Colors")]
    public Color normalColor   = Color.white;
    public Color memoryColor   = new Color(1f, 0.85f, 0.2f);
    public Color progressColor = new Color(0.2f, 1f, 0.4f);

    // ── Tool display maps ─────────────────────────────────────────────────
    static readonly Dictionary<ToolType, string> ToolNames = new Dictionary<ToolType, string>
    {
        { ToolType.WateringCan, "Watering Can" },
        { ToolType.Scissors,    "Scissors (Prune)" },
        { ToolType.Shovel,      "Shovel (Weed)" },
        { ToolType.Fertilizer,  "Fertilizer" }
    };

    static readonly Dictionary<ToolType, string> ToolActions = new Dictionary<ToolType, string>
    {
        { ToolType.WateringCan, "Water this plant" },
        { ToolType.Scissors,    "Prune this plant" },
        { ToolType.Shovel,      "Remove weeds here" },
        { ToolType.Fertilizer,  "Fertilize this plant" }
    };

    // Emoji-style icons using Unicode (renders in TMP with proper font)
    static readonly Dictionary<ToolType, string> ToolIcons = new Dictionary<ToolType, string>
    {
        { ToolType.WateringCan, "[Water]" },
        { ToolType.Scissors,    "[Prune]" },
        { ToolType.Shovel,      "[Weed]"  },
        { ToolType.Fertilizer,  "[Feed]"  }
    };

    // ── Unity ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (taskPanel      != null) taskPanel.SetActive(false);
        if (overviewPanel  != null) overviewPanel.SetActive(false);
        if (completePanel  != null) completePanel.SetActive(false);
        if (holdProgressRoot != null) holdProgressRoot.SetActive(false);
        if (txt_WrongTool  != null) txt_WrongTool.gameObject.SetActive(false);
        if (txt_MemoryHint != null) txt_MemoryHint.gameObject.SetActive(false);
    }

    // ── Public API — task panel ───────────────────────────────────────────

    public void ShowTask(PlantTask task, int taskIndex, int totalTasks)
    {
        if (overviewPanel  != null) overviewPanel.SetActive(false);
        if (completePanel  != null) completePanel.SetActive(false);
        if (taskPanel      != null) taskPanel.SetActive(true);

        if (txt_TaskNumber != null)
            txt_TaskNumber.text = $"Task {taskIndex} / {totalTasks}";

        if (txt_PlantName != null)
        {
            txt_PlantName.text  = task.plantName;
            txt_PlantName.color = normalColor;
        }

        if (txt_Action != null)
            txt_Action.text = ToolActions.ContainsKey(task.requiredTool)
                ? ToolActions[task.requiredTool] : "Interact with this plant";

        if (txt_ToolName != null)
            txt_ToolName.text = "Use: " + (ToolNames.ContainsKey(task.requiredTool)
                ? ToolNames[task.requiredTool] : task.requiredTool.ToString());

        if (txt_MemoryHint != null) txt_MemoryHint.gameObject.SetActive(false);
        if (txt_WrongTool  != null) txt_WrongTool.gameObject.SetActive(false);
    }

    public void HideInstruction()
    {
        if (txt_MemoryHint != null)
        {
            txt_MemoryHint.text  = "Instructions hidden — remember!";
            txt_MemoryHint.color = memoryColor;
            txt_MemoryHint.gameObject.SetActive(true);
        }
        SetMainTextVisible(false);
    }

    // ── Public API — session overview (memory mode) ───────────────────────

    /// <summary>
    /// Show all tasks for the session in a summary panel.
    /// Called at session start in memory / overview mode.
    /// </summary>
    public void ShowSessionOverview(List<PlantTask> tasks)
    {
        if (taskPanel     != null) taskPanel.SetActive(false);
        if (completePanel != null) completePanel.SetActive(false);
        if (overviewPanel != null) overviewPanel.SetActive(true);

        if (txt_OverviewHeader != null)
            txt_OverviewHeader.text = $"Remember these {tasks.Count} tasks!";

        // Fill each row
        for (int i = 0; i < txt_OverviewRows.Length; i++)
        {
            if (txt_OverviewRows[i] == null) continue;

            if (i < tasks.Count)
            {
                PlantTask t  = tasks[i];
                string icon  = ToolIcons.ContainsKey(t.requiredTool)  ? ToolIcons[t.requiredTool]  : "";
                string action = ToolActions.ContainsKey(t.requiredTool) ? ToolActions[t.requiredTool] : "";
                txt_OverviewRows[i].text = $"{i + 1}. {t.plantName}  —  {icon} {action}";
                txt_OverviewRows[i].gameObject.SetActive(true);
            }
            else
            {
                txt_OverviewRows[i].text = "";
                txt_OverviewRows[i].gameObject.SetActive(false);
            }
        }
    }

    public void HideOverview()
    {
        if (overviewPanel != null) overviewPanel.SetActive(false);
    }

    // ── Public API — hold progress ────────────────────────────────────────

    public void ShowHoldProgress(bool show)
    {
        if (holdProgressRoot != null) holdProgressRoot.SetActive(show);
        if (!show && holdProgressFill != null) holdProgressFill.fillAmount = 0f;
    }

    public void UpdateHoldProgress(float t)
    {
        if (holdProgressFill != null)
        {
            holdProgressFill.fillAmount = t;
            holdProgressFill.color = Color.Lerp(normalColor, progressColor, t);
        }
    }

    // ── Public API — wrong tool ───────────────────────────────────────────

    public void FlashWrongTool(ToolType used, ToolType needed)
    {
        if (txt_WrongTool == null) return;

        string usedName   = ToolNames.ContainsKey(used)   ? ToolNames[used]   : used.ToString();
        string neededName = ToolNames.ContainsKey(needed) ? ToolNames[needed] : needed.ToString();

        txt_WrongTool.text  = $"Wrong tool!\nYou used: {usedName}\nNeeded: {neededName}";
        txt_WrongTool.color = wrongFlashColor;
        txt_WrongTool.gameObject.SetActive(true);

        StartCoroutine(HideWrongToolAfterDelay());
    }

    // ── Public API — session complete ─────────────────────────────────────

    public void ShowSessionComplete(int nextTaskCount)
    {
        if (taskPanel     != null) taskPanel.SetActive(false);
        if (overviewPanel != null) overviewPanel.SetActive(false);
        if (completePanel != null)
        {
            completePanel.SetActive(true);
            if (txt_CompleteMessage != null)
                txt_CompleteMessage.text =
                    $"Session Complete!\n\nNext session: {nextTaskCount} tasks\n\nGreat work!";
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void SetMainTextVisible(bool visible)
    {
        if (txt_PlantName != null) txt_PlantName.gameObject.SetActive(visible);
        if (txt_Action    != null) txt_Action.gameObject.SetActive(visible);
        if (txt_ToolName  != null) txt_ToolName.gameObject.SetActive(visible);
    }

    IEnumerator HideWrongToolAfterDelay()
    {
        yield return new WaitForSeconds(wrongFlashTime);
        if (txt_WrongTool != null) txt_WrongTool.gameObject.SetActive(false);
    }
}
