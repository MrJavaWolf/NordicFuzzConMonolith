using Monolith.DonationPolling.PollDonations;
using NFC.Donation.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class LatestDonationsController : MonoBehaviour, ICoolEffectState
{
    public DataStorage dataStorage;
    public GameObject LatestDonorPrefab;
    public GameObject SpecialDonorPrefab;
    public GameObject BiggestDonorPrefab;
    public Vector2 areaSize = new(5f, 3f);
    public List<Sprite> TopDonorLogos = new();
    public List<Color> LatestDonorsColorPallet = new();
    public List<Sprite> DebugSpecialDonorImages = new();

    public int MaxNumberOfLatestDonors = 20;
    public int MaxNumberOfBiggestDonors = 10;
    public int MaxNumberOfSpecialDonors = 10;

    // ---- State implementation ----
    [Header("Transition")]
    public float transitionDuration = 1.0f;
    public CoolEffectState SimulationState { get; private set; } = CoolEffectState.Stopped;
    public bool IsCheckingForNewData { get; private set; }

    public float CheckForNewDataIntervalSeconds = 10f;
    private float LastCheckForNewDataTime = 0f;

    float stateChangeTime;
    float stateAlpha;
    private readonly List<SpriteRenderer> spriteRenderers = new();

    private DonationStorageDto<DonationListResponse> currentLatestDonations;
    private DonationStorageDto<DonationListResponse> newLatestDonations;

    private DonationStorageDto<DonationListResponse> currentBiggestDonations;
    private DonationStorageDto<DonationListResponse> newBiggestDonations;

    private static readonly System.Globalization.CultureInfo DanishCulture = new("da-DK");

    private DonationStorageDto<LastImageDonations> currentSpecialDonations;
    private DonationStorageDto<LastImageDonations> newSpecialDonations;

    private List<DonorBobble> BiggestDonors = new();
    private List<DonorBobble> LatestDonors = new();
    private List<DonorBobble> SpecialDonors = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateStateAlpha();

        if (SimulationState == CoolEffectState.Stopped)
            return;

        if (Time.time - LastCheckForNewDataTime > CheckForNewDataIntervalSeconds)
        {
            LastCheckForNewDataTime = Time.time;
            CheckForNewData();
        }


        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            SpawnLatestDonor(UnityEngine.Random.Range(1, 10000), Guid.NewGuid().ToString());
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            SpawnBiggestDonor(UnityEngine.Random.Range(1000, 10000), Guid.NewGuid().ToString());
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            string[] exampleFurryNames = new string[5];
            exampleFurryNames[0] = "Mr. Woof";
            exampleFurryNames[1] = "Bork";
            exampleFurryNames[2] = "Meow";
            exampleFurryNames[3] = "Pink Fluffy Snuggles";
            exampleFurryNames[4] = "xxx_Gamer_girl_xxx";
            string furryName = exampleFurryNames[UnityEngine.Random.Range(0, exampleFurryNames.Length)];
            int amount = UnityEngine.Random.Range(1, 100);
            Sprite image = DebugSpecialDonorImages[UnityEngine.Random.Range(0, DebugSpecialDonorImages.Count)];
            SpawnSpecialDonor(furryName, amount, image, Guid.NewGuid().ToString());
        }

        HandleNewLatestDonations();
        HandleNewBiggestDonations();
        HandleNewSpecialDonations();
    }


    private void HandleNewLatestDonations()
    {
        if (newLatestDonations == null)
        {
            return;
        }
        List<(string donationId, long amount)> donations = GetDonaitions(newLatestDonations);

        foreach (var donation in donations.Take(MaxNumberOfLatestDonors))
        {
            if (LatestDonors.Any(x => x.DonationId == donation.donationId))
                continue;
            SpawnLatestDonor(donation.amount, donation.donationId);
        }
        currentLatestDonations = newLatestDonations;
    }

    private void HandleNewBiggestDonations()
    {
        if (newBiggestDonations == null)
        {
            return;
        }
        List<(string donationId, long amount)> donations = GetDonaitions(newBiggestDonations);

        foreach (var donation in donations.Take(MaxNumberOfBiggestDonors))
        {
            if (BiggestDonors.Any(x => x.DonationId == donation.donationId))
                continue;
            SpawnBiggestDonor(donation.amount, donation.donationId);
        }
        currentBiggestDonations = newBiggestDonations;
    }

    private void HandleNewSpecialDonations()
    {
        if (newSpecialDonations == null) return;

        if (newSpecialDonations.Data == null) return;
        if (newSpecialDonations.Data.List == null) return;
        if (newSpecialDonations.Data.List.Count == 0) return;

        List<LastImageDonationsData> donations = new List<LastImageDonationsData>();

        foreach (var donation in newSpecialDonations.Data.List)
        {
            if (donation == null) continue;
            if (donation.BadgeNumber == 0) continue;
            donations.Add(donation);
        }

        foreach (var donation in donations.Take(MaxNumberOfSpecialDonors))
        {
            if (SpecialDonors.Any(x => x.DonationId == donation.BadgeNumber.ToString()))
                continue;
            Sprite furryImage = TryLoadImage(donation.ImageUrl);


            SpawnSpecialDonor(donation.NickName, donation.Amount, furryImage, donation.BadgeNumber.ToString());
        }
        currentSpecialDonations = newSpecialDonations;
    }

    private Sprite TryLoadImage(string imageUrl)
    {
        try
        {
            string imagePath = dataStorage.DonationImageStorage.GetImagePath(imageUrl);
            if (imagePath == null) return null;
            byte[] imageBytes = File.ReadAllBytes(imagePath);

            // Create texture (size doesn't matter, LoadImage will replace it)
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!texture.LoadImage(imageBytes))
            {
                Debug.LogError($"Failed to load image data from: {imagePath}");
                return null;
            }

            texture.Apply();

            // Create sprite from texture
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),   // pivot at center
                100f                      // pixels per unit (adjust if needed)
            );
            return sprite;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load image url: {imageUrl}. {e}");
            return null;
        }

    }

    private List<(string, long)> GetDonaitions(DonationStorageDto<DonationListResponse> inputDonations)
    {
        List<(string, long)> donations = new List<(string, long)>();
        if (inputDonations == null)
        {
            return donations;
        }

        if (inputDonations.Data == null)
        {
            return donations;
        }
        if (inputDonations.Data.Zettle != null)
        {
            if (inputDonations.Data.Zettle.Amount != null)
            {
                foreach (var donation in inputDonations.Data.Zettle.Amount)
                {
                    if (donation == null) continue;
                    if (string.IsNullOrWhiteSpace(donation.Id)) continue;
                    if (donation.Amount < 0) continue;
                    donations.Add((donation.Id, donation.Amount));
                }
            }
        }

        if (inputDonations.Data.Stripe != null)
        {
            if (inputDonations.Data.Stripe.Amount != null)
            {
                foreach (var newLatestDonation in inputDonations.Data.Stripe.Amount)
                {
                    if (newLatestDonation == null) continue;
                    if (string.IsNullOrWhiteSpace(newLatestDonation.Id)) continue;
                    if (newLatestDonation.Amount < 0) continue;
                    donations.Add((newLatestDonation.Id, newLatestDonation.Amount));
                }
            }
        }
        return donations;
    }

    public void SpawnLatestDonor(long amount, string donationId)
    {
        if (LatestDonors.Count > MaxNumberOfLatestDonors)
        {
            var donorToRemove = LatestDonors.First();
            LatestDonors.Remove(donorToRemove);
            StartCoroutine(donorToRemove.FadeOutAndDie());
        }

        Vector2 randomPos = GetRandomPosition();
        GameObject biggestObject = Instantiate(
            LatestDonorPrefab,
            randomPos,
            Quaternion.identity,
            transform
        );
        DonorBobble donorBobble = biggestObject.GetComponent<DonorBobble>();
        donorBobble.Text.text = amount.ToString("N0", DanishCulture);
        donorBobble.Background.color = LatestDonorsColorPallet[UnityEngine.Random.Range(0, LatestDonorsColorPallet.Count())];
        donorBobble.Border.color = donorBobble.Background.color * 0.5f;
        donorBobble.DonationId = donationId;
        StartCoroutine(donorBobble.FadeIn());
        LatestDonors.Add(donorBobble);
    }

    public void SpawnBiggestDonor(long amount, string donationId)
    {
        if (BiggestDonors.Count > MaxNumberOfBiggestDonors)
        {
            var donorToRemove = BiggestDonors.First();
            BiggestDonors.Remove(donorToRemove);
            StartCoroutine(donorToRemove.FadeOutAndDie());
        }


        Vector2 randomPos = GetRandomPosition();
        GameObject biggestObject = Instantiate(
            BiggestDonorPrefab,
            randomPos,
            Quaternion.identity,
            transform
        );
        DonorBobble donorBobble = biggestObject.GetComponent<DonorBobble>();
        donorBobble.Logo.sprite = TopDonorLogos[UnityEngine.Random.Range(0, TopDonorLogos.Count)];
        donorBobble.Logo.color = Color.white;
        donorBobble.Text.text = amount.ToString("N0", DanishCulture);
        donorBobble.DonationId = donationId;
        StartCoroutine(donorBobble.FadeIn());
        BiggestDonors.Add(donorBobble);
    }

    public void SpawnSpecialDonor(string furryName, long amount, Sprite sprite, string donationId)
    {
        if (SpecialDonors.Count > MaxNumberOfBiggestDonors)
        {
            var donorToRemove = SpecialDonors.First();
            SpecialDonors.Remove(donorToRemove);
            StartCoroutine(donorToRemove.FadeOutAndDie());
        }

        Vector2 randomPos = GetRandomPosition();
        GameObject biggestObject = Instantiate(
            SpecialDonorPrefab,
            randomPos,
            Quaternion.identity,
            transform
        );
        if (sprite == null)
        {
            sprite = DebugSpecialDonorImages[UnityEngine.Random.Range(0, DebugSpecialDonorImages.Count)];
        }
        DonorBobble donorBobble = biggestObject.GetComponent<DonorBobble>();
        donorBobble.Logo.sprite = sprite;
        donorBobble.Logo.color = Color.white;
        string finalString = "";
        if (!string.IsNullOrWhiteSpace(furryName))
        {
            finalString = furryName + Environment.NewLine;
        }
        if (amount > 0)
        {
            finalString += amount.ToString("N0", DanishCulture);
        }
        donorBobble.Text.text = finalString;
        donorBobble.DonationId = donationId;

        StartCoroutine(donorBobble.FadeIn());
        SpecialDonors.Add(donorBobble);
    }

    public Vector2 GetRandomPosition()
    {
        Vector2 center = transform.position;

        float x = UnityEngine.Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float y = UnityEngine.Random.Range(-areaSize.y / 2f, areaSize.y / 2f);

        return center + new Vector2(x, y);
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
                    Debug.Log($"Simulation {nameof(LatestDonationsController)} is now running");
                    SimulationState = CoolEffectState.Running;
                }
                break;
            case CoolEffectState.Running:
                stateAlpha = 1f;
                break;
            case CoolEffectState.Stopping:
                stateAlpha = 1f - t;
                if (t >= 1)
                {
                    SimulationState = CoolEffectState.Stopped;
                    Debug.Log($"Simulation {nameof(LatestDonationsController)} now stopped");
                    stateAlpha = 0;
                }
                break;
            case CoolEffectState.Stopped:
                stateAlpha = 0f;
                break;
        }

        SetSpritsAlpha(stateAlpha);
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



    private void CheckForNewData()
    {
        if (IsCheckingForNewData) return;
        IsCheckingForNewData = true;
        Task.Run(() =>
        {
            try
            {

                DonationDataPaths donationDataPaths = dataStorage.DonationDataPaths;
                if (!donationDataPaths.Enable) return;

                // Latest donations
                try
                {
                    string dataFile = donationDataPaths.GetCurrentDataFile(donationDataPaths.LatestDonationsPath);
                    DonationStorageDto<DonationListResponse> readStatus = DonationDataStorage.Load<DonationStorageDto<DonationListResponse>>(dataFile);
                    if (readStatus == null)
                    {
                        return;
                    }

                    if (currentLatestDonations == null)
                    {
                        newLatestDonations = readStatus;
                    }
                    else
                    {
                        if (currentLatestDonations.DataTimestamp != readStatus.DataTimestamp && readStatus.Data != null)
                        {
                            Debug.Log("New latest donations read");
                            newLatestDonations = readStatus;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to read latest donations: {ex}");
                }


                // Biggest donations
                try
                {
                    string dataFile = donationDataPaths.GetCurrentDataFile(donationDataPaths.BiggestDonationsPath);
                    DonationStorageDto<DonationListResponse> readStatus = DonationDataStorage.Load<DonationStorageDto<DonationListResponse>>(dataFile);
                    if (readStatus == null)
                    {
                        return;
                    }

                    if (currentBiggestDonations == null)
                    {
                        newBiggestDonations = readStatus;
                    }
                    else
                    {
                        if (currentBiggestDonations.DataTimestamp != readStatus.DataTimestamp && readStatus.Data != null)
                        {
                            Debug.Log("New biggest donations read");
                            newBiggestDonations = readStatus;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to read biggest donations: {ex}");
                }

                // Special donations
                try
                {
                    string dataFile = donationDataPaths.GetCurrentDataFile(donationDataPaths.LatestImageDonationsPath);
                    DonationStorageDto<LastImageDonations> readStatus = DonationDataStorage.Load<DonationStorageDto<LastImageDonations>>(dataFile);
                    if (readStatus == null)
                    {
                        return;
                    }

                    if (currentSpecialDonations == null)
                    {
                        newSpecialDonations = readStatus;
                    }
                    else
                    {
                        if (currentSpecialDonations.DataTimestamp != readStatus.DataTimestamp && readStatus.Data != null)
                        {
                            Debug.Log("New special donations read");
                            newSpecialDonations = readStatus;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to read biggest donations: {ex}");
                }


            }
            finally
            {
                IsCheckingForNewData = false;
            }
        });
    }


    // Visualize area in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
