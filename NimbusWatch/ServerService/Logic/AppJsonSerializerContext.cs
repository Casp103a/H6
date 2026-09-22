using ClassLibrary.Configuration;
using ClassLibrary.Dtos;
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
  [JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
  public partial class AppJsonSerializerContext : JsonSerializerContext
  {
  }
}
