using UnityEngine;

// Defines a hand pose using OVRHand finger strengths (0=extended, 1=curled).
// Fingers: [0]=Thumb [1]=Index [2]=Middle [3]=Ring [4]=Pinky
[CreateAssetMenu(fileName = "NewPose", menuName = "Trophy/Pose Data")]
public class TrophyPoseData : ScriptableObject
{
    public string poseName = "NewPose";

    [Header("Finger Targets (0=extended, 1=curled)")]
    [Range(0f, 1f)] public float thumbTarget  = 0f;
    [Range(0f, 1f)] public float indexTarget  = 0f;
    [Range(0f, 1f)] public float middleTarget = 0f;
    [Range(0f, 1f)] public float ringTarget   = 0f;
    [Range(0f, 1f)] public float pinkyTarget  = 0f;

    [Header("Per-Finger Tolerance")]
    [Range(0.05f, 0.5f)] public float thumbTol  = 0.25f;
    [Range(0.05f, 0.5f)] public float indexTol  = 0.25f;
    [Range(0.05f, 0.5f)] public float middleTol = 0.25f;
    [Range(0.05f, 0.5f)] public float ringTol   = 0.25f;
    [Range(0.05f, 0.5f)] public float pinkyTol  = 0.25f;

    [Header("Detection")]
    [Range(0.5f, 1.0f)] public float matchThreshold = 0.7f;

    [Header("Optional Pinch Override")]
    public bool requirePinch = false;

    [HideInInspector] public int sampleCount;

    public float[] Targets     => new[] { thumbTarget, indexTarget, middleTarget, ringTarget, pinkyTarget };
    public float[] Tolerances  => new[] { thumbTol, indexTol, middleTol, ringTol, pinkyTol };
}
