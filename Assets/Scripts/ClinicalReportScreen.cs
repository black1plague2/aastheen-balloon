using System;
using UnityEngine;
using TMPro;

[Serializable]
public class SessionReport
{
    public int      totalScore;
    public float    accuracy;
    public float    avgReactionTimeMs;
    public int      maxStreak;
    public string[] badges;
    public float    spatialLeft, spatialCenter, spatialRight;
    public float    sequenceAccuracy;
    public float    gazeLeft, gazeCenter, gazeRight;
    public bool     usnMode, sequenceMode;
    public int      cbsScore, mptScore;
    public int      difficultyLevel;
    public int      correctPops, incorrectPops, missedBalloons;
    public string   gameMode;
}

public class ClinicalReportScreen : MonoBehaviour
{
    public static ClinicalReportScreen Instance;

    [Header("Canvas")]
    public GameObject reportCanvas; // WorldSpace canvas — hidden by default

    [Header("Text Fields")]
    public TMP_Text titleText;
    public TMP_Text scoreText;
    public TMP_Text accuracyText;
    public TMP_Text reactionTimeText;
    public TMP_Text streakText;
    public TMP_Text badgesText;
    public TMP_Text spatialText;
    public TMP_Text sequenceText;
    public TMP_Text cbsMptText;
    public TMP_Text difficultyText;
    public TMP_Text gazeText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (reportCanvas) reportCanvas.SetActive(false);
    }

    public void ShowReport(SessionReport r)
    {
        if (reportCanvas) reportCanvas.SetActive(true);

        Set(titleText,        "SESSION REPORT");
        Set(scoreText,        $"Score: {r.totalScore}     Difficulty: Level {r.difficultyLevel}");
        Set(accuracyText,     $"Accuracy: {r.accuracy:P0}    ({r.correctPops} correct / {r.incorrectPops} wrong / {r.missedBalloons} missed)");
        Set(reactionTimeText, r.avgReactionTimeMs > 0f ? $"Avg Reaction Time: {r.avgReactionTimeMs:F0} ms" : "");
        Set(streakText,       $"Best Streak: ×{r.maxStreak}");
        Set(badgesText,       r.badges != null && r.badges.Length > 0
            ? "Badges:  " + string.Join("   |   ", r.badges)
            : "No badges earned this session");

        if (r.usnMode)
            Set(spatialText, $"Spatial Distribution\nLeft: {r.spatialLeft:P0}     Centre: {r.spatialCenter:P0}     Right: {r.spatialRight:P0}");
        else
            Set(spatialText, "");

        Set(sequenceText, r.sequenceMode
            ? $"Sequence Accuracy: {r.sequenceAccuracy:P0}"
            : "");

        string cbsPart = r.cbsScore >= 0 ? $"CBS: {r.cbsScore} / 30" : "";
        string mptPart = r.mptScore >= 0 ? $"  MPT: {r.mptScore} / 100" : "";
        Set(cbsMptText, cbsPart + mptPart);

        Set(difficultyText, $"Mode: {r.gameMode}     Final Difficulty: Level {r.difficultyLevel}");
        Set(gazeText,       $"Gaze Distribution\nLeft: {r.gazeLeft:P0}     Centre: {r.gazeCenter:P0}     Right: {r.gazeRight:P0}");
    }

    public void Hide()
    {
        if (reportCanvas) reportCanvas.SetActive(false);
    }

    private void Set(TMP_Text field, string value)
    {
        if (field != null) field.text = value;
    }
}
