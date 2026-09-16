using ClassLibrary.Dtos;
using ClassLibrary.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;
using NimbusWatchApi.Services;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ServiceAuthController : ControllerBase
  {
    private readonly IServiceClientManager serviceClientManager;
    private readonly IClientSecretService clientSecretService;
    private readonly ITokenService tokenService;
    private readonly IConfiguration configuration;

    public ServiceAuthController(
      IServiceClientManager serviceClientManager,
      IClientSecretService clientSecretService,
      ITokenService tokenService,
      IConfiguration configuration)
    {
      this.serviceClientManager = serviceClientManager;
      this.clientSecretService = clientSecretService;
      this.tokenService = tokenService;
      this.configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] ServiceTokenRequestDto request)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (!string.Equals(request.GrantType, "client_credentials", StringComparison.Ordinal))
        return BadRequest("Unsupported grant_type.");

      var client = await serviceClientManager.GetByClientIdAsync(request.ClientId);
      if (client == null || !client.IsActive)
        return Unauthorized();

      if (!clientSecretService.VerifySecret(request.ClientSecret, client.ClientSecretHash))
        return Unauthorized();

      var accessToken = tokenService.CreateServiceToken(client.ClientId, client.RoleId);
      var lifetimeMinutes = configuration.GetValue<int?>("ServiceAuth:AccessTokenLifetimeMinutes") ?? 5;

      return Ok(new ServiceTokenResponseDto
      {
        AccessToken = accessToken,
        TokenType = "Bearer",
        ExpiresIn = lifetimeMinutes * 60
      });
    }
  }
}
