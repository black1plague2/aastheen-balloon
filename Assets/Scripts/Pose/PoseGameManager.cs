using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to a persistent GameObject in Game_Pose scene.
// Reads PoseSettings from PlaylistManager (Verse V2).
// Counts reps per pose (only when held for holdDurationSec seconds).
// Posts PoseGameMetrics to VerseClient when session ends.
public class PoseGameManager : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("All PoseLogger components in the scene — one per pose sphere")]
    public PoseLogger[] poseLoggers;

    [Header("HUD")]
    public TMPro.TextMeshProUGUI timerText;
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI poseNameText;

    // ── Runtime state ──────────────────────────────────────────────────────────
    private PoseSettings _settings;
    private float        _timeRemaining;
    private bool         _sessionActive;

    // Per-pose rep counts keyed by poseName string
    private readonly Dictionary<string, int> _poseCounts = new Dictionary<string, int>();
    private int _totalReps;

    void Start()
    {
        // Read settings from Verse
        _settings = PlaylistManager.Instance?.PoseSettings ?? new PoseSettings();

        // Apply hold duration to every PoseLogger
        foreach (var pl in poseLoggers)
        {
            pl.holdDuration = _settings.holdDurationSec;
            pl.OnPoseHeld  += HandlePoseHeld;
            _poseCounts[pl.poseName] = 0;
        }

        _timeRemaining = _settings.sessionDuration;
        StartSession();
    }

    void StartSession()
    {
        _sessionActive = true;
        UpdateHUD();
        Debug.Log($"[POSE GAME] Session started — {_settings.sessionDuration}s, hold={_settings.holdDurationSec}s");
    }

    void Update()
    {
        if (!_sessionActive) return;

        _timeRemaining -= Time.deltaTime;
        UpdateHUD();

        if (_timeRemaining <= 0f)
            EndSession();
    }

    private void HandlePoseHeld(string poseName)
    {
        if (!_sessionActive) return;

        if (_poseCounts.ContainsKey(poseName))
            _poseCounts[poseName]++;
        else
            _poseCounts[poseName] = 1;

        _totalReps++;
        Debug.Log($"[POSE GAME] {poseName} count={_poseCounts[poseName]}  total={_totalReps}");

        if (poseNameText != null)
            poseNameText.text = $"✓ {poseName}";

        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (timerText != null)
            timerText.text = $"{Mathf.Max(0, _timeRemaining):0}s";
        if (scoreText != null)
            scoreText.text = $"Reps: {_totalReps}";
    }

    private void EndSession()
    {
        _sessionActive = false;

        foreach (var pl in poseLoggers)
            pl.OnPoseHeld -= HandlePoseHeld;

        float elapsed = _settings.sessionDuration - Mathf.Max(0, _timeRemaining);

        var metrics = new PoseGameMetrics
        {
            total_reps       = _totalReps,
            session_duration = elapsed,
            thumbsup_count   = GetCount("Thumbs Up"),
            yo_count         = GetCount("Yo"),
            nice_count       = GetCount("Nice"),
            four_fingers_count = GetCount("4 Fingers"),
            bunny_count      = GetCount("Bunny"),
            hold_duration_sec = _settings.holdDurationSec,
        };

        Debug.Log($"[POSE GAME] Session ended. Total reps={_totalReps}");

        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasSession)
        {
            var payload = new PoseSessionResultsPayload { game_metrics = metrics };
            VerseClient.Instance?.PostPoseSessionResults(
                PlaylistManager.Instance.SessionId,
                PlaylistManager.Instance.SessionToken,
                payload
            );
        }

        // Return to Bootstrap after brief delay
        Invoke(nameof(ReturnToBootstrap), 3f);
    }

    private int GetCount(string name)
    {
        _poseCounts.TryGetValue(name, out int v);
        return v;
    }

    private void ReturnToBootstrap()
    {
        PlaylistManager.Instance?.Clear();
        SceneManager.LoadScene("Bootstrap");
    }
}
