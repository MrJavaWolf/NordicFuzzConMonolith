using NFC.Donation.Api;
using NFC.Portal.Api;
using RestSharp;

namespace Monolith.DonationPolling.PollDonations;

public class PollDonationService(
    IConfiguration configuration,
    DonationPlatformClientFactory clientFactory,
    ILogger<PollDonationService> logger)
{

    public async Task Poll(CancellationToken cancellationToken)
    {
        await CallEndpointThingie(cancellationToken);
    }

    private async Task CallEndpointThingie(CancellationToken cancellationToken)
    {
        logger.LogInformation("Calling...");

        CachedCharityDonations? portal_result1 = await GetCharityDonationsAsync(cancellationToken);
        MonetaryStatusResponse? dontaiont_result = await GetMonetaryStatusAsync(cancellationToken);
        DonationListResponse? dontaiont1_result = await GetBiggestDonationsAsync(cancellationToken);
        DonationListResponse? dontaiont2_result = await GetLatestDonationsAsync(cancellationToken);
        DonationStatisticsResponse? dontaiont3_result = await GetBiggestDonationStatisticsAsync(cancellationToken);

        logger.LogInformation("Done!");
    }

    private string PortalUrl => configuration.GetRequiredValue<string>("DonationPlatform:PortalUrl");
    private string DonationUrl => configuration.GetRequiredValue<string>("DonationPlatform:DonationUrl");


    private async Task<CachedCharityDonations?> GetCharityDonationsAsync(CancellationToken cancellationToken)
    {
        string url = PortalUrl + "/CharityDonations/GetCharityDonations";
        return await GetSomething<CachedCharityDonations>(url, cancellationToken);
    }

    private async Task<MonetaryStatusResponse?> GetMonetaryStatusAsync(
        CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Donation/GetMonetaryStatus";
        return await GetSomething<MonetaryStatusResponse>(url, cancellationToken);
    }

    private async Task<DonationListResponse?> GetBiggestDonationsAsync(
        CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Donation/GetBiggestDonations";
        return await GetSomething<DonationListResponse>(url, cancellationToken);
    }

    private async Task<DonationListResponse?> GetLatestDonationsAsync(CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Donation/GetLatestDonations";
        return await GetSomething<DonationListResponse>(url, cancellationToken);
    }

    private async Task<DonationStatisticsResponse?> GetBiggestDonationStatisticsAsync(
        CancellationToken cancellationToken)
    {
        string url = DonationUrl + "/Donation/GetBiggestDonationStatistics";
        return await GetSomething<DonationStatisticsResponse>(url, cancellationToken);
    }


    private async Task<T?> GetSomething<T>(string url, CancellationToken cancellationToken) where T : class
    {
        // Gets a RestClient with a re-usable JWT token
        RestClient client = await clientFactory.GetClientAsync(cancellationToken);
        var request = new RestRequest(url, Method.Get);
        request.AddOrUpdateHeader("user-agent", "donation-pillar");
        request.AddOrUpdateHeader("x-nfc-donation-pillar", "true");
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
