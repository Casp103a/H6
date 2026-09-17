using Microsoft.Extensions.DependencyInjection;

namespace NimbusWatchApi.Database
{
  public sealed class DatabaseInitializerHostedService : IHostedService
  {
    private readonly IServiceProvider _services;

    public DatabaseInitializerHostedService(IServiceProvider services) => _services = services;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      using var scope = _services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<IDatabaseManager>();
      await db.SetupDatabase(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
  }
}
