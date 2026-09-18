using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface ICompanyHardwareDataTableManager
  {
    Task<bool> AddCompanyHardwareData(CompanyHardwareData data);
    Task<List<CompanyHardwareData>> GetCompanyHardwareDataByCompanyId(string companyId);
    Task<List<CompanyHardwareData>> GetCompanyHardwareDataByServiceId(string companyId, int serviceId);
  }

  public class CompanyHardwareDataTableManager : TableManager, ICompanyHardwareDataTableManager
  {
    public CompanyHardwareDataTableManager(string connectionString) : base(connectionString)
    {
    }

    public async Task<bool> AddCompanyHardwareData(CompanyHardwareData data)
    {
      var table = CompanyTableNames.HardwareData(data.CompanyId);

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
        INSERT INTO `{table}` (CompanyId, ServiceId, CpuUsages, DiskTotalSpace, DiskRemainingSpace, RamUsage, RamTotal, CreatedAt)
        VALUES (@CompanyId, @ServiceId, @CpuUsages, @DiskTotalSpace, @DiskRemainingSpace, @RamUsage, @RamTotal, @CreatedAt);
        """;

      cmd.Parameters.AddWithValue("@CompanyId", data.CompanyId);
      cmd.Parameters.AddWithValue("@ServiceId", data.ServiceId);
      cmd.Parameters.AddWithValue("@CpuUsages", data.CpuUsages);
      cmd.Parameters.AddWithValue("@DiskTotalSpace", data.DiskTotalSpace);
      cmd.Parameters.AddWithValue("@DiskRemainingSpace", data.DiskRemainingSpace);
      cmd.Parameters.AddWithValue("@RamUsage", data.RamUsage);
      cmd.Parameters.AddWithValue("@RamTotal", data.RamTotal);
      cmd.Parameters.AddWithValue("@CreatedAt", data.CreatedAt);

      return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<CompanyHardwareData>> GetCompanyHardwareDataByCompanyId(string companyId)
    {
      var table = CompanyTableNames.HardwareData(companyId);
      var result = new List<CompanyHardwareData>();

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
        SELECT Id, CompanyId, ServiceId, CpuUsages, DiskTotalSpace, DiskRemainingSpace, RamUsage, RamTotal, CreatedAt
        FROM `{table}`;
        """;

      await using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
        result.Add(MapModel(reader));

      return result;
    }

    public async Task<List<CompanyHardwareData>> GetCompanyHardwareDataByServiceId(string companyId, int serviceId)
    {
      var table = CompanyTableNames.HardwareData(companyId);
      var result = new List<CompanyHardwareData>();

      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = $"""
        SELECT Id, CompanyId, ServiceId, CpuUsages, DiskTotalSpace, DiskRemainingSpace, RamUsage, RamTotal, CreatedAt
        FROM `{table}`
        WHERE ServiceId = @ServiceId;
        """;

      cmd.Parameters.AddWithValue("@ServiceId", serviceId);

      await using var reader = await cmd.ExecuteReaderAsync();

      while (await reader.ReadAsync())
        result.Add(MapModel(reader));

      return result;
    }

    private static CompanyHardwareData MapModel(MySqlDataReader reader)
    {
      return new CompanyHardwareData
      {
        Id = reader.GetInt32("Id"),
        CompanyId = reader.GetGuid("CompanyId").ToString(),
        ServiceId = reader.GetInt32("ServiceId"),
        CpuUsages = reader.GetInt32("CpuUsages"),
        DiskTotalSpace = reader.GetDouble("DiskTotalSpace"),
        DiskRemainingSpace = reader.GetDouble("DiskRemainingSpace"),
        RamUsage = reader.GetDouble("RamUsage"),
        RamTotal = reader.GetDouble("RamTotal"),
        CreatedAt = reader.GetDateTime("CreatedAt"),
      };
    }
  }
}
