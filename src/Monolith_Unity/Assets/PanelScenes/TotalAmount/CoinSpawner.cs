using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public GameObject coin50Prefab;
    public GameObject coin200Prefab;

    public Transform bottomCollider;
    public Vector2 areaSize = new(5f, 3f);
    public float coinsPerSecond = 10f;

    public bool IsCoinsFalling { get; private set; } = false;

    public bool IsSpawningCoins { get; private set; } = false;

    public List<GameObject> AllCoins { get; } = new List<GameObject>();

    public (int ones, int fifties, int twoHundreds) CalculateCoins(int totalAmount)
    {
        if (totalAmount <= 0)
            return (0, 0, 0);

        int targetCoinCount = Random.Range(20, 30);

        int twoHundreds = 0;
        int fifties = 0;
        int ones = 0;

        int remaining = totalAmount;

        // First try to distribute evenly across target coin count
        int averageValuePerCoin = Mathf.Max(1, totalAmount / targetCoinCount);

        if (averageValuePerCoin >= 200)
        {
            twoHundreds = remaining / 200;
            remaining -= twoHundreds * 200;
        }

        if (averageValuePerCoin >= 50)
        {
            fifties = remaining / 50;
            remaining -= fifties * 50;
        }

        ones = remaining;

        int totalCoins = twoHundreds + fifties + ones;

        // If we exceed 50 coins, compress smaller coins upward
        while (totalCoins > 50)
        {
            if (ones >= 50)
            {
                ones -= 50;
                fifties += 1;
            }
            else if (fifties >= 4)
            {
                fifties -= 4;
                twoHundreds += 1;
            }
            else
            {
                break;
            }

            totalCoins = twoHundreds + fifties + ones;
        }

        return (ones, fifties, twoHundreds);
    }


    public void SpawnCoins((int ones, int fifties, int twoHundreds) amount, float coinsPerSecond = -1)
    {
        IsSpawningCoins = true;
        StartCoroutine(SpawnCoinsOverTime(amount, coinsPerSecond));
    }


    public IEnumerator SpawnCoinsOverTime((int ones, int fifties, int twoHundreds) amount, float coinsPerSecond = -1)
    {
        IsSpawningCoins = true;
        if (coinsPerSecond <= 0)
        {
            coinsPerSecond = this.coinsPerSecond;
        }

        float delay = 1f / coinsPerSecond;

        for (int i = 0; i < amount.twoHundreds; i++)
        {
            Vector2 randomPos = GetRandomPosition();
            GameObject coinObject = Instantiate(
                coin200Prefab,
                randomPos,
                Quaternion.identity,
                transform.parent
            );

            AllCoins.Add(coinObject);
            yield return new WaitForSeconds(delay);
        }

        for (int i = 0; i < amount.fifties; i++)
        {
            Vector2 randomPos = GetRandomPosition();
            GameObject coinObject = Instantiate(
                coin50Prefab,
                randomPos,
                Quaternion.identity,
                transform.parent
            );

            AllCoins.Add(coinObject);
            yield return new WaitForSeconds(delay);
        }

        for (int i = 0; i < amount.ones; i++)
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

    public void MakeCoinsJump()
    {
        foreach (var coin in AllCoins)
        {
            var totalAmountCoin = coin.GetComponent<TotalAmountCoin>();
            totalAmountCoin.MakeJump();
            totalAmountCoin.MakeJump();
        }
    }

    public void LetCoinsFall()
    {
        IsCoinsFalling = true;
        bottomCollider.gameObject.SetActive(false);
        foreach (var coin in AllCoins)
        {
            var totalAmountCoin = coin.GetComponent<TotalAmountCoin>();
            totalAmountCoin.StartFinalFall();
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
