using ClassLibrary.Dtos;
using ServerService.Logic;
using System.Text.Json;

namespace ServerService
{
  public class Worker : BackgroundService
  {
    private readonly ILogger<Worker> logger;
    private readonly IHardwareReader hardwareReader;
    private readonly IConfigReader configReader;
    private readonly HttpClient httpClient;
    private readonly IServiceInfoFetcher serviceInfoFetcher;
    private readonly IServiceReader serviceReader;

    public Worker(ILogger<Worker> logger, IHardwareReader hardwareReader, IConfigReader configReader, IHttpClientFactory factory, IServiceInfoFetcher serviceInfoFetcher, IServiceReader serviceReader)
    {
      this.logger = logger;
      this.hardwareReader = hardwareReader;
      this.configReader = configReader;
      this.serviceInfoFetcher = serviceInfoFetcher;
      this.serviceReader = serviceReader;
      httpClient = factory.CreateClient("ApiClient");
    }

    private DateTime lastReadTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      logger.LogInformation("NimbusWatch service started at {time}", DateTimeOffset.Now);

      try
      {
        while (!stoppingToken.IsCancellationRequested)
        {
          try
          {
            await ReadLogic(stoppingToken);
          }
          catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
          {
            break;
          }
          catch (Exception ex)
          {
            logger.LogError(ex, $"Failed to collect or report hardware readings inner ex {ex.InnerException}");
          }

          await Task.Delay(1000, stoppingToken);
        }
      }
      catch (OperationCanceledException)
      {
        // normal shutdown
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "NimbusWatch service failed unexpectedly");
        throw;
      }
      finally
      {
        logger.LogInformation("NimbusWatch service stopping at {time}", DateTimeOffset.Now);
      }
    }

    protected async Task ReadLogic(CancellationToken cancellationToken)
    {
      var reportFrequency = configReader.Configuration.ReportFrequencyInMilliSec;

      if (DateTime.Now - lastReadTime >= TimeSpan.FromMilliseconds(reportFrequency))
      {
        await ReportReadings();
        lastReadTime = DateTime.Now;
      }

    }

    private async Task ReportReadings()
    {
      var cpuUsage = hardwareReader.ReadCpuUsage();
      var memoryUsage = hardwareReader.ReadMemoryUsage();
      var diskUsage = hardwareReader.ReadDiskUsage();
      var serverName = configReader.Configuration.ServerName;
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

      var response = await httpClient.PostAsync($"api/CompanyHardwareData/Add/{companyId}", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

      if (!response.IsSuccessStatusCode)
      {
        logger.LogError("Failed to report hardware data: {statusCode}", response.StatusCode);
      }

      var statuses = serviceReader.LookAtStatuses();

      if (statuses.Count > 0)
      {
        var report = new ServiceReportDto
        {
          Statuses = statuses,
          ServerId = serviceInfoFetcher.ServiceId,
        };

        var reportJson = JsonSerializer.Serialize(report, AppJsonSerializerContext.Default.ServiceReportDto);
        var reportResponse = await httpClient.PostAsync($"api/ServiceStatus/Add/{configReader.Configuration.CompanyId}", new StringContent(reportJson, System.Text.Encoding.UTF8, "application/json"));

        if (!reportResponse.IsSuccessStatusCode)
        {
          logger.LogError("Failed to report service statuses: {statusCode}", reportResponse.StatusCode);
        }
      }

      return;
    }
  }
}
