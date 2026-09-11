using ClassLibrary.Dtos;
using MySqlConnector;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Database.TableManagers
{
  public interface ICompanyTableManager
  {
    Task<CompanyModel?> AddCompany(CreateCompanyRequestDto request);
    bool CompanyExists(string companyId);
    bool DeleteCompany(string companyId);
    bool UpdateCompany(string companyId, CompanyModel updatedCompany);
    Task<CompanyModel?> GetCompany(string companyId);
  }

  public class CompanyTableManager : TableManager, ICompanyTableManager
  {
    public CompanyTableManager(string connectionString) : base(connectionString)
    {
    }

    public bool CompanyExists(string companyId)
    {
      throw new NotImplementedException();
    }

    public async Task<CompanyModel?> AddCompany(CreateCompanyRequestDto request)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      var id = Guid.NewGuid();

      cmd.CommandText = "INSERT INTO companys (Id, Name, CreatedAt) VALUES (@Id, @Name, @CreatedAt);";
      cmd.Parameters.AddWithValue("@Id", id.ToString());
      cmd.Parameters.AddWithValue("@Name", request.CompanyName);
      cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

      var result = await cmd.ExecuteNonQueryAsync();

      if (result > 0)
      {
        return new CompanyModel
        {
          Id = id,
          Name = request.CompanyName,
        };
      }
      else
      {
        return null;
      }

    }

    public bool DeleteCompany(string companyId)
    {
      throw new NotImplementedException();
    }

    public bool UpdateCompany(string companyId, CompanyModel updatedCompany)
    {
      throw new NotImplementedException();
    }

    public async Task<CompanyModel?> GetCompany(string companyId)
    {
      await using var connection = await OpenConnection();
      await using var cmd = connection.CreateCommand();

      cmd.CommandText = "SELECT Id, Name FROM companys WHERE Id = @Id;";
      cmd.Parameters.AddWithValue("@Id", companyId);

      await using var reader = await cmd.ExecuteReaderAsync();
      if (await reader.ReadAsync())
      {
        return MapCompany(reader);
      }
      else
      {
        return null;
      }
    }

    private CompanyModel MapCompany(MySqlDataReader reader)
    {
      return new CompanyModel
      {
        Id = reader.GetGuid("Id"),
        Name = reader.GetString("Name"),
      };
    }
  }
}
