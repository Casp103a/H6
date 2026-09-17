using ClassLibrary.Dtos;
using NimbusWatchApi.Database;
using NimbusWatchApi.Models;

namespace NimbusWatchApi.Managers
{
  public interface ICompanyHardwareDataManager
  {
    Task<bool> AddAsync(CompanyHardwareDataDto data);
    //Task<bool> UpdateAsync(CompanyHardwareData data);
    //Task<bool> DeleteAsync(int id);
    //Task<CompanyHardwareData?> GetByIdAsync(int id);
    Task<List<CompanyHardwareData>> GetAllByCompanyIdAsync(string companyId);
    Task<List<CompanyHardwareData>> GetAllByServiceIdAsync(string companyId, int serviceId);
  }

  public class CompanyHardwareDataManager : ICompanyHardwareDataManager
  {
    private readonly IDatabaseManager databaseManager;

    public CompanyHardwareDataManager(IDatabaseManager databaseManager)
    {
      this.databaseManager = databaseManager;
    }

    public async Task<bool> AddAsync(CompanyHardwareDataDto data)
    {
      try
      {
        var companyHardwareData = new CompanyHardwareData
        {
          CompanyId = data.CompanyId,
          ServiceId = data.ServiceId,
          CpuUsages = data.CpuUsages,
          DiskTotalSpace = data.DiskTotalSpace,
          DiskRemainingSpace = data.DiskRemainingSpace,
          RamUsage = data.RamUsage,
          RamTotal = data.RamTotal
        };

        var result = await databaseManager.CompanyHardwareData.AddCompanyHardwareData(companyHardwareData);
        return result;
      }
      catch (Exception)
      {
        return false;
      }
    }

    //public Task<bool> DeleteAsync(int id)
    //{
    //  throw new NotImplementedException();
    //}

    public async Task<List<CompanyHardwareData>> GetAllByCompanyIdAsync(string companyId)
    {
      try
      {
        var result = await databaseManager.CompanyHardwareData.GetCompanyHardwareDataByCompanyId(companyId);
        return result;
      }
      catch (Exception)
      {

        return new List<CompanyHardwareData>();
      }
    }

    public async Task<List<CompanyHardwareData>> GetAllByServiceIdAsync(string companyId, int serviceId)
    {
      try
      {
        var result = await databaseManager.CompanyHardwareData.GetCompanyHardwareDataByServiceId(companyId, serviceId);
        return result;
      }
      catch (Exception)
      {
        return new List<CompanyHardwareData>();
      }
    }

    //public Task<CompanyHardwareData?> GetByIdAsync(int id)
    //{
    //  throw new NotImplementedException();
    //}

    //public Task<bool> UpdateAsync(CompanyHardwareData data)
    //{
    //  throw new NotImplementedException();
    //}
  }
}
