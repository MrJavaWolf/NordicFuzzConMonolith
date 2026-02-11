using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public Transform bottomCollider;
    public Vector2 areaSize = new Vector2(5f, 3f);
    public float coinsPerSecond = 10f; // seconds

    public bool IsCoinsFalling { get; private set; } = false;

    public bool IsSpawningCoins { get; private set; } = false;

    public List<GameObject> AllCoins { get; } = new List<GameObject>();


    public void SpawnCoins(int amount, float coinsPerSecond = -1)
    {
        IsSpawningCoins = true;
        StartCoroutine(SpawnCoinsOverTime(amount, coinsPerSecond));

    }
    public IEnumerator SpawnCoinsOverTime(int amount, float coinsPerSecond = -1)
    {
        IsSpawningCoins = true;
        if (coinsPerSecond <= 0)
        {
            coinsPerSecond = this.coinsPerSecond;
        }

        if (amount <= 0 || coinsPerSecond <= 0f)
            yield break;

        float delay = 1f / coinsPerSecond;

        for (int i = 0; i < amount; i++)
        {
            Vector2 randomPos = GetRandomPosition();
            GameObject coinObject = Instantiate(
                coinPrefab,
                randomPos,
                Quaternion.identity,
                transform.parent
            );

            AllCoins.Add(coinObject);
            yield return new WaitForSeconds(delay);
        }
        IsSpawningCoins = false;
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

    public void LetCoinsFall()
    {
        IsCoinsFalling = true;
        bottomCollider.gameObject.SetActive(false);
        foreach (var coin in AllCoins)
        {
            var rigdigBody = coin.GetComponent<TotalAmountCoin>();
            rigdigBody.StartFinalFall();
        }

        StartCoroutine(LetCoinsFallCoroutine());
    }

    public IEnumerator LetCoinsFallCoroutine()
    {
        yield return new WaitForSeconds(3);
        foreach (var coin in AllCoins)
        {
            Destroy(coin);
        }
        AllCoins.Clear();
        bottomCollider.gameObject.SetActive(true);
        IsCoinsFalling = false;
    }
}
