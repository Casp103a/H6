using ClassLibrary.Configuration;
using System.Text.Json;

namespace ServerService.Logic
{
  public interface IConfigReader
  {
    public ServiceConfiguration Configuration { get; }
  }

  internal class ConfigReader : IConfigReader
  {
    private readonly string configFilePath = "config.json";

    public ServiceConfiguration Configuration { get; }

    public ConfigReader()
    {
      Configuration = ReadConfig();
    }

    private ServiceConfiguration ReadConfig()
    {
      if (!File.Exists(configFilePath))
      {
        throw new FileNotFoundException($"Configuration file not found: {configFilePath}");
      }

      var file = File.ReadAllText(configFilePath);

      var config = JsonSerializer.Deserialize<ServiceConfiguration>(file);

      return config ?? new ServiceConfiguration();
    }
  }
}
