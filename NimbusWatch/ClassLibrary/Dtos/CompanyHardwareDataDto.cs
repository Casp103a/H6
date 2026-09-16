namespace ClassLibrary.Dtos
{
  public class CompanyHardwareDataDto
  {
    public int Id { get; set; }
    public string CompanyId { get; set; }
    public int ServiceId { get; set; }
    public int CpuUsages { get; set; }
    public double DiskTotalSpace { get; set; }
    public double DiskRemainingSpace { get; set; }
    public double RamUsage { get; set; }
    public double RamTotal { get; set; }
  }
}
