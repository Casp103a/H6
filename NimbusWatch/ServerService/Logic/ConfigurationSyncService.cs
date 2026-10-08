using ClassLibrary.Dtos.Configuration;
using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace ServerService.Logic
{
  internal sealed class ConfigurationSyncService : BackgroundService
  {
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(1);

    private readonly IConfigReader configReader;
    private readonly ITokenProvider tokenProvider;
    private readonly ILogger<ConfigurationSyncService> logger;
    private HubConnection? hubConnection;

    // Signals the main loop that the connection has closed and must be restarted
    private TaskCompletionSource closedSignal = NewSignal();
    private bool isDisconnected;

    public ConfigurationSyncService(
        IConfigReader configReader,
        ITokenProvider tokenProvider,
        ILogger<ConfigurationSyncService> logger)
    {
      this.configReader = configReader;
      this.tokenProvider = tokenProvider;
      this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      var config = configReader.Configuration;

      hubConnection = new HubConnectionBuilder()
          .WithUrl($"{config.ApiBaseUrl.TrimEnd('/')}/hubs/configuration", options =>
          {
            options.AccessTokenProvider = async () =>
                await tokenProvider.GetTokenAsync(stoppingToken);
          })
          .WithAutomaticReconnect(new InfiniteRetryPolicy(MaxRetryDelay))
          .Build();

      RegisterHandlers(hubConnection, stoppingToken);

      while (!stoppingToken.IsCancellationRequested)
      {
        await ConnectWithRetryAsync(hubConnection, stoppingToken);

        // Wait until the connection is closed for good (or service stops), then reconnect
        await closedSignal.Task.WaitAsync(stoppingToken);
        closedSignal = NewSignal();
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
            logger.LogInformation("Received configuration update.");
            await configReader.UpdateAsync(dto, stoppingToken);
            return GetCurrentConfiguration();
          });

      // Client result: the API calls InvokeAsync<ConfigurationDto>("GetConfiguration")
      connection.On<ConfigurationDto>(
          "GetConfiguration",
          () =>
          {
            logger.LogInformation("Configuration requested by API.");
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
        logger.LogDebug("Reconnected with ConnectionId {ConnectionId}", connectionId);
        return Task.CompletedTask;
      };

      connection.Closed += error =>
      {
        logger.LogDebug(error, "Configuration hub connection closed; restarting connect loop.");
        closedSignal.TrySetResult();
        return Task.CompletedTask;
      };
    }

    private void MarkDisconnected(Exception? error, int attempt)
    {
      if (!isDisconnected)
      {
        isDisconnected = true;
        logger.LogWarning(error,
          "Lost connection to configuration hub. Retrying in the background; further failures are suppressed.");
      }
      else
      {
        logger.LogDebug("Configuration hub connect attempt {Attempt} failed: {Message}", attempt, error?.Message);
      }
    }

    private void MarkConnected(int attempts)
    {
      if (isDisconnected)
      {
        isDisconnected = false;
        logger.LogInformation("Connection to configuration hub restored.");
      }
      else if (attempts == 0)
      {
        logger.LogInformation("Connected to configuration hub.");
      }
    }

    private ConfigurationDto GetCurrentConfiguration()
    {
      var current = configReader.Configuration;
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

      if (hubConnection is not null)
      {
        await hubConnection.DisposeAsync();
        hubConnection = null;
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
