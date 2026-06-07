using System.Collections;
using UnityEngine;

public class BalloonSpawner : MonoBehaviour
{
    public GameObject balloonPrefab;

    private Coroutine spawnCoroutine;

    private static readonly BalloonType[] AllTypes =
        { BalloonType.Red, BalloonType.Green, BalloonType.Blue, BalloonType.Yellow };

    public void StartSpawning()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine == null) return;
        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float interval = GameManager.Instance.spawnInterval
                * (DifficultyManager.Instance?.SpawnIntervalMultiplier ?? 1f);
            yield return new WaitForSeconds(interval);
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        if (balloonPrefab == null) return;
        var gm = GameManager.Instance;

        // Determine which colour is the target this wave
        BalloonType targetType = gm.currentGameMode == GameMode.Sequence
            ? SequenceManager.Instance?.GetCurrentTarget() ?? BalloonType.Red
            : BalloonType.Red;

        SpawnOne(targetType, isTarget: true);

        int distractorCount = gm.distractorCount
            + (DifficultyManager.Instance?.DistractorCountBonus ?? 0);

        for (int i = 0; i < distractorCount; i++)
            SpawnOne(RandomDistractor(targetType), isTarget: false);
    }

    private void SpawnOne(BalloonType type, bool isTarget)
    {
        var gm = GameManager.Instance;

        // Size — clamped to safe range
        float sizeMult = DifficultyManager.Instance?.BalloonSizeMultiplier ?? 1f;
        float size = Random.Range(gm.balloonSizeMin * sizeMult, gm.balloonSizeMax * sizeMult);
        size = Mathf.Clamp(size, 0.15f, 0.65f);

        // Position
        float xMin, xMax;
        if (USNManager.Instance != null && USNManager.Instance.UsnModeActive)
            USNManager.Instance.GetSpawnXRange(out xMin, out xMax);
        else
        {
            xMin = -1.8f;
            xMax =  1.8f;
        }

        float x = Random.Range(xMin, xMax);
        // EyeLevel tracking: camera is at y≈0, arms reach roughly y=-0.3 to y+0.5
        float y = Random.Range(-0.2f, 0.5f);
        float z = Random.Range(2.0f, 4.0f);

        var go = Instantiate(balloonPrefab, new Vector3(x, y, z), Quaternion.identity);
        go.transform.localScale = Vector3.one * size;

        var balloon = go.GetComponent<Balloon>();
        if (balloon != null)
        {
            balloon.balloonType       = type;
            balloon.isTarget          = isTarget;
            balloon.moveSpeed         = 0.3f * (DifficultyManager.Instance?.BalloonSpeedMultiplier ?? 1f);
            balloon.reactionTimeLimit = gm.reactionTimeLimit;
        }
    }

    private BalloonType RandomDistractor(BalloonType excludeType)
    {
        BalloonType t;
        do { t = AllTypes[Random.Range(0, AllTypes.Length)]; }
        while (t == excludeType);
        return t;
    }
}
