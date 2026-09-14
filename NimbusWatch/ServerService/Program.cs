using ClassLibrary.Services;
using ServerService.Logic;

namespace ServerService
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = Host.CreateApplicationBuilder(args);

      builder.Services.AddSingleton<IConfigReader, ConfigReader>();
      builder.Services.AddScoped<IHardwareReader, HardwareReader>();
      builder.Services.AddSingleton<ITokenProvider, TokenProvider>();
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

      var host = builder.Build();
      host.Run();
    }
  }
}
