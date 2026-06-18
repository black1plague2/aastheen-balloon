using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// World-space UI panel that shows per-task instructions and session feedback.
/// Enhanced with rehab context, colour-coded tools, encouraging language,
/// and a "hold steady" progress prompt.
/// </summary>
public class TaskInstructionUI : MonoBehaviour
{
    // ── Panels ─────────────────────────────────────────────────────────────
    [Header("Panels")]
    public GameObject taskPanel;
    public GameObject overviewPanel;
    public GameObject completePanel;

    // ── Task Panel ─────────────────────────────────────────────────────────
    [Header("Task Panel — Header")]
    public TextMeshProUGUI txt_TaskBadge;       // "Task 2 of 5"
    public Image           img_TaskBadgeBG;     // badge background colour tint

    [Header("Task Panel — Plant & Action")]
    public TextMeshProUGUI txt_PlantName;       // "Tomato Plant"
    public TextMeshProUGUI txt_Action;          // full action + movement cue

    [Header("Task Panel — Tool")]
    public Image           img_ToolColor;       // coloured square / icon for tool
    public TextMeshProUGUI txt_ToolName;        // "Watering Can"
    public TextMeshProUGUI txt_RehabTip;        // "Exercises: forearm supination & shoulder reach"

    [Header("Task Panel — Memory Mode")]
    public TextMeshProUGUI txt_MemoryHint;      // "Instructions hidden — remember!"

    // ── Session Overview ───────────────────────────────────────────────────
    [Header("Session Overview")]
    public TextMeshProUGUI   txt_OverviewHeader;
    [Tooltip("One TMP text per overview row")]
    public TextMeshProUGUI[] txt_OverviewRows;

    // ── Hold Progress ──────────────────────────────────────────────────────
    [Header("Hold Progress Bar")]
    public GameObject      holdProgressRoot;
    public Image           holdProgressFill;
    public TextMeshProUGUI txt_HoldPrompt;      // "Hold steady..." / "Almost there!"

    // ── Wrong-Tool Feedback ────────────────────────────────────────────────
    [Header("Wrong-Tool Feedback")]
    public TextMeshProUGUI txt_WrongTool;
    public float           wrongFlashTime = 2.2f;

    // ── Complete Panel ─────────────────────────────────────────────────────
    [Header("Complete Panel")]
    public TextMeshProUGUI txt_CompleteHeading;
    public TextMeshProUGUI txt_CompleteMessage;
    public TextMeshProUGUI txt_NextSession;

    // ── Colours ────────────────────────────────────────────────────────────
    [Header("Colours")]
    public Color colorNormal   = Color.white;
    public Color colorMemory   = new Color(1f,  0.85f, 0.20f);
    public Color colorProgress = new Color(0.2f, 1f,  0.45f);
    public Color colorError    = new Color(1f,  0.30f, 0.30f);
    public Color colorSuccess  = new Color(0.2f, 1f,  0.50f);
    public Color colorAccent   = new Color(0.4f, 0.85f, 1f);

    // ── Tool data ──────────────────────────────────────────────────────────

    static readonly Dictionary<ToolType, string> ToolNames = new()
    {
        { ToolType.WateringCan, "Watering Can"  },
        { ToolType.Scissors,    "Scissors"       },
        { ToolType.Shovel,      "Shovel"          },
        { ToolType.Fertilizer,  "Fertilizer"      }
    };

    // Two-line action: what to do + the rehab movement cue
    static readonly Dictionary<ToolType, string> ToolActions = new()
    {
        { ToolType.WateringCan,
            "Water this plant\n<size=80%>Reach forward and rotate your palm upward</size>" },
        { ToolType.Scissors,
            "Prune this plant\n<size=80%>Open and close your fingers to snip</size>" },
        { ToolType.Shovel,
            "Remove weeds here\n<size=80%>Scoop and lift with a smooth wrist motion</size>" },
        { ToolType.Fertilizer,
            "Fertilize this plant\n<size=80%>Rotate your palm downward to sprinkle</size>" }
    };

