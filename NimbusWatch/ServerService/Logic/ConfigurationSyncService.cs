using ClassLibrary.Dtos.Configuration;
using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace ServerService.Logic
{
  internal sealed class ConfigurationSyncService : BackgroundService
  {
    private readonly IConfigReader _configReader;
    private readonly ITokenProvider _tokenProvider;
    private readonly ILogger<ConfigurationSyncService> _logger;
    private HubConnection? _hubConnection;

    public ConfigurationSyncService(
        IConfigReader configReader,
        ITokenProvider tokenProvider,
        ILogger<ConfigurationSyncService> logger)
    {
      _configReader = configReader;
      _tokenProvider = tokenProvider;
      _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      var config = _configReader.Configuration;

      _hubConnection = new HubConnectionBuilder()
          .WithUrl($"{config.ApiBaseUrl.TrimEnd('/')}/hubs/configuration", options =>
          {
            options.AccessTokenProvider = async () =>
                await _tokenProvider.GetTokenAsync(stoppingToken);
          })
          .WithAutomaticReconnect()
          .Build();

      // Client result: the API calls InvokeAsync<ConfigurationDto>("UpdateConfiguration", dto)
      _hubConnection.On<ConfigurationDto, ConfigurationDto>(
          "UpdateConfiguration",
          async dto =>
          {
            _logger.LogInformation("Received configuration update.");
            await _configReader.UpdateAsync(dto, stoppingToken);
            return GetCurrentConfiguration();
          });

      // Client result: the API calls InvokeAsync<ConfigurationDto>("GetConfiguration")
      _hubConnection.On<ConfigurationDto>(
          "GetConfiguration",
          () =>
          {
            _logger.LogInformation("Configuration requested by API.");
            return GetCurrentConfiguration();
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

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          await _hubConnection.StartAsync(stoppingToken);
          _logger.LogInformation("Connected to configuration hub.");
          break;
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
          _logger.LogWarning(ex, "Failed to connect to configuration hub. Retrying in 5s.");
          await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
      }

      await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private ConfigurationDto GetCurrentConfiguration()
    {
      var current = _configReader.Configuration;
      return new ConfigurationDto
      {
        ReportFrequency = current.ReportFrequencyInMilliSec,
        MonitoredServices = current.Services
      };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      if (_hubConnection is not null)
      {
        await _hubConnection.StopAsync(cancellationToken);
        await _hubConnection.DisposeAsync();
        _hubConnection = null;
      }

      await base.StopAsync(cancellationToken);
    }
  }
}
