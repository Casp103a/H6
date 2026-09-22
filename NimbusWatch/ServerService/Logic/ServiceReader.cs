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
    private readonly List<ServiceController> controllers;
    private readonly ILogger<ServiceReader> logger;

    public ServiceReader(IConfigReader configReader, ILogger<ServiceReader> logger)
    {
      this.configReader = configReader;
      this.logger = logger;

      controllers = new List<ServiceController>();
      SetupControllers();
    }

    private void SetupControllers()
    {
      controllers.Clear();
      foreach (var serviceInfo in configReader.Configuration.Services)
      {
        try
        {
          var controller = new ServiceController(serviceInfo.Name);
          controllers.Add(controller);
        }
        catch (InvalidOperationException ex)
        {
          logger.LogError(ex, $"Service '{serviceInfo.Name}' not found.");
        }
      }
    }

    public List<ServiceStatusesDto> LookAtStatuses()
    {
      var reports = new List<ServiceStatusesDto>();

      foreach (var controller in controllers)
      {
        try
        {
          if (controller.Status != ServiceControllerStatus.Running)
          {
            var report = new ServiceStatusesDto
            {
              Name = controller.DisplayName,
              Status = controller.Status.ToString()
            };
            reports.Add(report);
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
