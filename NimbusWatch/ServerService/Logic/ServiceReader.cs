using ClassLibrary.Dtos;
using System.ServiceProcess;

namespace ServerService.Logic
{
  public interface IServiceReader
  {
    List<ServiceStatusesDto> LookAtStatuses();
  }

  internal class ServiceReader : IServiceReader
  {
    private readonly IConfigReader configReader;
    private readonly ILogger<ServiceReader> logger;
    private readonly object sync = new();
    private List<ServiceController> controllers = [];

    public ServiceReader(IConfigReader configReader, ILogger<ServiceReader> logger)
    {
      this.configReader = configReader;
      this.logger = logger;

      SetupControllers();
      configReader.ConfigurationChanged += (_, _) => SetupControllers();
    }

    private void SetupControllers()
    {
      var newControllers = new List<ServiceController>();
      foreach (var serviceInfo in configReader.Configuration.Services)
      {
        try
        {
          newControllers.Add(new ServiceController(serviceInfo.Name));
        }
        catch (InvalidOperationException ex)
        {
          logger.LogError(ex, "Service '{Name}' not found.", serviceInfo.Name);
        }
      }

      List<ServiceController> old;
      lock (sync)
      {
        old = controllers;
        controllers = newControllers;
      }

      foreach (var c in old) c.Dispose();
      logger.LogInformation("Monitored services reloaded ({Count}).", newControllers.Count);
    }

    public List<ServiceStatusesDto> LookAtStatuses()
    {
      List<ServiceController> snapshot;
      lock (sync) snapshot = controllers;

      var reports = new List<ServiceStatusesDto>();
      foreach (var controller in snapshot)
      {
        try
        {
          controller.Refresh(); // otherwise Status can be a stale cached value
          if (controller.Status != ServiceControllerStatus.Running)
          {
            reports.Add(new ServiceStatusesDto
            {
              Name = controller.DisplayName,
              Status = controller.Status.ToString()
            });
          }
        }
        catch (Exception)
        {
          continue;
        }
      }
      return reports;
    }
  }
}
