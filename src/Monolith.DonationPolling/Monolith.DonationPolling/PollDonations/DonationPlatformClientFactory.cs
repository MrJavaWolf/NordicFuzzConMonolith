using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;

namespace Monolith.DonationPolling.PollDonations;

public class DonationPlatformClientFactory(IConfiguration configuration)
{
    private AuthResponse? TokenData { get; set; }

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private DateTime? Expires { get; set; }

    public List<string> Scopes { get; } = [];

    public async Task<RestClient> GetClientAsync(CancellationToken cancellationToken)
    {
        var baseUrl = configuration.GetRequiredValue<string>("DonationPlatform:Auth:BaseUrl");
        if (string.IsNullOrEmpty(baseUrl))
            throw new ArgumentNullException(nameof(baseUrl));

        var token = await GetTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
            throw new Exception("Could not get token");


        // Create a handler that forces IPv4
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, token) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host);
                // Filter only IPv4 addresses
                var ipv4 = Array.Find(addresses, a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (ipv4 == null)
                    throw new Exception("No IPv4 address found for host");

                var endpoint = new IPEndPoint(ipv4, context.DnsEndPoint.Port);
                var socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(endpoint, token);
                return new NetworkStream(socket, ownsSocket: true);
            }
        };
        handler.SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12,
            ApplicationProtocols = new List<SslApplicationProtocol>
            {
                SslApplicationProtocol.Http11
            }
        };


        // Pass it to RestSharp
        var options = new RestClientOptions(baseUrl)
        {
            Authenticator = new JwtAuthenticator("Bearer " + token),
            ConfigureMessageHandler = _ => handler
        };

        // Create HttpClient with the handler
        var httpClient = new HttpClient(handler);

        var client = new RestClient(httpClient, options);

        var request = new RestRequest("endpoint", Method.Get);
        var response = await client.ExecuteAsync(request);
        Console.WriteLine(response.Content);

        return new RestClient(options);
    }

    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {

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

        var client = new RestClient(new Uri(hrSettings.Authority));
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
            return null;
        }
        if (string.IsNullOrEmpty(res.Content))
            return null;

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