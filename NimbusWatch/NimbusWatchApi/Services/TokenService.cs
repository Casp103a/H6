using Microsoft.IdentityModel.Tokens;
using NimbusWatchApi.Managers;
using NimbusWatchApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NimbusWatchApi.Services
{
  public interface ITokenService
  {
    string CreateToken(UserModel user, out Guid id);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    ClaimsPrincipal ValidateToken(string token);
    ClaimsPrincipal ValidateExpiredAccessToken(string token);
    bool IsTokenExpired(string token);
  }

  public class TokenService : ITokenService
  {
    private readonly IConfiguration config;
    private readonly SymmetricSecurityKey key;
    private readonly IRoleManager roleManager;

    public TokenService(IConfiguration config, IRoleManager roleManager)
    {
      this.config = config;
      this.roleManager = roleManager;
      key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:SigningKey"]!));
    }

    public string CreateRefreshToken()
    {
      var randomNumber = new byte[64];
      using var numberGenerator = RandomNumberGenerator.Create();
      numberGenerator.GetBytes(randomNumber);
      return Convert.ToBase64String(randomNumber);
    }

    public string HashRefreshToken(string refreshToken)
    {
      var bytes = Encoding.UTF8.GetBytes(refreshToken);
      var hash = SHA256.HashData(bytes);
      return Convert.ToBase64String(hash);
    }

    public string CreateToken(UserModel user, out Guid id)
    {
      id = Guid.NewGuid();
      var role = roleManager.GetRole(user.RoleId).GetAwaiter().GetResult();

      if (role == null)
        return string.Empty;

      var claims = new List<Claim>
      {
        new Claim(JwtRegisteredClaimNames.GivenName, user.Email ?? string.Empty),
        new Claim(ClaimTypes.Name, user.Email ?? string.Empty),
        new Claim(ClaimTypes.Role, role.Name ?? string.Empty),
        new Claim(ClaimTypes.Sid, id.ToString())
      };

      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

      var descriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(15),
        SigningCredentials = creds,
        Issuer = config["JWT:Issuer"],
        Audience = config["JWT:Audience"]
      };

      var handler = new JwtSecurityTokenHandler();
      var token = handler.CreateToken(descriptor);
      return handler.WriteToken(token);
    }

    public ClaimsPrincipal ValidateToken(string token) => Validate(token, validateLifetime: true);

    public ClaimsPrincipal ValidateExpiredAccessToken(string token) => Validate(token, validateLifetime: false);

    private ClaimsPrincipal Validate(string token, bool validateLifetime)
    {
      var parameters = new TokenValidationParameters
      {
        IssuerSigningKey = key,
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidIssuer = config["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = config["JWT:Audience"],
        ValidateLifetime = validateLifetime,
        ClockSkew = TimeSpan.Zero
      };

      var handler = new JwtSecurityTokenHandler();
      var principal = handler.ValidateToken(token, parameters, out var validatedToken);

      if (validatedToken is not JwtSecurityToken jwt ||
          !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
      {
        throw new SecurityTokenException("Invalid token.");
      }

      return principal;
    }

    public bool IsTokenExpired(string token)
    {
      var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
      return jwtToken.ValidTo < DateTime.UtcNow;
    }
  }
}
