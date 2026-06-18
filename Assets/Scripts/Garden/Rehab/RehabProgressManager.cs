using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Tracks per-session and cumulative rehabilitation progress.
/// Records correct actions, mistakes, session time, and difficulty level.
/// Displays a summary after each session.
/// </summary>
public class RehabProgressManager : MonoBehaviour
{
    [Header("Session UI")]
    public TextMeshProUGUI txt_SessionScore;
    public TextMeshProUGUI txt_SessionMistakes;
    public TextMeshProUGUI txt_SessionTime;
    public TextMeshProUGUI txt_DifficultyLevel;
    public TextMeshProUGUI txt_CumulativeScore;

    [Header("Colors")]
    public Color goodColor    = new Color(0.2f, 1f, 0.4f);
    public Color badColor     = new Color(1f, 0.3f, 0.3f);
    public Color neutralColor = Color.white;

    // ── Session state ─────────────────────────────────────────────────────
    private int   sessionCorrect  = 0;
    private int   sessionMistakes = 0;
    private float sessionStartTime;

    // ── Cumulative state ──────────────────────────────────────────────────
    private int   totalCorrect   = 0;
    private int   totalMistakes  = 0;
    private int   totalSessions  = 0;
    private float totalTimePlayed = 0f;

    // ── Unity ─────────────────────────────────────────────────────────────
    void Start()
    {
        BeginSession();
        RefreshUI();
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void BeginSession()
    {
        sessionCorrect   = 0;
        sessionMistakes  = 0;
        sessionStartTime = Time.time;
        RefreshUI();
    }

    public void RecordCorrect()
    {
        sessionCorrect++;
        totalCorrect++;
        RefreshUI();
        Debug.Log($"[RehabProgress] Correct: {sessionCorrect}  Total: {totalCorrect}");
    }

    public void RecordMistake()
    {
        sessionMistakes++;
        totalMistakes++;
        RefreshUI();
        Debug.Log($"[RehabProgress] Mistake: {sessionMistakes}  Total: {totalMistakes}");
    }

    public void OnSessionComplete(List<PlantTask> tasks)
    {
        totalSessions++;
        float elapsed = Time.time - sessionStartTime;
        totalTimePlayed += elapsed;

        int taskCount = tasks != null ? tasks.Count : 0;

        Debug.Log($"[RehabProgress] Session {totalSessions} complete — " +
                  $"Correct: {sessionCorrect}/{taskCount}  " +
                  $"Mistakes: {sessionMistakes}  " +
                  $"Time: {elapsed:F1}s");

        RefreshUI();
        RefreshSummaryUI(taskCount, elapsed);
    }

    // ── UI refresh ────────────────────────────────────────────────────────

    void RefreshUI()
    {
        if (txt_SessionScore != null)
        {
            txt_SessionScore.text  = $"Correct: {sessionCorrect}";
            txt_SessionScore.color = sessionCorrect > 0 ? goodColor : neutralColor;
        }

        if (txt_SessionMistakes != null)
        {
            txt_SessionMistakes.text  = $"Mistakes: {sessionMistakes}";
            txt_SessionMistakes.color = sessionMistakes > 0 ? badColor : neutralColor;
        }

        if (txt_SessionTime != null)
        {
            float elapsed = Time.time - sessionStartTime;
            txt_SessionTime.text  = $"Time: {elapsed:F0}s";
            txt_SessionTime.color = neutralColor;
        }

        if (txt_CumulativeScore != null)
        {
            txt_CumulativeScore.text  = $"Total: {totalCorrect} correct / {totalSessions} sessions";
            txt_CumulativeScore.color = neutralColor;
        }
    }

    void RefreshSummaryUI(int taskCount, float elapsed)
    {
        if (txt_DifficultyLevel != null)
        {
            int level = PlantTaskManager.Instance != null
                ? PlantTaskManager.Instance.tasksPerSession : taskCount;
            txt_DifficultyLevel.text  = $"Level: {level} tasks/session";
            txt_DifficultyLevel.color = neutralColor;
        }

        if (txt_SessionTime != null)
        {
            txt_SessionTime.text  = $"Time: {elapsed:F1}s";
            txt_SessionTime.color = neutralColor;
        }
    }

    // ── Accessors ─────────────────────────────────────────────────────────
    public int   GetSessionCorrect()   => sessionCorrect;
    public int   GetSessionMistakes()  => sessionMistakes;
    public int   GetTotalCorrect()     => totalCorrect;
    public int   GetTotalMistakes()    => totalMistakes;
    public int   GetTotalSessions()    => totalSessions;
    public float GetTotalTimePlayed()  => totalTimePlayed;
}
