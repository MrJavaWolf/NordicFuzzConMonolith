using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;
using System.Text.Json;

namespace Monolith.DonationPolling.PollDonations;

public class DonationPlatformClientFactory(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<DonationPlatformClientFactory> logger)
{
    private AuthResponse? TokenData { get; set; }

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private DateTime? Expires { get; set; }

    public List<string> Scopes { get; } = [];

    public async Task<RestClient> GetClientAsync(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
            throw new Exception("Could not get token");


        var options = new RestClientOptions()
        {
            Authenticator = new JwtAuthenticator(token),
        };
        var httpClient = httpClientFactory.CreateClient();
        var client = new RestClient(httpClient, options);
        return new RestClient(options);
    }

    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {

        string? tokenFromConfig = configuration.GetValue<string>("DonationPlatform:Auth:BearerToken");
        if (!string.IsNullOrWhiteSpace(tokenFromConfig))
        {
            return tokenFromConfig;
        }

        //return read_jwt;
        if (Expires != null && DateTime.UtcNow < Expires.Value && !string.IsNullOrEmpty(TokenData?.access_token))
            return TokenData.access_token;

        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            if (Expires != null && DateTime.UtcNow < Expires.Value && !string.IsNullOrEmpty(TokenData?.access_token))
                return TokenData.access_token;

            var res = await GenerateToken(cancellationToken);
            if (res == null)
            {
                Expires = null;
                return null;
            }

            SetTokenData(res);

            return res.access_token;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private void SetTokenData(AuthResponse data)
    {
        TokenData = data;
        Expires = DateTime.UtcNow.AddSeconds(data.expires_in);

        Scopes.Clear();
        if (!string.IsNullOrEmpty(data.scope))
        {
            var scopes = data.scope.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            Scopes.AddRange(scopes);
        }
    }

    private async Task<AuthResponse?> GenerateToken(CancellationToken cancellationToken)
    {
        logger.LogInformation("Gets a new JWT token");
        var hrSettings = new AuthData
        {
            Authority = configuration.GetRequiredValue<string>("DonationPlatform:Auth:Authority"),
            Audience = configuration.GetRequiredValue<string>("DonationPlatform:Auth:Audience"),
            ClientId = configuration.GetRequiredValue<string>("DonationPlatform:Auth:ClientId"),
            ClientSecret = configuration.GetRequiredValue<string>("DonationPlatform:Auth:ClientSecret")
        };

        if (string.IsNullOrEmpty(hrSettings.Authority))
            throw new ArgumentNullException(nameof(AuthData.Authority));
        if (string.IsNullOrEmpty(hrSettings.Audience))
            throw new ArgumentNullException(nameof(AuthData.Audience));
        if (string.IsNullOrEmpty(hrSettings.ClientId))
            throw new ArgumentNullException(nameof(AuthData.ClientId));
        if (string.IsNullOrEmpty(hrSettings.ClientSecret))
            throw new ArgumentNullException(nameof(AuthData.ClientSecret));

        var httpClient = httpClientFactory.CreateClient();
        var options = new RestClientOptions(hrSettings.Authority);
        var client = new RestClient(httpClient, options);
        var req = new RestRequest("/oauth/token");

        var reqData = new AuthRequest
        {
            grant_type = "client_credentials",
            audience = hrSettings.Audience,
            client_id = hrSettings.ClientId,
            client_secret = hrSettings.ClientSecret
        };
        req.AddJsonBody(reqData);

        var res = await client.PostAsync(req, cancellationToken);
        if (!res.IsSuccessful)
        {
            // Log error especially when the client secret has changed
            logger.LogError($"Failed to get a JWT token: ({res.StatusCode}) '{res.Content}'");
            return null;
        }
        if (string.IsNullOrEmpty(res.Content))
        {
            logger.LogError($"Failed to get a JWT token: ({res.StatusCode}) Empty content: '{res.Content}' ");
            return null;
        }

        var data = JsonSerializer.Deserialize<AuthResponse>(res.Content);
        return data;
    }

    internal class AuthResponse
    {
        public string? access_token { get; set; }
        public string? scope { get; set; }
        public int expires_in { get; set; }
        //public string token_type { get; set; }
    }

    internal class AuthRequest
    {
        public required string client_id { get; set; }
        public required string client_secret { get; set; }
        public required string audience { get; set; }
        public required string grant_type { get; set; }
    }

    internal class AuthData
    {
        public required string Authority { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string Audience { get; set; }
    }
}