using ClassLibrary.Dtos.Configuration;
using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace ServerService.Logic
{
  internal sealed class ConfigurationSyncService : BackgroundService
  {
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(1);

    private readonly IConfigReader _configReader;
    private readonly ITokenProvider _tokenProvider;
    private readonly ILogger<ConfigurationSyncService> _logger;
    private HubConnection? _hubConnection;

    // Signals the main loop that the connection has closed and must be restarted
    private TaskCompletionSource _closedSignal = NewSignal();
    private bool _isDisconnected;

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
      try
      {
        var config = _configReader.Configuration;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{config.ApiBaseUrl.TrimEnd('/')}/hubs/configuration", options =>
            {
              options.AccessTokenProvider = async () => await _tokenProvider.GetTokenAsync(stoppingToken);
            })
            .WithAutomaticReconnect(new InfiniteRetryPolicy(MaxRetryDelay))
            .Build();

        RegisterHandlers(_hubConnection, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
          await ConnectWithRetryAsync(_hubConnection, stoppingToken);
          await _closedSignal.Task.WaitAsync(stoppingToken);
          _closedSignal = NewSignal();
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        // normal shutdown
      }
      catch (Exception ex)
      {
        _logger.LogCritical(ex, "Configuration sync service stopped unexpectedly.");
      }
    }

    private async Task ConnectWithRetryAsync(HubConnection connection, CancellationToken stoppingToken)
    {
      var attempt = 0;

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          await connection.StartAsync(stoppingToken);
          MarkConnected(attempt);
          return;
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
          attempt++;
          MarkDisconnected(ex, attempt);

          var delay = InfiniteRetryPolicy.GetDelay(attempt, MaxRetryDelay);
          await Task.Delay(delay, stoppingToken);
        }
      }
    }

    private void RegisterHandlers(HubConnection connection, CancellationToken stoppingToken)
    {
      // Client result: the API calls InvokeAsync<ConfigurationDto>("UpdateConfiguration", dto)
      connection.On<ConfigurationDto, ConfigurationDto>(
          "UpdateConfiguration",
          async dto =>
          {
            _logger.LogInformation("Received configuration update.");
            await _configReader.UpdateAsync(dto, stoppingToken);
            return GetCurrentConfiguration();
          });

      // Client result: the API calls InvokeAsync<ConfigurationDto>("GetConfiguration")
      connection.On<ConfigurationDto>(
          "GetConfiguration",
          () =>
          {
            _logger.LogInformation("Configuration requested by API.");
            return GetCurrentConfiguration();
          });

      connection.Reconnecting += error =>
      {
        MarkDisconnected(error, 1);
        return Task.CompletedTask;
      };

      connection.Reconnected += connectionId =>
      {
        MarkConnected(0);
        _logger.LogDebug("Reconnected with ConnectionId {ConnectionId}", connectionId);
        return Task.CompletedTask;
      };

      connection.Closed += error =>
      {
        _logger.LogDebug(error, "Configuration hub connection closed; restarting connect loop.");
        _closedSignal.TrySetResult();
        return Task.CompletedTask;
      };
    }

    private void MarkDisconnected(Exception? error, int attempt)
    {
      if (!_isDisconnected)
      {
        _isDisconnected = true;
        _logger.LogWarning(error,
          "Lost connection to configuration hub. Retrying in the background; further failures are suppressed.");
      }
      else
      {
        _logger.LogDebug("Configuration hub connect attempt {Attempt} failed: {Message}", attempt, error?.Message);
      }
    }

    private void MarkConnected(int attempts)
    {
      if (_isDisconnected)
      {
        _isDisconnected = false;
        _logger.LogInformation("Connection to configuration hub restored.");
      }
      else if (attempts == 0)
      {
        _logger.LogInformation("Connected to configuration hub.");
      }
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

    private static TaskCompletionSource NewSignal() =>
      new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      await base.StopAsync(cancellationToken);

      if (_hubConnection is not null)
      {
        await _hubConnection.DisposeAsync();
        _hubConnection = null;
      }
    }

    /// <summary>Retries forever with exponential backoff: 0s, 2s, 4s, 8s ... capped at maxDelay.</summary>
    private sealed class InfiniteRetryPolicy(TimeSpan maxDelay) : IRetryPolicy
    {
      public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
        GetDelay((int)retryContext.PreviousRetryCount, maxDelay);

      public static TimeSpan GetDelay(int attempt, TimeSpan maxDelay)
      {
        if (attempt <= 0) return TimeSpan.Zero;
        var seconds = Math.Pow(2, Math.Min(attempt, 10));
        return TimeSpan.FromSeconds(Math.Min(seconds, maxDelay.TotalSeconds));
      }
    }
  }
}
