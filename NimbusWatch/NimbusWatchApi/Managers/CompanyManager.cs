using ClassLibrary.Dtos;
using NimbusWatchApi.Database;

namespace NimbusWatchApi.Managers
{
  public interface ICompanyManager
  {
    Task<CompanyDto?> CreateCompany(CreateCompanyRequestDto request);
    Task<CompanyDto?> GetCompany(string companyId);
  }

  public class CompanyManager : ICompanyManager
  {
    private readonly IDatabaseManager databaseManager;

    public CompanyManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<CompanyDto?> CreateCompany(CreateCompanyRequestDto request)
    {
      try
      {
        var result = await databaseManager.Companys.AddCompany(request);

        if (result == null)
        {
          return null;
        }

        await databaseManager.EnsureCompanyTables(result.Id.ToString());

        var companyDto = new CompanyDto
        {
          Id = result.Id,
          Name = result.Name
        };

        return companyDto;
      }
      catch (Exception)
      {
        return null;
      }
    }

    public async Task<CompanyDto?> GetCompany(string companyId)
    {
      try
      {
        var result = await databaseManager.Companys.GetCompany(companyId);

        if (result == null)
          return null;

        var companyDto = new CompanyDto
        {
          Id = result.Id,
          Name = result.Name
        };

        return companyDto;
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}