    static readonly Dictionary<ToolType, string> RehabTips = new()
    {
        { ToolType.WateringCan, "Exercises: forearm supination & shoulder reach" },
        { ToolType.Scissors,    "Exercises: grip strength & finger extension"    },
        { ToolType.Shovel,      "Exercises: wrist flexion & elbow control"       },
        { ToolType.Fertilizer,  "Exercises: forearm pronation & grasp control"   }
    };

    static readonly Dictionary<ToolType, Color> ToolColors = new()
    {
        { ToolType.WateringCan, new Color(0.20f, 0.60f, 1.00f) },  // blue
        { ToolType.Scissors,    new Color(1.00f, 0.50f, 0.20f) },  // orange
        { ToolType.Shovel,      new Color(0.60f, 0.40f, 0.20f) },  // brown
        { ToolType.Fertilizer,  new Color(0.35f, 0.80f, 0.30f) }   // green
    };

    static readonly Dictionary<ToolType, string> WrongToolHints = new()
    {
        { ToolType.WateringCan, "You need the Watering Can — look for the blue glow"   },
        { ToolType.Scissors,    "You need the Scissors — look for the orange glow"      },
        { ToolType.Shovel,      "You need the Shovel — look for the brown glow"         },
        { ToolType.Fertilizer,  "You need the Fertilizer — look for the green glow"     }
    };

