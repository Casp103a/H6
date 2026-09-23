using ClassLibrary.Configuration;
using ClassLibrary.Dtos;
using ClassLibrary.Dtos.Configuration;
using System.Text.Json.Serialization;

namespace ServerService.Logic
{
  [JsonSerializable(typeof(ServiceConfiguration))]
  [JsonSerializable(typeof(CompanyHardwareDataDto))]
  [JsonSerializable(typeof(ServiceTokenRequestDto))]
  [JsonSerializable(typeof(ServiceTokenResponseDto))]
  [JsonSerializable(typeof(ServiceInfoDto))]
  [JsonSerializable(typeof(ServiceStatusesDto))]
  [JsonSerializable(typeof(ServiceReportDto))]
  [JsonSerializable(typeof(ServerServicesInfo))]
  [JsonSerializable(typeof(CreateServiceMonitorDto))]
  [JsonSerializable(typeof(ConfigurationDto))]
  [JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
  public partial class AppJsonSerializerContext : JsonSerializerContext
  {
  }
}
