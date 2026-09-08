
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace NimbusWatchApi
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.

      builder.Services.AddControllers();
      // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
      builder.Services.AddOpenApi();

      builder.Services.AddAuthorization();
      builder.Services.AddAuthentication(option =>
      {
        option.DefaultAuthenticateScheme =
        option.DefaultChallengeScheme =
        option.DefaultForbidScheme =
        option.DefaultScheme =
        option.DefaultSignInScheme =
        option.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(option =>
      {
        option.TokenValidationParameters = new TokenValidationParameters()
        {
          ValidateIssuer = true,
          ValidIssuer = builder.Configuration["JWT:Issuer"],
          ValidateAudience = false,
          ValidateIssuerSigningKey = true,
          ClockSkew = TimeSpan.Zero,
          IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"]!)),
          RoleClaimType = ClaimTypes.Role
        };

        option.Events = new JwtBearerEvents
        {
          OnMessageReceived = context =>
          {
            // SignalR browser clients often send token as query string: ?access_token=...
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrWhiteSpace(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
              context.Token = accessToken;
            }

            // existing cookie fallback
            if (string.IsNullOrWhiteSpace(context.Token) &&
                context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
            {
              context.Token = cookieToken;
            }

            return Task.CompletedTask;
          }
        };
      });

      var app = builder.Build();

      // Configure the HTTP request pipeline.
      if (app.Environment.IsDevelopment())
      {
        app.MapOpenApi();
      }

      app.UseHttpsRedirection();

      app.UseAuthorization();


      app.MapControllers();

      app.Run();
    }
  }
}
