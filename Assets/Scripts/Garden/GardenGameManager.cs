using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Verse-aware garden session controller.
// Runs at order -90: after RehabAutoWire (-100) wires the scene,
// but before PlantTaskManager (0) calls StartSession() in its own Start().
// This lets us inject prescription settings before gameplay begins.
[DefaultExecutionOrder(-90)]
public class GardenGameManager : MonoBehaviour
{
    public static GardenGameManager Instance { get; private set; }

    [Header("Intro")]
    [Tooltip("Show the how-to-play intro before the first session")]
    public bool showIntroOnStart = true;

    private PlantTaskManager _ptm;
    private float _startTime;
    private bool  _resultsSent;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _ptm = PlantTaskManager.Instance;
        if (_ptm == null)
        {
            Debug.LogError("[GardenGame] PlantTaskManager not found.");
            return;
        }

        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasSession)
        {
            var s = PlaylistManager.Instance.GardenSettings;
            if (s != null)
            {
                _ptm.tasksPerSession          = s.tasksPerSession;
                _ptm.gameMode                 = s.gameMode.ToLower() == "freeorder"
                                                    ? GardenGameMode.FreeOrder
                                                    : GardenGameMode.Sequenced;
                _ptm.memoryModeEnabled        = s.memoryMode;
                _ptm.showSessionOverviewFirst = s.showOverviewFirst;
                _ptm.instructionDisplayTime   = s.instructionDisplayTime;
            }
        }

        _startTime = Time.time;

        // If the intro UI is present and requested, show it first.
        // PTM.autoStartSession was set false by the time PTM.Start() ran (order 0 > -90),
        // so we call StartSession() ourselves inside the OnDone callback.
        if (showIntroOnStart && GameIntroUI.Instance != null)
        {
            _ptm.autoStartSession = false;
            GameIntroUI.Instance.Show(onDone: () =>
            {
                _startTime = Time.time;  // reset timer — count only gameplay time
                _ptm.StartSession();
            });
        }
        // If no intro UI is in the scene, PTM auto-starts as normal.
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

        var pm = _ptm.progressManager;
        var metrics = new GardenGameMetrics
        {
            correct_tasks    = pm != null ? pm.GetSessionCorrect()  : _ptm.GetTotalTasks(),
            mistakes         = pm != null ? pm.GetSessionMistakes() : 0,
            total_tasks      = _ptm.GetTotalTasks(),
            session_duration = Time.time - _startTime,
            difficulty_level = _ptm.tasksPerSession,
            game_mode        = _ptm.gameMode.ToString().ToLower(),
        };

        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasSession
            && VerseClient.Instance != null)
        {
            VerseClient.Instance.SubmitGardenResults(
                PlaylistManager.Instance.SessionId,
                PlaylistManager.Instance.SessionToken,
                metrics,
                onDone:  () => { Debug.Log("[GardenGame] Results submitted."); AdvanceOrReturn(); },
                onError: err => { Debug.LogWarning("[GardenGame] Submit failed: " + err); AdvanceOrReturn(); });
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
        Debug.Log($"[GardenGame] Loading → {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Garden/Force Session Complete (test)")]
    static void EditorForceComplete()
    {
        if (PlantTaskManager.Instance != null)
            Debug.Log("[GardenGame] Force-complete: call PTM.RestartSession() to reset.");
        else
            Debug.LogWarning("[GardenGame] PlantTaskManager not found — enter Play Mode first.");
    }
#endif
}
