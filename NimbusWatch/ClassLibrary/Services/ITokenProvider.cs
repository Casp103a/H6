namespace ClassLibrary.Services
{
  public interface ITokenProvider
  {
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
  }
}
