using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SequenceManager : MonoBehaviour
{
    public static SequenceManager Instance;

    [Header("UI")]
    public TMP_Text sequenceDisplayText;  // "Pop: RED"
    public TMP_Text sequenceProgressText; // "Step 1 / 3"

    private List<BalloonType> currentSequence = new List<BalloonType>();
    private int  currentStep = 0;
    private int  sequenceLength = 3;
    private bool isActive = false;

    // Stats
    public int   TotalSequencesAttempted  { get; private set; }
    public int   TotalSequencesCompleted  { get; private set; }
    public float SequenceAccuracy =>
        TotalSequencesAttempted > 0 ? (float)TotalSequencesCompleted / TotalSequencesAttempted : 1f;

    private static readonly BalloonType[] SequenceTypes =
        { BalloonType.Red, BalloonType.Green, BalloonType.Blue };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(int length)
    {
        sequenceLength           = Mathf.Clamp(length, 2, 6);
        TotalSequencesAttempted  = 0;
        TotalSequencesCompleted  = 0;
        isActive                 = true;
        GenerateNewSequence();
    }

    public BalloonType GetCurrentTarget()
    {
        if (!isActive || currentSequence.Count == 0) return BalloonType.Red;
        return currentSequence[currentStep];
    }

    // Returns true = correct step, false = wrong (sequence reset)
    public bool OnBalloonPopped(BalloonType type)
    {
        if (!isActive || currentSequence.Count == 0) return true;

        if (type == currentSequence[currentStep])
        {
            currentStep++;
            if (currentStep >= currentSequence.Count)
            {
                TotalSequencesCompleted++;
                GenerateNewSequence();
            }
            else
            {
                UpdateUI();
            }
            return true;
        }
        else
        {
            // Wrong colour — reset to beginning of same sequence
            currentStep = 0;
            TotalSequencesAttempted++; // count as a failed attempt
            UpdateUI();
            return false;
        }
    }

    public void Reset()
    {
        isActive = false;
        currentSequence.Clear();
        currentStep = 0;
        if (sequenceDisplayText)  sequenceDisplayText.text  = "";
        if (sequenceProgressText) sequenceProgressText.text = "";
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void GenerateNewSequence()
    {
        currentSequence.Clear();
        currentStep = 0;
        for (int i = 0; i < sequenceLength; i++)
            currentSequence.Add(SequenceTypes[Random.Range(0, SequenceTypes.Length)]);
        TotalSequencesAttempted++;
        UpdateUI();
        Debug.Log("[SequenceManager] New sequence: " + string.Join(" → ", currentSequence));
    }

    private void UpdateUI()
    {
        if (currentSequence.Count == 0) return;
        BalloonType next  = currentSequence[currentStep];
        string colorHex   = ColorUtility.ToHtmlStringRGB(BalloonData.GetColor(next));
        if (sequenceDisplayText)
            sequenceDisplayText.text  = $"Pop: <color=#{colorHex}><b>{next.ToString().ToUpper()}</b></color>";
        if (sequenceProgressText)
            sequenceProgressText.text = $"Step {currentStep + 1} / {currentSequence.Count}";
    }
}
