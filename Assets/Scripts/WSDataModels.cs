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
    public float  targetRotation;
    public float  holdTimeMs;
    public int    repCount;
    public float  balloonSize;
    public float  spawnInterval;
    public float  sessionDuration;
}

[Serializable]
public class RepDoneMessage
{
    public string type;
    public int    score;
    public float  rotationAchieved;
    public float  heldMs;
}

[Serializable]
public class CommandMessage
{
    public string type;
    public string command;
}
