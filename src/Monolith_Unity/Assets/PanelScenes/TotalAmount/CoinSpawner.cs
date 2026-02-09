using System.Collections;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public Vector2 areaSize = new Vector2(5f, 3f);
    public int spawnCount = 100;
    public float spawnDuration = 5f; // seconds

    void Start()
    {
        StartCoroutine(SpawnCoinsOverTime(spawnCount, spawnDuration));
    }


    IEnumerator SpawnCoinsOverTime(int amount, float duration)
    {
        if (amount <= 0)
            yield break;

        float delay = duration / amount;

        for (int i = 0; i < amount; i++)
        {
            Vector2 randomPos = GetRandomPosition();
            Instantiate(coinPrefab, randomPos, Quaternion.identity, transform.parent);

            yield return new WaitForSeconds(delay);
        }
    }

    Vector2 GetRandomPosition()
    {
        Vector2 center = transform.position;

        float x = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float y = Random.Range(-areaSize.y / 2f, areaSize.y / 2f);

        return center + new Vector2(x, y);
    }

    // Visualize area in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }


}
