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

    public Worker(ILogger<Worker> logger, IHardwareReader hardwareReader, IConfigReader configReader, IHttpClientFactory factory, IServiceInfoFetcher serviceInfoFetcher)
    {
      this.logger = logger;
      this.hardwareReader = hardwareReader;
      this.configReader = configReader;
      this.serviceInfoFetcher = serviceInfoFetcher;
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
            logger.LogError(ex, "Failed to collect or report hardware readings");
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

      return;
    }
  }
}