    static readonly string[] CompleteMessages =
    {
        "Excellent work! Your persistence is building real strength.",
        "Great effort! Every session brings you closer to your goals.",
        "Well done! Consistency is the key to recovery — keep it up!",
        "Fantastic! Your coordination improves with every session.",
        "Amazing focus today! You should be proud of that effort."
    };

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (taskPanel        != null) taskPanel.SetActive(false);
        if (overviewPanel    != null) overviewPanel.SetActive(false);
        if (completePanel    != null) completePanel.SetActive(false);
        if (holdProgressRoot != null) holdProgressRoot.SetActive(false);
        if (txt_WrongTool    != null) txt_WrongTool.gameObject.SetActive(false);
        if (txt_MemoryHint   != null) txt_MemoryHint.gameObject.SetActive(false);
    }

    // ── Task panel ─────────────────────────────────────────────────────────

    public void ShowTask(PlantTask task, int taskIndex, int totalTasks)
    {
        if (overviewPanel != null) overviewPanel.SetActive(false);
        if (completePanel != null) completePanel.SetActive(false);
        if (taskPanel     != null) taskPanel.SetActive(true);

        // Badge
        if (txt_TaskBadge != null)
            txt_TaskBadge.text = $"Task {taskIndex} of {totalTasks}";

        // Tint badge background with tool colour
        Color toolCol = ToolColors.TryGetValue(task.requiredTool, out Color tc) ? tc : colorAccent;
        if (img_TaskBadgeBG != null) img_TaskBadgeBG.color = new Color(toolCol.r, toolCol.g, toolCol.b, 0.25f);

        // Plant name
        if (txt_PlantName != null)
        {
            txt_PlantName.text  = task.plantName;
            txt_PlantName.color = colorNormal;
        }

        // Action (two-line with movement cue)
        if (txt_Action != null)
            txt_Action.text = ToolActions.TryGetValue(task.requiredTool, out string act)
                ? act : "Interact with this plant";

        // Tool name + colour swatch
        if (txt_ToolName != null)
            txt_ToolName.text = ToolNames.TryGetValue(task.requiredTool, out string tn) ? tn : task.requiredTool.ToString();

        if (img_ToolColor != null) img_ToolColor.color = toolCol;

        // Rehab tip
        if (txt_RehabTip != null)
        {
            txt_RehabTip.text  = RehabTips.TryGetValue(task.requiredTool, out string tip) ? tip : "";
            txt_RehabTip.color = colorAccent;
        }

        // Clear transient elements
        if (txt_MemoryHint   != null) txt_MemoryHint.gameObject.SetActive(false);
        if (txt_WrongTool    != null) txt_WrongTool.gameObject.SetActive(false);
        if (holdProgressRoot != null) holdProgressRoot.SetActive(false);
    }

    public void HideInstruction()
    {
        SetMainTextVisible(false);
        if (txt_MemoryHint != null)
        {
            txt_MemoryHint.text  = "Instructions hidden — can you remember?";
            txt_MemoryHint.color = colorMemory;
            txt_MemoryHint.gameObject.SetActive(true);
        }
    }

    // ── Session overview ───────────────────────────────────────────────────

    public void ShowSessionOverview(List<PlantTask> tasks)
    {
        if (taskPanel     != null) taskPanel.SetActive(false);
        if (completePanel != null) completePanel.SetActive(false);
        if (overviewPanel != null) overviewPanel.SetActive(true);

        if (txt_OverviewHeader != null)
            txt_OverviewHeader.text = $"Remember these {tasks.Count} tasks:";

        for (int i = 0; i < txt_OverviewRows.Length; i++)
        {
            if (txt_OverviewRows[i] == null) continue;

            if (i < tasks.Count)
            {
                PlantTask t     = tasks[i];
                string toolName = ToolNames.TryGetValue(t.requiredTool, out string tn) ? tn : t.requiredTool.ToString();
                txt_OverviewRows[i].text = $"{i + 1}.  {t.plantName}  →  {toolName}";
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

    // ── Hold progress bar ──────────────────────────────────────────────────

    public void ShowHoldProgress(bool show)
    {
        if (holdProgressRoot != null) holdProgressRoot.SetActive(show);
        if (txt_HoldPrompt   != null) txt_HoldPrompt.text = "Hold steady...";
        if (!show && holdProgressFill != null) holdProgressFill.fillAmount = 0f;
    }

    public void UpdateHoldProgress(float t)
    {
        if (holdProgressFill != null)
        {
            holdProgressFill.fillAmount = t;
            holdProgressFill.color      = Color.Lerp(new Color(0.4f, 0.7f, 1f), colorProgress, t);
        }
        if (txt_HoldPrompt != null)
            txt_HoldPrompt.text = t > 0.78f ? "Almost there!" : "Hold steady...";
    }

    // ── Wrong-tool feedback ────────────────────────────────────────────────

    public void FlashWrongTool(ToolType used, ToolType needed)
    {
        if (txt_WrongTool == null) return;

        string hint = WrongToolHints.TryGetValue(needed, out string h)
            ? h : "Check the instruction panel for the right tool";

        txt_WrongTool.text  = $"Not quite!\n<size=85%>{hint}</size>";
        txt_WrongTool.color = colorError;
        txt_WrongTool.gameObject.SetActive(true);

        StopCoroutine(nameof(HideWrongToolCR));
        StartCoroutine(nameof(HideWrongToolCR));
    }

    // ── Session complete ───────────────────────────────────────────────────

    public void ShowSessionComplete(int nextTaskCount)
    {
        if (taskPanel     != null) taskPanel.SetActive(false);
        if (overviewPanel != null) overviewPanel.SetActive(false);
        if (completePanel != null) completePanel.SetActive(true);

        if (txt_CompleteHeading != null)
        {
            txt_CompleteHeading.text  = "Session Complete!";
            txt_CompleteHeading.color = colorSuccess;
        }

        if (txt_CompleteMessage != null)
        {
            txt_CompleteMessage.text  = CompleteMessages[Random.Range(0, CompleteMessages.Length)];
            txt_CompleteMessage.color = colorNormal;
        }

        if (txt_NextSession != null)
        {
            txt_NextSession.text  = $"Next session: {nextTaskCount} tasks";
            txt_NextSession.color = colorAccent;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void SetMainTextVisible(bool on)
    {
        if (txt_PlantName != null) txt_PlantName.gameObject.SetActive(on);
        if (txt_Action    != null) txt_Action.gameObject.SetActive(on);
        if (txt_ToolName  != null) txt_ToolName.gameObject.SetActive(on);
        if (img_ToolColor != null) img_ToolColor.gameObject.SetActive(on);
        if (txt_RehabTip  != null) txt_RehabTip.gameObject.SetActive(on);
    }

    IEnumerator HideWrongToolCR()
    {
        yield return new WaitForSeconds(wrongFlashTime);
        if (txt_WrongTool != null) txt_WrongTool.gameObject.SetActive(false);
    }
}
