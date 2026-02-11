using UnityEngine;

public class TotalAmountController : MonoBehaviour
{
    public CoinSpawner coinSpawner;
    public int MaxNumberOfCoins = 600;
    public float SpawnIntervalSeconds = 2f;

    private float spawnTimer;

    private enum State
    {
        WaitingToSpawn,
        Spawning,
        WaitingForCoinsToFall
    }

    private State currentState = State.WaitingToSpawn;

    void Update()
    {
        switch (currentState)
        {
            case State.WaitingToSpawn:
                HandleWaitingToSpawn();
                break;

            case State.Spawning:
                HandleSpawning();
                break;

            case State.WaitingForCoinsToFall:
                HandleWaitingForCoinsToFall();
                break;
        }
    }

    private void HandleWaitingToSpawn()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer < SpawnIntervalSeconds)
            return;

        spawnTimer = 0f;

        // Check if we've reached the max
        if (coinSpawner.AllCoins.Count >= MaxNumberOfCoins)
        {
            coinSpawner.LetCoinsFall();
            currentState = State.WaitingForCoinsToFall;
            return;
        }

        // Spawn random amount between 1 and 100
        int randomAmount = Random.Range(50, 100);
        coinSpawner.SpawnCoins(randomAmount);
        currentState = State.Spawning;
    }

    private void HandleSpawning()
    {
        // Wait until spawner finishes
        if (coinSpawner.IsSpawningCoins == false)
        {
            currentState = State.WaitingToSpawn;
        }
    }

    private void HandleWaitingForCoinsToFall()
    {
        // Wait until coins finish falling
        if (coinSpawner.IsCoinsFalling == false)
        {
            currentState = State.WaitingToSpawn;
        }
    }
}
