using ClassLibrary.Configuration;
using ClassLibrary.Services;
using System.Text.Json;

namespace ServerService.Logic
{
  public interface IConfigReader
  {
    public ServiceConfiguration Configuration { get; }
  }

  internal class ConfigReader : IConfigReader
  {
    private readonly string configFilePath = "service_config.json";

    public ServiceConfiguration Configuration { get; }
    private readonly ILogger<ConfigReader> logger;

    public ConfigReader(ILogger<ConfigReader> logger)
    {
      Configuration = ReadConfig();
      this.logger = logger;
    }

    private ServiceConfiguration ReadConfig()
    {
      // Use ProgramData for service configuration
      var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NimbusWatch");
      var configFilePath = Path.Combine(configDir, "service_config.json");

      if (!File.Exists(configFilePath))
      {
        var sad = new FileNotFoundException($"Configuration file not found: {configFilePath}");
        logger.LogError(sad, "Configuration file not found.");
        throw sad;
      }

      try
      {
        var file = File.ReadAllText(configFilePath);

        var decryptedJson = new EncryptingService().DecryptForFile(file);

        var config = JsonSerializer.Deserialize<ServiceConfiguration>(decryptedJson, AppJsonSerializerContext.Default.ServiceConfiguration);

        return config ?? new ServiceConfiguration();
      }
      catch (Exception ex)
      {
        var exg = new InvalidOperationException($"Failed to read configuration from {configFilePath}", ex);
        logger.LogError(exg, "An error occurred while reading the configuration.");
        throw exg;
      }
    }
  }
}
