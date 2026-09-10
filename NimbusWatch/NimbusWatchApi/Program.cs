using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NimbusWatchApi.Database;
using NimbusWatchApi.Managers;
using NimbusWatchApi.Services;
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
      builder.Services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme =
        options.DefaultChallengeScheme =
        options.DefaultForbidScheme =
        options.DefaultScheme =
        options.DefaultSignInScheme =
        options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(option =>
      {
        option.TokenValidationParameters = new TokenValidationParameters
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

      builder.Services.AddCors(options =>
      {
        options.AddPolicy("AllowFrontend", policy =>
        {
          policy.WithOrigins("http://localhost:8000", "https://localhost:8000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
      });

      builder.Services.AddScoped<IUserManager, UserManager>();
      builder.Services.AddScoped<IRefreshTokenManager, RefreshTokenManager>();
      builder.Services.AddScoped<IPasswordService, PasswordService>();
      builder.Services.AddScoped<ITokenService, TokenService>();
      builder.Services.AddScoped<ICompanyManager, CompanyManager>();
      builder.Services.AddScoped<IRoleManager, RoleManager>();

      var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

      builder.Services.AddScoped<IDatabaseManager>(_ => new DatabaseManager(connectionString));

      var app = builder.Build();

      // Configure the HTTP request pipeline.
      if (app.Environment.IsDevelopment())
      {
        app.MapOpenApi();
      }

      app.UseHttpsRedirection();

      app.UseCors("AllowFrontend");

      app.UseAuthentication();
      app.UseAuthorization();

      app.MapControllers();

      app.Run();
    }
  }
}
