using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public TMP_Text scoreText;
    public TMP_Text repsText;
    public TMP_Text warningText;
    public TMP_Text timerText;
    public TMP_Text gameModeText;

    [Header("Scene References")]
    public BalloonSpawner balloonSpawner;

    // ── Prescription fields ────────────────────────────────────────────────────
    [HideInInspector] public float    targetRotation    = 60f;
    [HideInInspector] public float    holdTimeMs        = 1000f;
    [HideInInspector] public int      repCount          = 10;
    [HideInInspector] public float    balloonSize       = 0.3f;
    [HideInInspector] public float    balloonSizeMin    = 0.2f;
    [HideInInspector] public float    balloonSizeMax    = 0.5f;
    [HideInInspector] public float    spawnInterval     = 3f;
    [HideInInspector] public float    sessionDuration   = 120f;
    [HideInInspector] public GameMode currentGameMode   = GameMode.Standard;
    [HideInInspector] public int      distractorCount   = 1;
    [HideInInspector] public int      sequenceLength    = 3;
    [HideInInspector] public float    reactionTimeLimit = -1f;
    [HideInInspector] public int      cbsScore          = -1;
    [HideInInspector] public int      mptScore          = -1;

    private bool adaptiveDifficultyEnabled = false;
    private bool usnModeEnabled            = false;

    [HideInInspector] public float latestRotation = 0f;
    [HideInInspector] public float latestSpeed    = 0f;

    // ── Session state ──────────────────────────────────────────────────────────
    private int   score          = 0;
    private int   repsCompleted  = 0;
    private bool  sessionActive  = false;
    private float sessionTimer   = 0f;

    private int          correctPops    = 0;
    private int          incorrectPops  = 0;
    private int          missedBalloons = 0;
    private List<float>  reactionTimes  = new List<float>();

    private Coroutine _warningCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Verse mode: if Bootstrap has already verified a code, load settings and auto-start.
        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasSession)
        {
            var s = PlaylistManager.Instance.Settings;
            if (s != null)
            {
                var msg = new PrescriptionMessage
                {
                    targetRotation    = s.targetRotation,
                    holdTimeMs        = s.holdTimeMs,
                    repCount          = s.repCount,
                    balloonSize       = s.balloonSize,
                    balloonSizeMin    = s.balloonSizeMin,
                    balloonSizeMax    = s.balloonSizeMax,
                    spawnInterval     = s.spawnInterval,
                    sessionDuration   = s.sessionDuration,
                    gameMode          = s.gameMode,
                    distractorCount   = s.distractorCount,
                    sequenceLength    = s.sequenceLength,
                    reactionTimeLimit = s.reactionTimeLimit,
                    adaptiveDifficulty= s.adaptiveDifficulty,
                    usnMode           = s.usnMode,
                    cbsScore          = s.cbsScore,
                    mptScore          = s.mptScore,
                };
                OnPrescriptionReceived(msg);
            }
            StartSession();
        }
    }

    private void Update()
    {
        if (!sessionActive) return;
        sessionTimer -= Time.deltaTime;
        UpdateUI();
        if (sessionTimer <= 0f) EndSession();
    }

    // ── WS / Editor callbacks ─────────────────────────────────────────────────

    public void OnPrescriptionReceived(PrescriptionMessage msg)
    {
        targetRotation    = msg.targetRotation;
        holdTimeMs        = msg.holdTimeMs;
        repCount          = msg.repCount;
        balloonSize       = msg.balloonSize;
        spawnInterval     = msg.spawnInterval;
        sessionDuration   = msg.sessionDuration;
        distractorCount   = Mathf.Max(0, msg.distractorCount);
        sequenceLength    = Mathf.Clamp(msg.sequenceLength, 2, 6);
        reactionTimeLimit = msg.reactionTimeLimit > 0f ? msg.reactionTimeLimit : -1f;
        cbsScore          = msg.cbsScore;
        mptScore          = msg.mptScore;

        balloonSizeMin = msg.balloonSizeMin > 0f ? msg.balloonSizeMin : balloonSize * 0.7f;
        balloonSizeMax = msg.balloonSizeMax > 0f ? msg.balloonSizeMax : balloonSize * 1.3f;

        switch (msg.gameMode != null ? msg.gameMode.ToLower() : "standard")
        {
            case "sequence": currentGameMode = GameMode.Sequence; break;
            case "reaction": currentGameMode = GameMode.Reaction; break;
            case "usn":      currentGameMode = GameMode.USN;      break;
            default:         currentGameMode = GameMode.Standard;  break;
        }

        usnModeEnabled = msg.usnMode || (cbsScore >= 10);

        if (mptScore >= 0 && mptScore < 50)
        {
            balloonSizeMin *= 1.3f;
            balloonSizeMax *= 1.3f;
        }

        adaptiveDifficultyEnabled = msg.adaptiveDifficulty;
        Debug.Log($"[GameManager] Prescription received — mode:{currentGameMode}  reps:{repCount}");
    }

    public void OnPrescriptionReceived(float tRot, float holdMs, int reps,
        float bSize, float spawnInt, float sessDur)
    {
        var msg = new PrescriptionMessage
        {
            targetRotation  = tRot,
            holdTimeMs      = holdMs,
            repCount        = reps,
            balloonSize     = bSize,
            spawnInterval   = spawnInt,
            sessionDuration = sessDur
        };
        OnPrescriptionReceived(msg);
    }

    public void OnCommand(string command)
    {
        switch (command.ToLower())
        {
            case "start":
                if (!sessionActive) StartSession();
                break;
            case "stop":
                if (sessionActive) EndSession();
                break;
            case "pause":
                sessionActive = !sessionActive;
                break;
            case "reset":
                if (sessionActive) EndSession();
                score = repsCompleted = correctPops = incorrectPops = missedBalloons = 0;
                reactionTimes.Clear();
                UpdateUI();
                break;
        }
    }

    public void OnSensorData(float rotation, float speed, string warning)
    {
        latestRotation = rotation;
        latestSpeed    = speed;
        if (_warningCoroutine == null && warningText)
            warningText.text = string.IsNullOrEmpty(warning) ? "" : warning;
    }

    // ── Balloon events ─────────────────────────────────────────────────────────

    public void OnTargetPopped(BalloonType type, float reactionMs, float size, Vector3 position)
    {
        if (!sessionActive) return;

        if (currentGameMode == GameMode.Sequence && SequenceManager.Instance != null)
        {
            bool sequenceCorrect = SequenceManager.Instance.OnBalloonPopped(type);
            if (!sequenceCorrect)
            {
                incorrectPops++;
                score = Mathf.Max(0, score - 50);
                StreakRewardSystem.Instance?.OnIncorrectPop(size);
                DifficultyManager.Instance?.RecordResult(false);
                PopEffectController.Instance?.PlayWrongSequencePop(position);
                FlashWarning("X  WRONG ORDER!", Color.red, 1.5f);
                UpdateUI();
                return;
            }
        }

        correctPops++;
        repsCompleted++;
        reactionTimes.Add(reactionMs);

        int baseScore   = BalloonData.GetBaseScore(size);
        int streakBonus = StreakRewardSystem.Instance != null
            ? StreakRewardSystem.Instance.OnCorrectPop(reactionMs, size)
            : 0;
        score += baseScore + streakBonus;

        USNManager.Instance?.RecordPop(position.x);
        DifficultyManager.Instance?.RecordResult(true);
        PopEffectController.Instance?.PlayCorrectPop(position, type);

        // Rep-level updates no longer sent over WS in Verse mode (session result is POSTed on end).

        UpdateUI();
        if (repsCompleted >= repCount) EndSession();
    }

    public void OnDistractorPopped(BalloonType type, Vector3 position)
    {
        if (!sessionActive) return;
        incorrectPops++;
        score = Mathf.Max(0, score - 50);
        StreakRewardSystem.Instance?.OnIncorrectPop(0.3f);
        DifficultyManager.Instance?.RecordResult(false);
        PopEffectController.Instance?.PlayIncorrectPop(position);
        FlashWarning("X  DISTRACTOR!", new Color(1f, 0.55f, 0f), 1f);
        UpdateUI();
    }

    public void OnBalloonMissed()
    {
        if (!sessionActive) return;
        missedBalloons++;
        StreakRewardSystem.Instance?.OnMissedBalloon();
        DifficultyManager.Instance?.RecordResult(false);
    }

    // ── Session flow ───────────────────────────────────────────────────────────

    private void StartSession()
    {
        score = repsCompleted = correctPops = incorrectPops = missedBalloons = 0;
        reactionTimes.Clear();
        sessionTimer  = sessionDuration;
        sessionActive = true;

        DifficultyManager.Instance?.Initialize(adaptiveDifficultyEnabled);
        USNManager.Instance?.Initialize(usnModeEnabled || currentGameMode == GameMode.USN);
        StreakRewardSystem.Instance?.Initialize();
        GazeTracker.Instance?.StartTracking();

        if (currentGameMode == GameMode.Sequence)
            SequenceManager.Instance?.Initialize(sequenceLength);

        if (gameModeText) gameModeText.text = currentGameMode.ToString();
        UpdateUI();

        if (currentGameMode == GameMode.Sequence)
            StartCoroutine(StartAfterSequenceDisplay());
        else
            balloonSpawner?.StartSpawning();

        Debug.Log($"[GameManager] Session started — mode:{currentGameMode}  reps:{repCount}  duration:{sessionDuration}s");
    }

    private IEnumerator StartAfterSequenceDisplay()
    {
        if (warningText) warningText.text = "Remember the sequence!";
        yield return new WaitForSeconds(3f);
        if (warningText) warningText.text = "";
        balloonSpawner?.StartSpawning();
    }

    private void EndSession()
    {
        if (!sessionActive && repsCompleted == 0 && score == 0) return;
        sessionActive = false;
        balloonSpawner?.StopSpawning();
        GazeTracker.Instance?.StopTracking();
        SequenceManager.Instance?.Reset();

        PopEffectController.Instance?.PlaySessionComplete();

        float elapsed       = sessionDuration - sessionTimer;
        float avgReactionMs = AverageReactionTime();
        float accuracy      = TotalAttempts() > 0
            ? (float)correctPops / TotalAttempts() : 1f;
        float seqAccuracy   = SequenceManager.Instance != null
            ? SequenceManager.Instance.SequenceAccuracy : 1f;

        var usn     = USNManager.Instance;
        var gaze    = GazeTracker.Instance;
        var rewards = StreakRewardSystem.Instance;
        var diff    = DifficultyManager.Instance;

        if (rewards != null)
            rewards.EvaluateSessionBadges(accuracy, avgReactionMs, seqAccuracy,
                usn != null ? usn.LeftFraction : 0f,
                usn != null && usn.UsnModeActive);

        string badgeStr = rewards != null ? string.Join(",", rewards.EarnedBadges) : "";

        // Post results to backend if this session was started via Verse auth.
        if (PlaylistManager.Instance != null && PlaylistManager.Instance.HasSession
            && VerseClient.Instance != null)
        {
            var metrics = new BalloonGameMetrics
            {
                total_score          = score,
                accuracy             = accuracy,
                avg_reaction_time_ms = avgReactionMs,
                max_streak           = rewards != null ? rewards.MaxStreak : 0,
                correct_pops         = correctPops,
                incorrect_pops       = incorrectPops,
                missed_balloons      = missedBalloons,
                game_mode            = currentGameMode.ToString(),
                session_duration     = elapsed,
                cbs_score            = cbsScore,
                mpt_score            = mptScore,
                difficulty_level     = diff != null ? diff.CurrentLevel : 1,
                sequence_accuracy    = seqAccuracy,
                spatial_left         = usn != null ? usn.LeftFraction   : 0f,
                spatial_center       = usn != null ? usn.CenterFraction : 0f,
                spatial_right        = usn != null ? usn.RightFraction  : 0f,
            };
            VerseClient.Instance.SubmitResults(
                PlaylistManager.Instance.SessionId,
                PlaylistManager.Instance.SessionToken,
                metrics,
                onDone: () =>
                {
                    Debug.Log("[GameManager] Results submitted.");
                    PlaylistManager.Instance.Clear();
                    StartCoroutine(ReturnToBootstrap(3f));
                },
                onError: err =>
                {
                    Debug.LogWarning("[GameManager] Results submit failed: " + err);
                    PlaylistManager.Instance.Clear();
                    StartCoroutine(ReturnToBootstrap(3f));
                });
        }
        else
        {
            // Editor / V1 WS path — send over WebSocket as before.
            WSClient.Instance?.SendSessionResult(new SessionResultMessage
            {
                type              = "session_result",
                totalScore        = score,
                accuracy          = accuracy,
                avgReactionTimeMs = avgReactionMs,
                maxStreak         = rewards != null ? rewards.MaxStreak : 0,
                badges            = badgeStr,
                spatialLeft       = usn != null ? usn.LeftFraction   : 0f,
                spatialCenter     = usn != null ? usn.CenterFraction : 0f,
                spatialRight      = usn != null ? usn.RightFraction  : 0f,
                sequenceAccuracy  = seqAccuracy,
                gazeLeft          = gaze != null ? gaze.LeftGazeFraction   : 0f,
                gazeCenter        = gaze != null ? gaze.CenterGazeFraction : 0f,
                gazeRight         = gaze != null ? gaze.RightGazeFraction  : 0f,
                difficultyLevel   = diff != null ? diff.CurrentLevel : 1,
                correctPops       = correctPops,
                incorrectPops     = incorrectPops,
                missedBalloons    = missedBalloons,
                cbsScore          = cbsScore,
                mptScore          = mptScore,
                gameMode          = currentGameMode.ToString(),
                sessionDuration   = elapsed
            });
        }

        ClinicalReportScreen.Instance?.ShowReport(new SessionReport
        {
            totalScore        = score,
            accuracy          = accuracy,
            avgReactionTimeMs = avgReactionMs,
            maxStreak         = rewards != null ? rewards.MaxStreak : 0,
            badges            = rewards != null ? rewards.EarnedBadges.ToArray() : new string[0],
            spatialLeft       = usn != null ? usn.LeftFraction   : 0f,
            spatialCenter     = usn != null ? usn.CenterFraction : 0f,
            spatialRight      = usn != null ? usn.RightFraction  : 0f,
            sequenceAccuracy  = seqAccuracy,
            gazeLeft          = gaze != null ? gaze.LeftGazeFraction   : 0f,
            gazeCenter        = gaze != null ? gaze.CenterGazeFraction : 0f,
            gazeRight         = gaze != null ? gaze.RightGazeFraction  : 0f,
            usnMode           = usn != null && usn.UsnModeActive,
            sequenceMode      = currentGameMode == GameMode.Sequence,
            cbsScore          = cbsScore,
            mptScore          = mptScore,
            difficultyLevel   = diff != null ? diff.CurrentLevel : 1,
            correctPops       = correctPops,
            incorrectPops     = incorrectPops,
            missedBalloons    = missedBalloons,
            gameMode          = currentGameMode.ToString()
        });

        if (warningText) warningText.text = $"Done!  Score: {score}";
        Debug.Log($"[GameManager] Session ended — score:{score}  acc:{accuracy:P0}");
    }

    private IEnumerator ReturnToBootstrap(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("Bootstrap");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void FlashWarning(string msg, Color color, float duration)
    {
        if (_warningCoroutine != null) StopCoroutine(_warningCoroutine);
        _warningCoroutine = StartCoroutine(FlashWarningRoutine(msg, color, duration));
    }

    private IEnumerator FlashWarningRoutine(string msg, Color color, float duration)
    {
        if (warningText != null)
        {
            warningText.text  = msg;
            warningText.color = color;
        }
        yield return new WaitForSeconds(duration);
        if (warningText != null)
        {
            warningText.text  = "";
            warningText.color = Color.white;
        }
        _warningCoroutine = null;
    }

    private float AverageReactionTime()
    {
        if (reactionTimes.Count == 0) return 0f;
        float sum = 0f;
        foreach (float t in reactionTimes) sum += t;
        return sum / reactionTimes.Count;
    }

    private int TotalAttempts() => correctPops + incorrectPops + missedBalloons;

    private void UpdateUI()
    {
        if (scoreText) scoreText.text = "Score: " + score;
        if (repsText)  repsText.text  = $"Pops: {repsCompleted} / {repCount}";
        if (timerText) timerText.text = $"Time: {Mathf.CeilToInt(sessionTimer)}s";
    }

    // ── Editor test menu ───────────────────────────────────────────────────────
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Test/1 - Send Prescription (Standard)")]
    static void EditorPrescriptionStandard()
    {
        if (Instance == null) { UnityEngine.Debug.LogError("[Test] Enter Play Mode first."); return; }
        Instance.OnPrescriptionReceived(60f, 1000f, 10, 0.3f, 2f, 60f);
        UnityEngine.Debug.Log("[Test] Standard prescription sent.");
    }

    [UnityEditor.MenuItem("Test/2 - Start Session")]
    static void EditorStart()
    {
        if (Instance == null) return;
        Instance.OnCommand("start");
    }

    [UnityEditor.MenuItem("Test/3 - Stop Session")]
    static void EditorStop()
    {
        if (Instance == null) return;
        Instance.OnCommand("stop");
    }

    [UnityEditor.MenuItem("Test/4 - Simulate Target Pop")]
    static void EditorSimulatePop()
    {
        if (Instance == null) return;
        Instance.OnTargetPopped(BalloonType.Red, 1100f, 0.3f, Vector3.zero);
    }

    [UnityEditor.MenuItem("Test/5 - Reset")]
    static void EditorReset()
    {
        if (Instance == null) return;
        Instance.OnCommand("reset");
    }

    [UnityEditor.MenuItem("Test/6 - Sequence Mode")]
    static void EditorSequenceMode()
    {
        if (Instance == null) return;
        var msg = new PrescriptionMessage
        {
            targetRotation = 60f, holdTimeMs = 1000f, repCount = 10,
            balloonSize = 0.3f, spawnInterval = 3f, sessionDuration = 60f,
            gameMode = "sequence", sequenceLength = 3, distractorCount = 2
        };
        Instance.OnPrescriptionReceived(msg);
        Instance.OnCommand("start");
        UnityEngine.Debug.Log("[Test] Sequence mode started.");
    }

    [UnityEditor.MenuItem("Test/7 - USN Mode (CBS 18)")]
    static void EditorUSNMode()
    {
        if (Instance == null) return;
        var msg = new PrescriptionMessage
        {
            targetRotation = 60f, holdTimeMs = 1000f, repCount = 10,
            balloonSize = 0.3f, spawnInterval = 2f, sessionDuration = 60f,
            gameMode = "usn", cbsScore = 18, distractorCount = 2
        };
        Instance.OnPrescriptionReceived(msg);
        Instance.OnCommand("start");
        UnityEngine.Debug.Log("[Test] USN mode started (CBS=18).");
    }

    [UnityEditor.MenuItem("Test/8 - Adaptive Difficulty")]
    static void EditorAdaptive()
    {
        if (Instance == null) return;
        var msg = new PrescriptionMessage
        {
            targetRotation = 60f, holdTimeMs = 1000f, repCount = 15,
            balloonSize = 0.35f, spawnInterval = 3f, sessionDuration = 90f,
            adaptiveDifficulty = true, distractorCount = 1
        };
        Instance.OnPrescriptionReceived(msg);
        Instance.OnCommand("start");
        UnityEngine.Debug.Log("[Test] Adaptive difficulty started.");
    }
#endif
}
