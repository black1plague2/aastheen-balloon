using System.Collections;
using UnityEngine;

public class BalloonSpawner : MonoBehaviour
{
    public GameObject balloonPrefab;

    private Coroutine spawnCoroutine;

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
            yield return new WaitForSeconds(GameManager.Instance.spawnInterval);
            SpawnBalloon();
        }
    }

    private void SpawnBalloon()
    {
        if (balloonPrefab == null) return;
        float x = Random.Range(-1.5f, 1.5f);
        float y = Random.Range( 1.2f, 2.0f);
        float z = Random.Range( 2.5f, 4.0f);
        Instantiate(balloonPrefab, new Vector3(x, y, z), Quaternion.identity);
    }
}
