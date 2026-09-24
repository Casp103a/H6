using ClassLibrary.Dtos;
using ServerService.Logic;
using System.Text.Json;

namespace ServerService
{
  public class Worker : BackgroundService
  {
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private readonly ILogger<Worker> logger;
    private readonly IHardwareReader hardwareReader;
    private readonly IConfigReader configReader;
    private readonly HttpClient httpClient;
    private readonly IServiceInfoFetcher serviceInfoFetcher;
    private readonly IServiceReader serviceReader;

    private DateTime lastReadTime;
    private int consecutiveFailures;
    private DateTime failingSince;

    public Worker(ILogger<Worker> logger, IHardwareReader hardwareReader, IConfigReader configReader, IHttpClientFactory factory, IServiceInfoFetcher serviceInfoFetcher, IServiceReader serviceReader)
    {
      this.logger = logger;
      this.hardwareReader = hardwareReader;
      this.configReader = configReader;
      this.serviceInfoFetcher = serviceInfoFetcher;
      this.serviceReader = serviceReader;
      httpClient = factory.CreateClient("ApiClient");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      logger.LogInformation("NimbusWatch service started at {Time}", DateTimeOffset.Now);

      try
      {
        while (!stoppingToken.IsCancellationRequested)
        {
          await ReadLogic(stoppingToken);
          await Task.Delay(1000, stoppingToken);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        // normal shutdown
      }
      finally
      {
        logger.LogInformation("NimbusWatch service stopping at {Time}", DateTimeOffset.Now);
      }
    }

    private async Task ReadLogic(CancellationToken cancellationToken)
    {
      var interval = TimeSpan.FromMilliseconds(configReader.Configuration.ReportFrequencyInMilliSec);

      // Back off exponentially while failing: interval * 2^failures, capped at MaxBackoff
      if (consecutiveFailures > 0)
      {
        var backoffMs = interval.TotalMilliseconds * Math.Pow(2, Math.Min(consecutiveFailures, 10));
        interval = TimeSpan.FromMilliseconds(Math.Min(backoffMs, MaxBackoff.TotalMilliseconds));
      }

      if (DateTime.Now - lastReadTime < interval)
        return;

      try
      {
        await ReportReadings(cancellationToken);
        OnSuccess();
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception ex)
      {
        OnFailure(ex);
      }
      finally
      {
        // Always advance, so a failure doesn't cause a retry every second
        lastReadTime = DateTime.Now;
      }
    }

    private void OnSuccess()
    {
      if (consecutiveFailures > 0)
      {
        logger.LogInformation(
          "Connection to API restored after {Failures} failed attempts (down since {Since}).",
          consecutiveFailures, failingSince);
      }
      consecutiveFailures = 0;
    }

    private void OnFailure(Exception ex)
    {
      consecutiveFailures++;

      if (consecutiveFailures == 1)
      {
        failingSince = DateTime.Now;
        // Only the first failure goes to the Event Log as an error
        logger.LogError(ex, "Failed to report data to API. Further failures will be suppressed until the connection is restored.");
      }
      else
      {
        logger.LogDebug("Report attempt {Attempt} failed: {Message}", consecutiveFailures, ex.Message);
      }
    }

    private async Task ReportReadings(CancellationToken cancellationToken)
    {
      var cpuUsage = hardwareReader.ReadCpuUsage();
      var memoryUsage = hardwareReader.ReadMemoryUsage();
      var diskUsage = hardwareReader.ReadDiskUsage();
      var companyId = configReader.Configuration.CompanyId;
      var networkUsage = hardwareReader.ReadNetworkUsage();

      var serviceDto = new CompanyHardwareDataDto
      {
        CpuUsages = (int)cpuUsage,
        CompanyId = companyId.ToString(),
        RamUsage = memoryUsage.useRam,
        RamTotal = memoryUsage.totalRam,
        DiskRemainingSpace = diskUsage.RemainingDisk,
        DiskTotalSpace = diskUsage.totalDisk,
        NetworkBytesSent = networkUsage.bytesSend,
        NetworkBytesReceived = networkUsage.bytesReceived,
        ServiceId = serviceInfoFetcher.ServiceId,
      };

      var json = JsonSerializer.Serialize(serviceDto, AppJsonSerializerContext.Default.CompanyHardwareDataDto);
      await PostAsync($"api/CompanyHardwareData/Add/{companyId}", json, "hardware data", cancellationToken);

      var statuses = serviceReader.LookAtStatuses();
      if (statuses.Count > 0)
      {
        var report = new ServiceReportDto
        {
          Statuses = statuses,
          ServerId = serviceInfoFetcher.ServiceId,
        };

        var reportJson = JsonSerializer.Serialize(report, AppJsonSerializerContext.Default.ServiceReportDto);
        await PostAsync($"api/ServiceStatus/Add/{companyId}", reportJson, "service statuses", cancellationToken);
      }
    }

    private async Task PostAsync(string url, string json, string what, CancellationToken cancellationToken)
    {
      using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
      using var response = await httpClient.PostAsync(url, content, cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        // Throw so it goes through the same suppression/backoff logic
        throw new HttpRequestException(
          $"Failed to report {what}: {(int)response.StatusCode} {response.StatusCode}. Body: {body}",
          null, response.StatusCode);
      }
    }
  }
}
