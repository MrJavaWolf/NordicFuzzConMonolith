using NFC.Donation.Api;
using NFC.Portal.Api;
using RestSharp;

namespace Monolith.DonationPolling.PollDonations;

public class PollDonationService(
    IConfiguration configuration,
    DonationPlatformClientFactory clientFactory,
    DonationDataStorage dataStorage,
    DonationDataPaths dataPaths,
    ILogger<PollDonationService> logger)
{

    public async Task Poll(CancellationToken cancellationToken)
    {
        await CallEndpointThingie(cancellationToken);
    }

    private async Task CallEndpointThingie(CancellationToken cancellationToken)
    {
        CachedCharityDonations? cachedCharityDonations = await GetCharityDonationsAsync(cancellationToken);
        await dataStorage.SaveAsync(cachedCharityDonations, dataPaths.PortalCharityDonationsPath, cancellationToken);

        MonetaryStatusResponse? monetaryStatus = await GetMonetaryStatusAsync(cancellationToken);
        await dataStorage.SaveAsync(monetaryStatus, dataPaths.MonetaryStatusPath, cancellationToken);

        DonationListResponse? biggestDonations = await GetBiggestDonationsAsync(cancellationToken);
        await dataStorage.SaveAsync(biggestDonations, dataPaths.BiggestDonationsPath, cancellationToken);

        DonationListResponse? latestDonations = await GetLatestDonationsAsync(cancellationToken);
        await dataStorage.SaveAsync(latestDonations, dataPaths.LatestDonationsPath, cancellationToken);

        DonationStatisticsResponse? biggestDonationStatistics = await GetBiggestDonationStatisticsAsync(cancellationToken);
        await dataStorage.SaveAsync(biggestDonationStatistics, dataPaths.BiggestDonationStatisticsPath, cancellationToken);

        LastImageDonations? lastImageDonations = await GetLastImageDonationsAsync(cancellationToken);
        await dataStorage.SaveAsync(lastImageDonations, dataPaths.LatestImageDonationsPath, cancellationToken);
    }

    private string PortalUrl => configuration.GetRequiredValue<string>("DonationPlatform:PortalUrl");
    private string DonationUrl => configuration.GetRequiredValue<string>("DonationPlatform:DonationUrl");


    private async Task<CachedCharityDonations?> GetCharityDonationsAsync(CancellationToken cancellationToken)
    {
        string url = PortalUrl + "/CharityDonations/GetCharityDonations";
        return await SendRequest<CachedCharityDonations>(url, cancellationToken: cancellationToken);
    }

    private async Task<MonetaryStatusResponse?> GetMonetaryStatusAsync(
        CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Donation/GetMonetaryStatus";
        return await SendRequest<MonetaryStatusResponse>(url, cancellationToken: cancellationToken);
    }

    private async Task<DonationListResponse?> GetBiggestDonationsAsync(
        CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Donation/GetBiggestDonations";
        return await SendRequest<DonationListResponse>(url, cancellationToken: cancellationToken);
    }

    private async Task<DonationListResponse?> GetLatestDonationsAsync(CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Donation/GetLatestDonations";
        return await SendRequest<DonationListResponse>(url, cancellationToken: cancellationToken);
    }

    private async Task<DonationStatisticsResponse?> GetBiggestDonationStatisticsAsync(CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Donation/GetBiggestDonationStatistics";
        return await SendRequest<DonationStatisticsResponse>(url, cancellationToken: cancellationToken);
    }

    private async Task<LastImageDonations?> GetLastImageDonationsAsync(CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Image/GetLastDonations";
        Dictionary<string, string> query = new() { { "numberOfDonations", "20" } };
        return await SendRequest<LastImageDonations>(url, query: query, cancellationToken: cancellationToken);
    }



    private async Task<T?> SendRequest<T>(string url, Dictionary<string, string>? query = null, CancellationToken cancellationToken = default) where T : class
    {
        // Gets a RestClient with a re-usable JWT token
        RestClient client = await clientFactory.GetClientAsync(cancellationToken);
        var request = new RestRequest(url, Method.Get);

        // Add headers to get access to the API
        request.AddOrUpdateHeader("user-agent", "donation-pillar");
        request.AddOrUpdateHeader("x-nfc-donation-pillar", "true");

        // Add query if nessesary
        if (query != null)
        {
            foreach (var queryParameter in query)
            {
                request.AddQueryParameter(queryParameter.Key, queryParameter.Value);
            }
        }

        RestResponse executeResponse;
        try
        {
            logger.LogInformation($"Calls url: '{url}'");
            executeResponse = await client.ExecuteAsync(request, cancellationToken);
            logger.LogInformation($"Response from url '{url}': ({executeResponse.StatusCode}) '{executeResponse.Content}'");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to get a response from url: '{url}'");
            return null;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(executeResponse.Content))
            {
                return null;
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(executeResponse.Content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to deserialize response as '{typeof(T).FullName}' from url: '{url}', response: '{executeResponse.Content}'");
            return null;
        }

    }
}
