using ClassLibrary.Dtos;
using ClassLibrary.Services;
using System.Text;
using System.Text.Json;

namespace ServerService.Logic
{
  internal sealed class TokenProvider : ITokenProvider
  {
    private readonly IConfigReader configReader;
    private readonly HttpClient httpClient;
    private readonly ILogger<TokenProvider> logger;
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    private string? accessToken;
    private DateTimeOffset expiresAtUtc = DateTimeOffset.MinValue;

    public TokenProvider(
      IConfigReader configReader,
      IHttpClientFactory factory,
      ILogger<TokenProvider> logger)
    {
      this.configReader = configReader;
      httpClient = factory.CreateClient("TokenClient");
      this.logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
      if (HasValidToken())
        return accessToken!;

      await refreshLock.WaitAsync(cancellationToken);
      try
      {
        if (HasValidToken())
          return accessToken!;

        await RequestTokenAsync(cancellationToken);
        return accessToken!;
      }
      finally
      {
        refreshLock.Release();
      }
    }

    private bool HasValidToken()
    {
      var refreshBuffer = TimeSpan.FromSeconds(
        Math.Max(0, configReader.Configuration.TokenRefreshBufferSeconds));

      return !string.IsNullOrWhiteSpace(accessToken)
             && expiresAtUtc > DateTimeOffset.UtcNow.Add(refreshBuffer);
    }

    private async Task RequestTokenAsync(CancellationToken cancellationToken)
    {
      var config = configReader.Configuration;

      if (string.IsNullOrWhiteSpace(config.ApiBaseUrl))
        throw new InvalidOperationException("Missing ApiBaseUrl in config.json.");

      if (string.IsNullOrWhiteSpace(config.ServiceTokenPath))
        throw new InvalidOperationException("Missing ServiceTokenPath in config.json.");

      if (string.IsNullOrWhiteSpace(config.ClientId))
        throw new InvalidOperationException("Missing ClientId in config.json.");

      if (string.IsNullOrWhiteSpace(config.ClientSecret))
        throw new InvalidOperationException("Missing ClientSecret in config.json.");

      var request = new ServiceTokenRequestDto
      {
        GrantType = "client_credentials",
        ClientId = config.ClientId,
        ClientSecret = config.ClientSecret
      };

      var json = JsonSerializer.Serialize(
        request,
        AppJsonSerializerContext.Default.ServiceTokenRequestDto);

      using var content = new StringContent(json, Encoding.UTF8, "application/json");

      using var response = await httpClient.PostAsync(
        $"{config.ServiceTokenPath}/{config.CompanyId}",
        content,
        cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError("Failed to fetch service token. Status: {StatusCode}. Body: {Body}", response.StatusCode, error);
        throw new HttpRequestException($"Unable to fetch service token. Status code: {response.StatusCode}");
      }

      var body = await response.Content.ReadAsStringAsync(cancellationToken);

      var tokenResponse = JsonSerializer.Deserialize(
        body,
        AppJsonSerializerContext.Default.ServiceTokenResponseDto);

      if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        throw new InvalidOperationException("Token endpoint returned an invalid response.");

      accessToken = tokenResponse.AccessToken;
      expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, tokenResponse.ExpiresIn));

      logger.LogInformation("Fetched new service access token. Expires at {ExpiresAtUtc}.", expiresAtUtc);
    }
  }
}
