using Monolith.DonationPolling.PollDonations;
using NFC.Donation.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TotalAmountController : MonoBehaviour, ICoolEffectState
{
    public DataStorage dataStorage;
    public CoinSpawner coinSpawner;
    public TotalAmountUiTextCounter TextUi;
    public int MaxNumberOfCoins = 600;
    public float CheckForMoneyIntervalSeconds = 10f;

    public float FireworksWaitTime = 3.5f;
    public List<ParticleSystem> Fireworks = new();

    public float CoinsJumpInterval = 30;
    private float lastCoinJumpTime;


    public int DebugMinimumMoney = 50;
    public int DebugMaximumMoney = 5000;

    private float spawnTimer;
    private bool IsCheckingForNewData = false;
    private DonationStorageDto<MonetaryStatusResponse> currentMoneyStatus;
    private DonationStorageDto<MonetaryStatusResponse> newMoneyStatus;

    private readonly List<SpriteRenderer> spriteRenderers = new();

    // ---- State implementation ----
    [Header("Transition")]
    public float transitionDuration = 1.0f;
    public CoolEffectState SimulationState { get; private set; } = CoolEffectState.Stopped;
    float stateChangeTime;
    float stateAlpha;
    private bool waitingForMoreMoneyStateToStop = false;


    private float currentStateStartTime;
    private float FireworksStartTime = -1;
    private enum State
    {
        WaitingForMoreMoney,
        Spawning,
        WaitingForFireworks,
        WaitingForCoinsToFall
    }

    private State _currentState { get; set; } = State.WaitingForMoreMoney;
    private State currentState
    {
        get => _currentState;
        set
        {
            Debug.Log($"Changes state from '{_currentState}' to '{value}'");
            _currentState = value;
            currentStateStartTime = Time.time;
        }
    }

    public void Start()
    {
        RefreshSpriteCache();
        SetSpritsAlpha(0);
        TextUi.SetAlpha(0);
    }

    void Update()
    {
        UpdateStateAlpha();

        if (SimulationState == CoolEffectState.Stopped)
            return;

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
            Debug.Log($"Current money amount: {currentMoney}, new money amount: {newMoney}, diff: {newMoneyRecieved}");

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
            newMoneyStatus = null;
            TextUi.SetAmount(newMoney);
            if (newMoneyRecieved > 0)
            {
                Debug.Log($"Ones: {ones}, Fifties: {fifties}, TwoHundreds: {twoHundreds}");
                coinSpawner.SpawnCoins((ones, fifties, twoHundreds));
                currentState = State.Spawning;
                return;
            }
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer < CheckForMoneyIntervalSeconds)
            return;

        spawnTimer = 0f;
        CheckForNewTotalAmountData();

        if (Time.time - CoinsJumpInterval > currentStateStartTime && // Have to been in the state for a while
            Time.time - lastCoinJumpTime > CoinsJumpInterval) // Wait for the jump interval time
        {
            Debug.Log("Nothing have happend in a while, will make the coins jump");
            coinSpawner.MakeCoinsJump();
            lastCoinJumpTime = Time.time;
        }
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


    public void StartCoolEffect()
    {
        if (SimulationState == CoolEffectState.Running ||
            SimulationState == CoolEffectState.Starting)
        {
            return;
        }
        RefreshSpriteCache();
        SimulationState = CoolEffectState.Starting;
        stateChangeTime = Time.time;
    }

    public void StopCoolEffect()
    {
        if (SimulationState == CoolEffectState.Stopped ||
            SimulationState == CoolEffectState.Stopping)
        {
            return;
        }
        RefreshSpriteCache();
        SimulationState = CoolEffectState.Stopping;
        stateChangeTime = Time.time;
        waitingForMoreMoneyStateToStop = true;
    }


    public void RefreshSpriteCache()
    {
        spriteRenderers.Clear();
        GetComponentsInChildren(spriteRenderers);
    }

    void UpdateStateAlpha()
    {
        float t = Mathf.Clamp01((Time.time - stateChangeTime) / transitionDuration);

        switch (SimulationState)
        {
            case CoolEffectState.Starting:
                stateAlpha = t;
                if (t >= 1)
                {
                    Debug.Log($"Simulation {nameof(TotalAmountController)} is now running");
                    SimulationState = CoolEffectState.Running;
                }
                break;
            case CoolEffectState.Running:
                stateAlpha = 1f;
                break;
            case CoolEffectState.Stopping:
                if (waitingForMoreMoneyStateToStop && currentState != State.WaitingForMoreMoney)
                {
                    return;
                }
                else if (newMoneyStatus != null)
                {
                    return;
                }
                else if (currentState == State.WaitingForMoreMoney && waitingForMoreMoneyStateToStop)
                {
                    waitingForMoreMoneyStateToStop = false;
                    RefreshSpriteCache();
                    stateChangeTime = Time.time;
                    return;
                }


                stateAlpha = 1f - t;
                if (t >= 1 && currentState == State.WaitingForMoreMoney)
                {
                    SimulationState = CoolEffectState.Stopped;
                    Debug.Log($"Simulation {nameof(TotalAmountController)} now stopped");
                    stateAlpha = 0;
                }
                break;
            case CoolEffectState.Stopped:
                stateAlpha = 0f;
                break;
        }

        SetSpritsAlpha(stateAlpha);
        TextUi.SetAlpha(stateAlpha);
    }

    private void SetSpritsAlpha(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] == null)
                continue;
            var color = spriteRenderers[i].color;
            color.a = alpha;
            spriteRenderers[i].color = color;
        }
    }
}
