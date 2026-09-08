using ClassLibrary.Dtos;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Database;
using NimbusWatchApi.Managers;
using NimbusWatchApi.Models;
using NimbusWatchApi.Services;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class LoginController : ControllerBase
  {
    private const string AccessTokenCookieName = "access_token";
    private const string RefreshTokenCookieName = "refresh_token";

    private readonly IUserManager userManager;
    private readonly ITokenService tokenService;
    private readonly IRefreshTokenManager refreshTokenManager;
    private readonly IDatabaseManager databaseManager;

    public LoginController(
      IUserManager userManager,
      ITokenService tokenService,
      IRefreshTokenManager refreshTokenManager,
      IDatabaseManager databaseManager)
    {
      this.userManager = userManager;
      this.tokenService = tokenService;
      this.refreshTokenManager = refreshTokenManager;
      this.databaseManager = databaseManager;
    }

    // Cookie-based login (website)
    [HttpPost("login")]
    public async Task<ActionResult> LoginWeb([FromBody] LoginRequestDto request)
    {
      if (!ModelState.IsValid) return BadRequest(ModelState);

      var user = await ValidateUser(request);
      if (user == null) return Unauthorized("Email not found and/or password incorrect");

      var accessToken = tokenService.CreateToken(user, out Guid accessTokenId);
      var rawRefreshToken = await CreateAndStoreRefreshToken(user, accessTokenId);
      if (rawRefreshToken == null) return StatusCode(500, "Failed while saving");

      SetAccessTokenCookie(accessToken);
      SetRefreshTokenCookie(rawRefreshToken);

      // Do NOT return refresh token for browser flow
      return Ok(new LoginTokenDto { Token = accessToken });
    }

    // Non-cookie login (mobile/desktop/CLI)
    [HttpPost("app")]
    public async Task<ActionResult> LoginApp([FromBody] LoginRequestDto request)
    {
      if (!ModelState.IsValid) return BadRequest(ModelState);

      var user = await ValidateUser(request);
      if (user == null) return Unauthorized("Email not found and/or password incorrect");

      var accessToken = tokenService.CreateToken(user, out Guid accessTokenId);
      var rawRefreshToken = await CreateAndStoreRefreshToken(user, accessTokenId);
      if (rawRefreshToken == null) return StatusCode(500, "Failed while saving");

      // Return both tokens for non-browser clients
      return Ok(new LoginTokenDto
      {
        Token = accessToken,
        RefreshToken = rawRefreshToken
      });
    }

    private async Task<UserModel?> ValidateUser(LoginRequestDto request)
    {
      var result = await userManager.CheckPasswordSignInAsync(request, request.Password);
      if (!result) return null;

      return await databaseManager.Users.GetUser(request.Email);
    }

    private async Task<string?> CreateAndStoreRefreshToken(UserModel user, Guid accessTokenId)
    {
      var rawRefreshToken = tokenService.CreateRefreshToken();
      var refreshTokenHash = tokenService.HashRefreshToken(rawRefreshToken);

      var refreshToken = new RefreshTokens
      {
        ID = accessTokenId,
        RefreshToken = refreshTokenHash,
        RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
        RefreshTokenIsUsed = false,
        UserId = user.Id
      };

      var refreshTokenSaved = await refreshTokenManager.Add(refreshToken);
      if (!refreshTokenSaved) return null;

      return rawRefreshToken;
    }

    private void SetAccessTokenCookie(string accessToken)
    {
      Response.Cookies.Append(AccessTokenCookieName, accessToken, new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = DateTimeOffset.UtcNow.AddMinutes(15),
        Path = "/"
      });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
      Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = DateTimeOffset.UtcNow.AddDays(7),
        Path = "/api/Token"
      });
    }
  }
}
