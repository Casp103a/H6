using ServerService.Logic;

namespace ServerService
{
  public class Worker : BackgroundService
  {
    private readonly ILogger<Worker> logger;
    private readonly IHardwareReader hardwareReader;
    private readonly IConfigReader configReader;
    private readonly HttpClient httpClient;

    public Worker(ILogger<Worker> logger, IHardwareReader hardwareReader, IConfigReader configReader, IHttpClientFactory factory)
    {
      this.logger = logger;
      this.hardwareReader = hardwareReader;
      this.configReader = configReader;
      this.httpClient = factory.CreateClient("ApiClient");
    }

    private DateTime lastReadTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        if (logger.IsEnabled(LogLevel.Information))
        {
          logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }
        await Task.Delay(1000, stoppingToken);
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

    private Task ReportReadings()
    {
      var cpuUsage = hardwareReader.ReadCpuUsage();
      var cpuTemperature = hardwareReader.ReadCpuTemperature();
      var memoryUsage = hardwareReader.ReadMemoryUsage();
      var diskUsage = hardwareReader.ReadDiskUsage();
      var serverName = configReader.Configuration.ServerName;
      var companyId = configReader.Configuration.CompanyId;

      //httpClient.PostAsync();

      return Task.CompletedTask;
    }
  }
}
