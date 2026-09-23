using ClassLibrary.Dtos.Configuration;
using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text;
using System.Text.Json;

namespace ServerService.Logic
{
  internal sealed class ConfigurationSyncService : BackgroundService
  {
    private readonly IConfigReader _configReader;
    private readonly ITokenProvider _tokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigurationSyncService> _logger;
    private HubConnection? _hubConnection;

    public ConfigurationSyncService(
        IConfigReader configReader,
        ITokenProvider tokenProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<ConfigurationSyncService> logger)
    {
      _configReader = configReader;
      _tokenProvider = tokenProvider;
      _httpClientFactory = httpClientFactory;
      _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      var config = _configReader.Configuration;

      _hubConnection = new HubConnectionBuilder()
          .WithUrl($"{config.ApiBaseUrl.TrimEnd('/')}/hubs/configuration?serviceId={config.ClientId}", options =>
          {
            options.AccessTokenProvider = async () =>
                      await _tokenProvider.GetTokenAsync(stoppingToken);
          })
          .WithAutomaticReconnect()
          .Build();

      _hubConnection.On<ConfigurationDto>(
          "ReceiveConfigurationUpdate",
          async dto =>
          {
            _logger.LogInformation("Received configuration update.");
            await _configReader.UpdateAsync(dto, stoppingToken);
          });

      _hubConnection.On<string>(
          "ReceiveConfigurationRequest",
          async requestId =>
          {
            _logger.LogInformation("Received configuration request {RequestId}.", requestId);

            var current = _configReader.Configuration;

            var dto = new ConfigurationDto
            {
              ReportFrequency = current.ReportFrequencyInMilliSec,
              MonitoredServices = current.Services
            };

            var client = _httpClientFactory.CreateClient("ApiClient");

            var json = JsonSerializer.Serialize(
                dto,
                AppJsonSerializerContext.Default.ConfigurationDto);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync(
                $"api/configuration/response/{requestId}",
                content,
                stoppingToken);

            if (!response.IsSuccessStatusCode)
            {
              _logger.LogWarning(
                        "Failed to send configuration response. StatusCode: {StatusCode}",
                        response.StatusCode);
            }
          });

      _hubConnection.Reconnecting += error =>
      {
        _logger.LogWarning(error, "Configuration hub reconnecting.");
        return Task.CompletedTask;
      };

      _hubConnection.Reconnected += connectionId =>
      {
        _logger.LogInformation("Configuration hub reconnected. ConnectionId: {ConnectionId}", connectionId);
        return Task.CompletedTask;
      };

      _hubConnection.Closed += error =>
      {
        _logger.LogWarning(error, "Configuration hub connection closed.");
        return Task.CompletedTask;
      };

      await _hubConnection.StartAsync(stoppingToken);
      _logger.LogInformation("Connected to configuration hub.");

      await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      if (_hubConnection is not null)
      {
        await _hubConnection.StopAsync(cancellationToken);
        await _hubConnection.DisposeAsync();
      }

      await base.StopAsync(cancellationToken);
    }
  }
}
