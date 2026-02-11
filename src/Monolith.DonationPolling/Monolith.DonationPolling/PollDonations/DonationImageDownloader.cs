using RestSharp;

namespace Monolith.DonationPolling.PollDonations;

public class DonationImageDownloader(
    IHttpClientFactory httpClientFactory,
    ILogger<DonationImageDownloader> logger)
{
    public async Task<byte[]?> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var client = new RestClient(httpClient);
        var request = new RestRequest(url, Method.Get);


        RestResponse executeResponse;
        try
        {
            logger.LogInformation($"Calls image url: '{url}'");
            executeResponse = await client.ExecuteAsync(request, cancellationToken);
            logger.LogInformation($"Response from image url '{url}': ({executeResponse.StatusCode}) bytes: {executeResponse.RawBytes?.Length}");
            if (!executeResponse.IsSuccessStatusCode)
            {
                logger.LogWarning($"Got a failed status code from image url: '{url}'. Content: '{executeResponse.Content}'");
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to get a response from image url: '{url}'");
            return null;
        }

        if (executeResponse.RawBytes?.Length <= 0)
        {
            return null;
        }
        return executeResponse.RawBytes;
    }
}
