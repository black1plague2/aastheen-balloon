using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verse-aware Paint the Wall session controller. Runs at order -90: after
/// PaintWallAutoWire (-100) wires the scene, but before PaintTaskManager's
/// own Start() begins the session. Mirrors GardenGameManager.
/// </summary>
[DefaultExecutionOrder(-90)]
public class PaintWallGameManager : MonoBehaviour
{
    public static PaintWallGameManager Instance { get; private set; }

    private PaintTaskManager _ptm;
    private float _startTime;
    private bool  _resultsSent;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _ptm = PaintTaskManager.Instance;
        if (_ptm == null)
        {
            Debug.LogError("[PaintWallGame] PaintTaskManager not found.");
            return;
        }

        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasSession)
        {
            var s = PlaylistManager.Instance.PaintWallSettings;
            if (s != null)
            {
                _ptm.wallHeight       = s.wallHeight;
                _ptm.paintingAreaSize = new Vector2(s.paintingAreaWidth, s.paintingAreaHeight);
                _ptm.sessionDuration  = s.sessionDuration;
                _ptm.pattern          = ParsePattern(s.paintingPattern);
                _ptm.requiredTool     = s.toolType != null && s.toolType.ToLower() == "roller"
                                            ? PaintToolType.Roller
                                            : PaintToolType.Brush;
            }
        }
        // PTM.Start() fires next (order 0) and calls StartSession() with these values.
        _startTime = Time.time;
    }

    static PaintPatternType ParsePattern(string p)
    {
        switch (p != null ? p.ToLower() : "")
        {
            case "vertical": return PaintPatternType.Vertical;
            case "circular": return PaintPatternType.Circular;
            case "zigzag":   return PaintPatternType.Zigzag;
            default:         return PaintPatternType.Horizontal;
        }
    }

    private void Update()
    {
        if (_resultsSent || _ptm == null) return;
        if (!_ptm.IsSessionActive() && Time.time - _startTime > 1f)
            SubmitAndAdvance();
    }

    private void SubmitAndAdvance()
    {
        _resultsSent = true;

        var metrics = new PaintWallGameMetrics
        {
            coverage_percent  = _ptm.GetCoveragePercent(),
            strokes_completed = _ptm.GetStrokesCompleted(),
            mistakes          = _ptm.GetMistakes(),
            session_duration  = Time.time - _startTime,
            tool_used         = _ptm.requiredTool.ToString().ToLower(),
            painting_pattern  = _ptm.pattern.ToString().ToLower(),
            exercise_tags     = _ptm.GetExerciseTags(),
        };

        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasSession
            && VerseClient.Instance != null)
        {
            VerseClient.Instance.SubmitPaintWallResults(
                PlaylistManager.Instance.SessionId,
                PlaylistManager.Instance.SessionToken,
                metrics,
                onDone:  () => { Debug.Log("[PaintWallGame] Results submitted."); AdvanceOrReturn(); },
                onError: err => { Debug.LogWarning("[PaintWallGame] Submit failed: " + err); AdvanceOrReturn(); });
        }
        else
        {
            AdvanceOrReturn();
        }
    }

    private void AdvanceOrReturn()
    {
        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasNextGame)
        {
            string next = PlaylistManager.Instance.AdvanceToNextGame();
            StartCoroutine(LoadScene(next, 2f));
        }
        else
        {
            PlaylistManager.Instance?.Clear();
            StartCoroutine(LoadScene("Bootstrap", 3f));
        }
    }

    private IEnumerator LoadScene(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"[PaintWallGame] Loading → {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
