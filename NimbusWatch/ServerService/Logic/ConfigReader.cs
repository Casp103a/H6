using ClassLibrary.Configuration;
using ClassLibrary.Dtos.Configuration;
using ClassLibrary.Services;
using System.Text.Json;

namespace ServerService.Logic
{
  public interface IConfigReader
  {
    ServiceConfiguration Configuration { get; }
    event EventHandler<ServiceConfiguration>? ConfigurationChanged;
    Task UpdateAsync(ConfigurationDto dto, CancellationToken cancellationToken = default);
  }

  internal class ConfigReader : IConfigReader
  {
    private readonly string _configFileName = "service_config.config";
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<ConfigReader> _logger;

    private ServiceConfiguration _configuration;

    public ServiceConfiguration Configuration => _configuration;
    public event EventHandler<ServiceConfiguration>? ConfigurationChanged;

    public ConfigReader(ILogger<ConfigReader> logger)
    {
      _logger = logger;
      _configuration = ReadConfig();
    }

    public async Task UpdateAsync(ConfigurationDto dto, CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(dto);

      await _lock.WaitAsync(cancellationToken);
      try
      {
        var updated = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(_configuration, AppJsonSerializerContext.Default.ServiceConfiguration),
            AppJsonSerializerContext.Default.ServiceConfiguration)!;

        updated.ReportFrequencyInMilliSec = dto.ReportFrequency;
        updated.Services = dto.MonitoredServices ?? [];

        await SaveConfigAsync(updated, cancellationToken);
        Volatile.Write(ref _configuration, updated);
      }
      finally { _lock.Release(); }

      ConfigurationChanged?.Invoke(this, _configuration);
    }

    private ServiceConfiguration ReadConfig()
    {
      var configDir = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "NimbusWatch");

      var configFilePath = Path.Combine(configDir, _configFileName);

      if (!File.Exists(configFilePath))
      {
        var ex = new FileNotFoundException($"Configuration file not found: {configFilePath}");
        _logger.LogError(ex, "Configuration file not found.");
        throw ex;
      }

      try
      {
        var file = File.ReadAllText(configFilePath);
        var decryptedJson = new EncryptingService().DecryptForFile(file);

        return JsonSerializer.Deserialize(
                   decryptedJson,
                   AppJsonSerializerContext.Default.ServiceConfiguration)
               ?? new ServiceConfiguration();
      }
      catch (Exception ex)
      {
        var wrapped = new InvalidOperationException(
            $"Failed to read configuration from {configFilePath}",
            ex);

        _logger.LogError(wrapped, "An error occurred while reading the configuration.");
        throw wrapped;
      }
    }

    private async Task SaveConfigAsync(ServiceConfiguration configuration, CancellationToken cancellationToken)
    {
      var configDir = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "NimbusWatch");

      Directory.CreateDirectory(configDir);

      var configFilePath = Path.Combine(configDir, _configFileName);

      var json = JsonSerializer.Serialize(
          configuration,
          AppJsonSerializerContext.Default.ServiceConfiguration);

      var encryptedJson = new EncryptingService().EncryptForFile(json);

      await File.WriteAllTextAsync(configFilePath, encryptedJson, cancellationToken);

      _logger.LogInformation("Configuration updated and saved.");
    }
  }
}
