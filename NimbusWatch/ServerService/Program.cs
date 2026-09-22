using ClassLibrary.Services;
using Microsoft.Extensions.Logging.EventLog;
using ServerService.Logic;

namespace ServerService
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = Host.CreateApplicationBuilder(args);

      // Run as a Windows Service (no-op when started from console/debugger)
      builder.Services.AddWindowsService(options =>
      {
        options.ServiceName = "NimbusWatch";
      });

      // Write logs to the Windows Event Log under the "NimbusWatch" source
      builder.Services.Configure<EventLogSettings>(settings =>
      {
        settings.SourceName = "NimbusWatch";
        settings.LogName = "Application";
      });

      builder.Services.AddSingleton<IConfigReader, ConfigReader>();
      builder.Services.AddSingleton<IHardwareReader, HardwareReader>();
      builder.Services.AddSingleton<ITokenProvider, TokenProvider>();
      builder.Services.AddSingleton<IServiceInfoFetcher, ServiceInfoFetcher>();
      builder.Services.AddSingleton<IServiceReader, ServiceReader>();
      builder.Services.AddTransient<AuthHeaderHandler>();
      builder.Services.AddHostedService<Worker>();

      builder.Services.AddHttpClient("TokenClient")
        .ConfigureHttpClient((sp, client) =>
        {
          var config = sp.GetRequiredService<IConfigReader>().Configuration;
          client.BaseAddress = new Uri(config.ApiBaseUrl);
        });

      builder.Services.AddHttpClient("ApiClient")
        .ConfigureHttpClient((sp, client) =>
        {
          var config = sp.GetRequiredService<IConfigReader>().Configuration;
          client.BaseAddress = new Uri(config.ApiBaseUrl);
        })
        .AddHttpMessageHandler<AuthHeaderHandler>();

      using var stream = typeof(Program).Assembly
        .GetManifestResourceStream("ServerService.appsettings.json")!;
      builder.Configuration.AddJsonStream(stream);

      var host = builder.Build();
      host.Run();
    }
  }
}
