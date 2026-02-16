using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CombiningMoneyState
{
    None,
    StartCombining,
    CombiningMoney,
}

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;


    public Vector2 areaSize = new(5f, 3f);
    public float coinsPerSecond = 10f;
    public int MaximumNumberOfActiveCoins = 200;

    public int MaximumNumberOfCoins = 2000;
    public int TargetCount = 1000;
    public int NeighborsToCombine = 50;
    public int MaximumNumberOfCoinsType2 = 100;
    public int MaximumNumberOfCoinsType3 = 100;

    public float horizontalOffset = 20f;
    public float verticalOffset = 2000f;
    public float bottomOffset = 10f;
    public GameObject CombineCoinEffect;
    public CompositeCollider2D CompositCollider;

    private CombiningMoneyState combiningMoneyState = CombiningMoneyState.None;

    public bool IsCoinsFalling { get; private set; } = false;

    public bool IsSpawningCoins { get; private set; } = false;

    public int NumberOfCoins = 0;
    public int NumberOfActiveCoins = 0;


    private List<TotalAmountCoin> AllCoins { get; } = new List<TotalAmountCoin>();
    private HashSet<TotalAmountCoin> ActiveCoins = new HashSet<TotalAmountCoin>();
    private List<BoxCollider2D> boxCollider2Ds = new List<BoxCollider2D>();



    public void Update()
    {
        NumberOfCoins = AllCoins.Count;
        NumberOfActiveCoins = ActiveCoins.Count;
    }

    public void SpawnCoins(int amount, float coinsPerSecond = -1)
    {
        IsSpawningCoins = true;
        StartCoroutine(SpawnCoinsOverTime(amount, coinsPerSecond));
    }


    public IEnumerator SpawnCoinsOverTime(int amount, float coinsPerSecond = -1)
    {
        Debug.Log($"Starts spawning {amount} coins.");
        IsSpawningCoins = true;

        if (coinsPerSecond <= 0)
            coinsPerSecond = this.coinsPerSecond;

        float spawnInterval = 1f / coinsPerSecond;
        float timer = 0f;
        int spawned = 0;

        while (spawned < amount)
        {
            while (ActiveCoins.Count > MaximumNumberOfActiveCoins)
            {
                yield return null;
            }

            while (combiningMoneyState != CombiningMoneyState.None)
            {
                yield return null;
            }

            timer += Time.deltaTime;

            while (timer >= spawnInterval && spawned < amount)
            {
                timer -= spawnInterval;
                spawned++;

                Vector2 randomPos = GetRandomPosition();
                GameObject coinObject = Instantiate(
                    coinPrefab,
                    randomPos,
                    Quaternion.identity,
                    transform.parent
                );
                var totalAmountCoin = coinObject.GetComponent<TotalAmountCoin>();
                totalAmountCoin.Spawner = this;
                ActiveCoins.Add(totalAmountCoin);
                AllCoins.Add(totalAmountCoin);
            }

            if (AllCoins.Count > MaximumNumberOfCoins)
            {
                yield return StartCoroutine(CombineMoneyOverTime());
            }

            yield return null;
        }

        IsSpawningCoins = false;
    }

    public void CoinIsNoLongerActive(TotalAmountCoin totalAmountCoin)
    {
        ActiveCoins.Remove(totalAmountCoin);

        totalAmountCoin.transform.parent = CompositCollider.transform;
        BoxCollider2D[] boxColliders = totalAmountCoin.GetComponentsInChildren<BoxCollider2D>();
        for (int i = 0; i < boxColliders.Length; i++)
        {
            boxCollider2Ds.Add(boxColliders[i]);
        }

        if (ActiveCoins.Count == 0)
        {
            for (int i = 0; i < boxCollider2Ds.Count; i++)
            {
                boxCollider2Ds[i].compositeOperation = Collider2D.CompositeOperation.Merge;
            }
            CompositCollider.GenerateGeometry();
            boxCollider2Ds.Clear();
        }
    }

    public Vector2 GetRandomPosition()
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

    public IEnumerator CombineMoneyOverTime()
    {
        while (AllCoins.Count > TargetCount)
        {
            // Wait until no active coins
            while (ActiveCoins.Count > 0)
                yield return null;

            if (AllCoins.Count <= NeighborsToCombine)
                yield break;


            // Choose a coin to merge

            // Group coins by type, ignoring Coin4
            var coinTypes = AllCoins
                .Where(x => x.CoinType != CoinType.Coin4)
                .GroupBy(x => x.CoinType)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Initialize the list we will select
            List<TotalAmountCoin> coinTypeList = null;

            // Priority: Coin3 > Coin2 > Coin1
            if (coinTypes.ContainsKey(CoinType.Coin3) && coinTypes[CoinType.Coin3].Count > MaximumNumberOfCoinsType3)
            {
                coinTypeList = coinTypes[CoinType.Coin3];
            }
            else if (coinTypes.ContainsKey(CoinType.Coin2) && coinTypes[CoinType.Coin2].Count > MaximumNumberOfCoinsType2)
            {
                coinTypeList = coinTypes[CoinType.Coin2];
            }
            else if (coinTypes.ContainsKey(CoinType.Coin1))
            {
                coinTypeList = coinTypes[CoinType.Coin1];
            }
            else
            {
                coinTypeList = coinTypes
                    .OrderByDescending(g => g.Value.Count)
                    .First().Value;
            }

            int coinToMergeId = UnityEngine.Random.Range(0, coinTypeList.Count());
            TotalAmountCoin mergeCoin = coinTypeList[coinToMergeId];

            Vector3 basePos = mergeCoin.transform.position;

            var neighbors = AllCoins
                .Where(c => c != mergeCoin && c.CoinType == mergeCoin.CoinType)
                .OrderBy(c => (c.transform.position - basePos).sqrMagnitude)
                .Take(NeighborsToCombine)
                .ToList();

            // Determine leftmost and rightmost BEFORE destroying
            var mergedCoins = neighbors.Append(mergeCoin).ToList();
            TotalAmountCoin bottomLeft = mergedCoins
               .OrderBy(c => c.transform.position.x)
               .ThenBy(c => c.transform.position.y)
               .First();

            TotalAmountCoin bottomRight = mergedCoins
                .OrderByDescending(c => c.transform.position.x)
                .ThenBy(c => c.transform.position.y)
                .First();

            Vector3 bl = bottomLeft.transform.position;
            Vector3 br = bottomRight.transform.position;

            Vector3 center = Vector3.zero;

            foreach (var coin in mergedCoins)
            {
                center += coin.transform.position;
            }

            center /= mergedCoins.Count;

            // Remove neighbors
            foreach (var coin in neighbors)
            {
                AllCoins.Remove(coin);
                Destroy(coin.gameObject);
            }

            // Upgrade Coin
            mergeCoin.UpgradeCoinType();
            Instantiate(
                    CombineCoinEffect,
                    mergeCoin.transform.position,
                    Quaternion.identity,
                    transform.parent);
            yield return new WaitForSeconds(0.5f);

        }

        foreach (var coin in AllCoins)
        {
            coin.transform.parent = transform.parent;
        }

        // Recalculate colliders
        yield return null;
        CompositCollider.GenerateGeometry();

        // --------------------------------------------------
        // Activate coins
        // --------------------------------------------------
        List<TotalAmountCoin> coinsToActivate = AllCoins.OrderBy(x => x.transform.position.y).ToList();
        int index = 0;
        while (index < coinsToActivate.Count)
        {
            yield return null;
            yield return null;
            // Determine batch size
            int batchSize = Mathf.Min(MaximumNumberOfActiveCoins, coinsToActivate.Count - index);
            // Activate batch
            for (int i = 0; i < batchSize; i++)
            {
                TotalAmountCoin coin = coinsToActivate[index + i];
                ActiveCoins.Add(coin);
                coin.ReEnable();
            }

            index += batchSize;

            // Wait until all active coins finish
            while (ActiveCoins.Count > 0)
                yield return null;
        }
    }
}

