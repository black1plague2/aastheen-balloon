using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tracks per-session progress for the Paint the Wall game: coverage%,
/// strokes completed, mistakes, and session time. Drives the world-space
/// HUD canvas and session-complete panel. Mirrors Garden's
/// RehabProgressManager.
/// </summary>
public class PaintProgressManager : MonoBehaviour
{
    [Header("HUD Text")]
    public TextMeshProUGUI txt_PatternExercise;
    public TextMeshProUGUI txt_Tool;
    public TextMeshProUGUI txt_Coverage;
    public TextMeshProUGUI txt_Strokes;
    public TextMeshProUGUI txt_Mistakes;
    public TextMeshProUGUI txt_SessionTime;

    [Header("HUD Bar")]
    public Slider coverageSlider;

    [Header("Session Complete Panel")]
    public GameObject sessionCompletePanel;
    public TextMeshProUGUI txt_SummaryTitle;
    public TextMeshProUGUI txt_SummaryBody;

    [Header("Colors")]
    public Color goodColor    = new Color(0.2f, 1f, 0.4f);
    public Color badColor     = new Color(1f, 0.3f, 0.3f);
    public Color neutralColor = Color.white;

    private float sessionStartTime;
    private int   sessionMistakes;
    private int   totalSessions;

    public void BeginSession()
    {
        sessionStartTime = Time.time;
        sessionMistakes  = 0;
        UpdateCoverage(0f);

        if (txt_Strokes  != null) { txt_Strokes.text  = "Strokes: 0";  txt_Strokes.color  = neutralColor; }
        if (txt_Mistakes != null) { txt_Mistakes.text = "Mistakes: 0"; txt_Mistakes.color = neutralColor; }
        if (sessionCompletePanel != null) sessionCompletePanel.SetActive(false);
    }

    public void SetPresetLabels(string patternExerciseLabel, string toolLabel)
    {
        if (txt_PatternExercise != null) txt_PatternExercise.text = patternExerciseLabel;
        if (txt_Tool != null) txt_Tool.text = toolLabel;
    }

    public void UpdateCoverage(float percent01)
    {
        if (txt_Coverage != null)
        {
            txt_Coverage.text  = $"Coverage: {percent01 * 100f:F0}%";
            txt_Coverage.color = percent01 >= 0.85f ? goodColor : neutralColor;
        }
        if (coverageSlider != null)
            coverageSlider.value = percent01;
    }

    public void RecordStroke(int totalStrokes)
    {
        if (txt_Strokes != null)
            txt_Strokes.text = $"Strokes: {totalStrokes}";
    }

    public void RecordMistake()
    {
        sessionMistakes++;
        if (txt_Mistakes != null)
        {
            txt_Mistakes.text  = $"Mistakes: {sessionMistakes}";
            txt_Mistakes.color = badColor;
        }
    }

    public void OnSessionComplete(float coveragePercent, int strokes, float elapsed)
    {
        totalSessions++;
        Debug.Log($"[PaintProgress] Session {totalSessions} complete — Coverage: {coveragePercent:P0} Strokes: {strokes} Time: {elapsed:F1}s");

        if (txt_SessionTime != null)
            txt_SessionTime.text = $"Time: {elapsed:F0}s";

        if (sessionCompletePanel != null)
        {
            sessionCompletePanel.SetActive(true);
            if (txt_SummaryTitle != null) txt_SummaryTitle.text = "Session Complete!";
            if (txt_SummaryBody  != null) txt_SummaryBody.text  =
                $"Coverage: {coveragePercent:P0}\nStrokes: {strokes}\nMistakes: {sessionMistakes}\nTime: {elapsed:F0}s";
        }
    }

    public int GetSessionMistakes() => sessionMistakes;
}
