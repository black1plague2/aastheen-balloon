using System;

[Serializable]
public class SensorMessage
{
    public string type;
    public float  rotation;
    public float  speed;
    public string warning;
}

[Serializable]
public class PrescriptionMessage
{
    public string type;
    // Core
    public float  targetRotation;
    public float  holdTimeMs;
    public int    repCount;
    public float  balloonSize;
    public float  spawnInterval;
    public float  sessionDuration;
    // Extended
    public string gameMode            = "standard"; // standard | sequence | reaction | usn
    public int    distractorCount     = 1;
    public int    sequenceLength      = 3;
    public float  reactionTimeLimit   = -1f;        // -1 = no limit
    public bool   adaptiveDifficulty  = false;
    public bool   usnMode             = false;
    public int    cbsScore            = -1;         // Catherine Bergego Scale 0-30 (-1 = not provided)
    public int    mptScore            = -1;         // Motor Perf Test 0-100 (-1 = not provided)
    public float  balloonSizeMin      = 0f;         // 0 = derive from balloonSize
    public float  balloonSizeMax      = 0f;
    public int    streakBonusThreshold = 3;
}

[Serializable]
public class RepDoneMessage
{
    public string type;
    public int    score;
    public float  rotationAchieved;
    public float  heldMs;
    public string balloonType;
    public float  reactionTimeMs;
    public bool   wasCorrect;
    public int    currentStreak;
}

[Serializable]
public class SessionResultMessage
{
    public string type;
    public int    totalScore;
    public float  accuracy;
    public float  avgReactionTimeMs;
    public int    maxStreak;
    public string badges;           // comma-separated
    public float  spatialLeft;
    public float  spatialCenter;
    public float  spatialRight;
    public float  sequenceAccuracy;
    public float  gazeLeft;
    public float  gazeCenter;
    public float  gazeRight;
    public int    difficultyLevel;
    public int    correctPops;
    public int    incorrectPops;
    public int    missedBalloons;
    public int    cbsScore;
    public int    mptScore;
    public string gameMode;
    public float  sessionDuration;
}

[Serializable]
public class CommandMessage
{
    public string type;
    public string command;
}
