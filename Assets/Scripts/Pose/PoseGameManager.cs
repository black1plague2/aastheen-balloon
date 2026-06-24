using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PoseGameManager : MonoBehaviour
{
    private PoseSettings _settings;
    private float        _timeRemaining;
    private bool         _sessionActive;
    private PoseLogger[] _loggers;
    private readonly Dictionary<string, int> _poseCounts = new Dictionary<string, int>();
    private int _totalReps;

    void Start()
    {
        _settings = PlaylistManager.Instance?.PoseSettings ?? new PoseSettings();

        // Auto-find all PoseLoggers in the scene — no manual wiring needed
        _loggers = FindObjectsOfType<PoseLogger>();
        foreach (var pl in _loggers)
        {
            pl.holdDuration   = _settings.holdDurationSec;
            pl.OnPoseHeld    += HandlePoseHeld;
            _poseCounts[pl.poseName] = 0;
        }

        _timeRemaining = _settings.sessionDuration;
        _sessionActive = true;
        Debug.Log($"[POSE] Started — {_loggers.Length} poses, {_settings.sessionDuration}s, hold={_settings.holdDurationSec}s");
    }

    void Update()
    {
        if (!_sessionActive) return;
        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f) EndSession();
    }

    private void HandlePoseHeld(string poseName)
    {
        if (!_sessionActive) return;
        _poseCounts[poseName] = _poseCounts.GetValueOrDefault(poseName, 0) + 1;
        _totalReps++;
        Debug.Log($"[POSE] {poseName} held — total={_totalReps}");
    }

    private void EndSession()
    {
        _sessionActive = false;
        foreach (var pl in _loggers) pl.OnPoseHeld -= HandlePoseHeld;

        float elapsed = _settings.sessionDuration - Mathf.Max(0, _timeRemaining);
        var metrics = new PoseGameMetrics
        {
            total_reps         = _totalReps,
            session_duration   = elapsed,
            thumbsup_count     = GetCount("thumbsup"),
            yo_count           = GetCount("yo"),
            nice_count         = GetCount("nice"),
            four_fingers_count = GetCount("4 fingers"),
            bunny_count        = GetCount("bunny"),
            hold_duration_sec  = _settings.holdDurationSec,
        };

        Debug.Log($"[POSE] Session ended. Reps={_totalReps}");

        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasSession)
        {
            var payload = new PoseSessionResultsPayload { game_metrics = metrics };
            VerseClient.Instance?.PostPoseSessionResults(
                PlaylistManager.Instance.SessionId,
                PlaylistManager.Instance.SessionToken,
                payload
            );
        }

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
