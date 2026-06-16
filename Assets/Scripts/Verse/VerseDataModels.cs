using System;
using UnityEngine;

// ── Inbound: POST /prescriptions/verify ───────────────────────────────────────

[Serializable]
public class VerifyCodeRequest
{
    public string code;
}

// Balloon-specific prescription settings stored in the backend's `targets` dict.
// Doctor sets these fields when creating the prescription in the Flutter app.
[Serializable]
public class BalloonSettings
{
    public float  targetRotation     = 60f;
    public float  holdTimeMs         = 1000f;
    public int    repCount           = 10;
    public float  balloonSize        = 0.3f;
    public float  balloonSizeMin     = 0f;
    public float  balloonSizeMax     = 0f;
    public float  spawnInterval      = 3f;
    public float  sessionDuration    = 120f;
    public string gameMode           = "standard"; // standard | sequence | reaction | usn
    public int    distractorCount    = 1;
    public int    sequenceLength     = 3;
    public float  reactionTimeLimit  = -1f;
    public bool   adaptiveDifficulty = false;
    public bool   usnMode            = false;
    public int    cbsScore           = -1;
    public int    mptScore           = -1;
}

[Serializable]
public class PrescriptionPublic
{
    public int           id;
    public string        game_id;
    public string        game_name;
    public string        difficulty;
    public BalloonSettings targets;       // typed so JsonUtility can deserialise directly
    public bool          requires_nurse_approval;
    public string        patient_name;
}

[Serializable]
public class VerifyCodeResponse
{
    public int              session_id;
    public string           session_token;
    public string           status;        // "active" | "pending_approval"
    public PrescriptionPublic prescription;
}

// ── Outbound: POST /sessions/{id}/results ─────────────────────────────────────

[Serializable]
public class BalloonGameMetrics
{
    public int    total_score;
    public float  accuracy;
    public float  avg_reaction_time_ms;
    public int    max_streak;
    public int    correct_pops;
    public int    incorrect_pops;
    public int    missed_balloons;
    public string game_mode;
    public float  session_duration;
    public int    cbs_score;
    public int    mpt_score;
    public int    difficulty_level;
    public float  sequence_accuracy;
    public float  spatial_left;
    public float  spatial_center;
    public float  spatial_right;
}

// emg_samples / imu_samples are omitted intentionally — backend accepts null fine.
[Serializable]
public class SessionResultsPayload
{
    public BalloonGameMetrics game_metrics;
    public string             network_mode = "home";
}
