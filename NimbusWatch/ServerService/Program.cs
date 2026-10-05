using ClassLibrary.Services;
using Microsoft.Extensions.Logging.EventLog;
using ServerService.Logic;
using System.Diagnostics;

namespace ServerService
{
  public class Program
  {
    public static int Main(string[] args)
    {
      AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        WriteFatal("Unhandled exception", e.ExceptionObject as Exception);
      TaskScheduler.UnobservedTaskException += (_, e) =>
      {
        WriteFatal("Unobserved task exception", e.Exception);
        e.SetObserved();
      };

      try
      {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddWindowsService(o => o.ServiceName = "NimbusWatch");
        builder.Services.Configure<EventLogSettings>(s =>
        {
          s.SourceName = "NimbusWatch";
          s.LogName = "Application";
        });

        // Don't let one failing BackgroundService take down the other
        builder.Services.Configure<HostOptions>(o =>
          o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

        builder.Services.AddSingleton<IConfigReader, ConfigReader>();
        builder.Services.AddSingleton<IHardwareReader, HardwareReader>();
        builder.Services.AddSingleton<ITokenProvider, TokenProvider>();
        builder.Services.AddSingleton<IServiceInfoFetcher, ServiceInfoFetcher>();
        builder.Services.AddSingleton<IServiceReader, ServiceReader>();
        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services.AddHostedService<Worker>();
        builder.Services.AddHostedService<ConfigurationSyncService>();

        builder.Services.AddHttpClient("TokenClient")
          .ConfigureHttpClient((sp, client) => client.BaseAddress = GetBaseUri(sp));

        builder.Services.AddHttpClient("ApiClient")
          .ConfigureHttpClient((sp, client) => client.BaseAddress = GetBaseUri(sp))
          .AddHttpMessageHandler<AuthHeaderHandler>();

        using var stream = typeof(Program).Assembly
          .GetManifestResourceStream("ServerService.appsettings.json")
          ?? throw new InvalidOperationException("Embedded appsettings.json not found.");
        builder.Configuration.AddJsonStream(stream);

        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

        builder.Build().Run();
        return 0;
      }
      catch (Exception ex)
      {
        WriteFatal("NimbusWatch failed to start", ex);
        return 1; // non-zero exit lets Windows Service recovery restart it
      }
    }

    private static Uri GetBaseUri(IServiceProvider sp)
    {
      var url = sp.GetRequiredService<IConfigReader>().Configuration.ApiBaseUrl;
      if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        throw new InvalidOperationException($"Invalid ApiBaseUrl in configuration: '{url}'.");
      return uri;
    }

    private static void WriteFatal(string message, Exception? ex)
    {
      try
      {
        EventLog.WriteEntry("NimbusWatch", $"{message}: {ex}", EventLogEntryType.Error);
      }
      catch
      {
        Console.Error.WriteLine($"{message}: {ex}");
      }
    }
  }
}
