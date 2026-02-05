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

        string portalUrl = configuration.GetRequiredValue<string>("DonationPlatform:PortalUrl");
        string donationUrl = configuration.GetRequiredValue<string>("DonationPlatform:DonationUrl");
        string finalUrl = $"{donationUrl}/Donation/GetMonetaryStatus";

        // Gets a RestClient with a re-usable JWT token
        RestClient client = await clientFactory.GetClientAsync(cancellationToken);
        var request = new RestRequest(finalUrl, Method.Get);
        request.AddOrUpdateHeader("user-agent", "nordicfuzzcon");
        try
        {
            var executeResponse = await client.ExecuteAsync(request, cancellationToken);
            logger.LogInformation("Received a response! Squeee");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get a response :'(");
        }

    }
}
