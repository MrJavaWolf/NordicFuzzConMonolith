using Monolith.DonationPolling.PollDonations;
using NFC.Donation.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class TotalAmountController : MonoBehaviour
{
    public DataStorage dataStorage;
    public CoinSpawner coinSpawner;
    public int MaxNumberOfCoins = 600;
    public float CheckForMoneyIntervalSeconds = 10f;

    public float FireworksWaitTime = 3.5f;
    public List<ParticleSystem> Fireworks = new();

    public int DebugMinimumMoney = 50;
    public int DebugMaximumMoney = 5000;

    private float spawnTimer;
    private bool IsCheckingForNewData = false;
    private DonationStorageDto<MonetaryStatusResponse> currentMoneyStatus;
    private DonationStorageDto<MonetaryStatusResponse> newMoneyStatus;
    

    private float FireworksStartTime = -1;
    private enum State
    {
        WaitingForMoreMoney,
        Spawning,
        WaitingForFireworks,
        WaitingForCoinsToFall
    }

    private State currentState = State.WaitingForMoreMoney;

    void Update()
    {
        switch (currentState)
        {
            case State.WaitingForMoreMoney:
                HandleWaitingForMoreMoney();
                break;

            case State.Spawning:
                HandleSpawning();
                break;
            case State.WaitingForFireworks:
                HandleWaitingForFireworks();
                break;

            case State.WaitingForCoinsToFall:
                HandleWaitingForCoinsToFall();
                break;
        }
    }

    private void HandleWaitingForMoreMoney()
    {

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            MonetaryStatusResponse current = currentMoneyStatus?.Data;
            if (current == null)
            {
                current = new MonetaryStatusResponse()
                {
                    Stripe = new MonetaryStripeStatusResponse()
                    {
                        Amount = 0,
                        Currency = "sek"
                    },
                    Zettle = new MonetaryZettleStatusResponse()
                    {
                        Amount = 0,
                        Currency = "sek"
                    },
                    Manual = new MonetaryManualStatusResponse()
                    {
                        Amount = 0,
                        Currency = "sek"
                    }
                };
            }
            current = Newtonsoft.Json.JsonConvert.DeserializeObject<MonetaryStatusResponse>(Newtonsoft.Json.JsonConvert.SerializeObject(current));

            int additionAmount = UnityEngine.Random.Range(DebugMinimumMoney, DebugMaximumMoney);
            Debug.Log($"AdditionAmount: {additionAmount}");
            current.Stripe.Amount = current.Stripe.Amount + additionAmount;

            newMoneyStatus = new DonationStorageDto<MonetaryStatusResponse>()
            {
                DataTimestamp = DateTimeOffset.Now,
                IsUpToDate = true,
                LastUpdateAttemptTimestamp = DateTimeOffset.Now,
                Data = current
            };
        }


        if (newMoneyStatus != null)
        {
            long currentMoney = GetTotalAmount(currentMoneyStatus);
            long newMoney = GetTotalAmount(newMoneyStatus);
            long newMoneyRecieved = newMoney - currentMoney;
            var (ones, fifties, twoHundreds) = coinSpawner.CalculateCoins((int)newMoneyRecieved);

            // Check if we've reached the max
            if (coinSpawner.AllCoins.Count > 0 &&
                coinSpawner.AllCoins.Count + ones + fifties + twoHundreds >= MaxNumberOfCoins)
            {
                Debug.Log("Plays fireworks");
                foreach (var firework in Fireworks)
                {
                    firework.Play();
                }
                currentState = State.WaitingForFireworks;
                FireworksStartTime = Time.time;
                return;
            }

            currentMoneyStatus = newMoneyStatus;
            if (newMoneyRecieved > 0)
            {
                Debug.Log($"Ones: {ones}, Fifties: {fifties}, TwoHundreds: {twoHundreds}");
                coinSpawner.SpawnCoins((ones, fifties, twoHundreds));
                currentState = State.Spawning;
            }
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer < CheckForMoneyIntervalSeconds)
            return;

        spawnTimer = 0f;
        CheckForNewTotalAmountData();
    }

    private void HandleSpawning()
    {
        // Wait until spawner finishes
        if (coinSpawner.IsSpawningCoins == false)
        {
            currentState = State.WaitingForMoreMoney;
        }
    }


    private void HandleWaitingForFireworks()
    {
        // Wait until coins finish falling
        if (Time.time - FireworksStartTime >= FireworksWaitTime)
        {
            coinSpawner.LetCoinsFall();
            currentState = State.WaitingForCoinsToFall;
        }
    }


    private void HandleWaitingForCoinsToFall()
    {
        // Wait until coins finish falling
        if (coinSpawner.IsCoinsFalling == false)
        {
            currentState = State.WaitingForMoreMoney;
        }
    }


    private void CheckForNewTotalAmountData()
    {
        if (IsCheckingForNewData) return;
        IsCheckingForNewData = true;
        Task.Run(() =>
        {
            try
            {
                DonationDataPaths donationDataPaths = dataStorage.DonationDataPaths;
                if (!donationDataPaths.Enable) return;
                string dataFile = donationDataPaths.GetCurrentDataFile(donationDataPaths.MonetaryStatusPath);
                DonationStorageDto<MonetaryStatusResponse> readStatus = DonationDataStorage.Load<DonationStorageDto<MonetaryStatusResponse>>(dataFile);
                if (readStatus == null)
                {
                    return;
                }
                long totalAmount = GetTotalAmount(readStatus);
                Debug.Log($"{DateTime.Now} - Total amount: {totalAmount}");
                if (totalAmount > 0)
                {
                    if (currentMoneyStatus == null)
                    {
                        newMoneyStatus = readStatus;
                    }
                    else
                    {
                        if (currentMoneyStatus.DataTimestamp != readStatus.DataTimestamp && readStatus.Data != null)
                        {
                            newMoneyStatus = readStatus;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to CheckForNewTotalAmountData(): {ex}");
            }
            finally
            {
                IsCheckingForNewData = false;
            }
        });
    }

    private static long GetTotalAmount(DonationStorageDto<MonetaryStatusResponse> readStatus)
    {
        return (readStatus?.Data?.Manual?.Amount ?? 0) +
            (readStatus?.Data?.Zettle?.Amount ?? 0) +
            (readStatus?.Data?.Stripe?.Amount ?? 0);
    }
}
