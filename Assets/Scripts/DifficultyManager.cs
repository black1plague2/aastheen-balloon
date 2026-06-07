using System.Collections.Generic;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    public bool AdaptiveEnabled { get; private set; }

    // Multipliers read by BalloonSpawner each wave
    public float SpawnIntervalMultiplier { get; private set; } = 1f;
    public float BalloonSizeMultiplier   { get; private set; } = 1f;
    public float BalloonSpeedMultiplier  { get; private set; } = 1f;
    public int   DistractorCountBonus    { get; private set; } = 0;
    public int   CurrentLevel            { get; private set; } = 1;

    private Queue<bool> recentResults = new Queue<bool>();
    private const int WindowSize = 5;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(bool adaptive)
    {
        AdaptiveEnabled          = adaptive;
        SpawnIntervalMultiplier  = 1f;
        BalloonSizeMultiplier    = 1f;
        BalloonSpeedMultiplier   = 1f;
        DistractorCountBonus     = 0;
        CurrentLevel             = 1;
        recentResults.Clear();
    }

    public void RecordResult(bool correct)
    {
        if (!AdaptiveEnabled) return;
        recentResults.Enqueue(correct);
        if (recentResults.Count > WindowSize) recentResults.Dequeue();
        if (recentResults.Count >= WindowSize) Adjust();
    }

    private void Adjust()
    {
        int correct = 0;
        foreach (bool r in recentResults) if (r) correct++;
        float accuracy = (float)correct / recentResults.Count;

        if (accuracy > 0.80f) // Too easy — increase challenge
        {
            CurrentLevel            = Mathf.Min(CurrentLevel + 1, 5);
            SpawnIntervalMultiplier = Mathf.Max(0.50f, SpawnIntervalMultiplier - 0.10f);
            BalloonSizeMultiplier   = Mathf.Max(0.60f, BalloonSizeMultiplier   - 0.10f);
            BalloonSpeedMultiplier  = Mathf.Min(2.00f, BalloonSpeedMultiplier  + 0.10f);
            DistractorCountBonus    = Mathf.Min(3,     DistractorCountBonus    + 1);
        }
        else if (accuracy < 0.50f) // Too hard — ease off
        {
            CurrentLevel            = Mathf.Max(CurrentLevel - 1, 1);
            SpawnIntervalMultiplier = Mathf.Min(1.50f, SpawnIntervalMultiplier + 0.10f);
            BalloonSizeMultiplier   = Mathf.Min(1.40f, BalloonSizeMultiplier   + 0.10f);
            BalloonSpeedMultiplier  = Mathf.Max(0.50f, BalloonSpeedMultiplier  - 0.10f);
            DistractorCountBonus    = Mathf.Max(0,     DistractorCountBonus    - 1);
        }

        Debug.Log($"[Difficulty] Level {CurrentLevel} — acc:{accuracy:P0}  spawnMult:{SpawnIntervalMultiplier:F2}  sizeMult:{BalloonSizeMultiplier:F2}  speedMult:{BalloonSpeedMultiplier:F2}");
    }
}
