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
    public List<ParticleSystem> Fireworks = new List<ParticleSystem>();

    private float spawnTimer;
    private bool IsCheckingForNewData = false;
    private DonationStorageDto<MonetaryStatusResponse> currentMoneyStatus;
    private DonationStorageDto<MonetaryStatusResponse> newMoneyStatus;


    private enum State
    {
        WaitingForMoreMoney,
        Spawning,
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

            case State.WaitingForCoinsToFall:
                HandleWaitingForCoinsToFall();
                break;
        }
    }

    private void HandleWaitingForMoreMoney()
    {

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            MonetaryStatusResponse current = currentMoneyStatus.Data;
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
            
            current.Stripe.Amount = current.Stripe.Amount + UnityEngine.Random.Range(10, 50);

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

            // Check if we've reached the max
            if (coinSpawner.AllCoins.Count + newMoneyRecieved >= MaxNumberOfCoins)
            {
                foreach (var firework in Fireworks)
                {
                    firework.Play();
                }
                coinSpawner.LetCoinsFall();
                currentState = State.WaitingForCoinsToFall;
                return;
            }

            currentMoneyStatus = newMoneyStatus;
            if (newMoneyRecieved > 0)
            {
                coinSpawner.SpawnCoins((int)newMoneyRecieved);
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
