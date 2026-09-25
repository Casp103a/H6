using ClassLibrary.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NimbusWatchApi.Managers;
using NimbusWatchApi.Models;
using NimbusWatchApi.Services;
using System.Security.Claims;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TokenController : ControllerBase
  {
    private const string AccessTokenCookieName = "access_token";
    private const string RefreshTokenCookieName = "refresh_token";

    private readonly IUserManager userManager;
    private readonly ITokenService tokenService;
    private readonly IRefreshTokenManager refreshTokenManager;

    public TokenController(IUserManager userManager, ITokenService tokenService, IRefreshTokenManager refreshTokenManager)
    {
      this.userManager = userManager;
      this.tokenService = tokenService;
      this.refreshTokenManager = refreshTokenManager;
    }

    [HttpPost("RefreshToken")]
    public Task<IActionResult> RefreshToken() => RefreshCore(null);

    [HttpPost("RefreshTokenWithToken")]
    public Task<IActionResult> RefreshTokenWithToken(LoginTokenDto tokendto) => RefreshCore(tokendto);

    private async Task<IActionResult> RefreshCore(LoginTokenDto? dto)
    {
      if (!ModelState.IsValid) return BadRequest(ModelState);

      var accessToken = GetAccessTokenFromRequest(dto);
      var rawRefreshToken = GetRefreshTokenFromRequest(dto);

      if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(rawRefreshToken))
        return Unauthorized("Access token or refresh token is missing.");

      ClaimsPrincipal principal;
      try
      {
        principal = tokenService.ValidateExpiredAccessToken(accessToken);
      }
      catch (SecurityTokenException)
      {
        return Unauthorized();
      }

      var username = principal.Identity?.Name;
      var sid = principal.FindFirst(ClaimTypes.Sid)?.Value;

      if (string.IsNullOrWhiteSpace(username) || !Guid.TryParse(sid, out var usedRefreshTokenId))
        return Unauthorized();

      var user = await userManager.GetUser(username);
      if (user == null)
        return Unauthorized();

      var incomingHash = tokenService.HashRefreshToken(rawRefreshToken);

      var newAccessToken = tokenService.CreateToken(user, out Guid newRefreshTokenId);
      var newRawRefreshToken = tokenService.CreateRefreshToken();
      var newRefreshTokenHash = tokenService.HashRefreshToken(newRawRefreshToken);

      var newRefreshToken = new RefreshTokens
      {
        ID = newRefreshTokenId,
        RefreshToken = newRefreshTokenHash,
        RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
        RefreshTokenIsUsed = false,
        UserId = user.Id
      };

      var rotated = await refreshTokenManager.RotateRefreshToken(
        usedRefreshTokenId,
        user,
        incomingHash,
        newRefreshToken);

      if (!rotated)
        return Unauthorized();

      SetAccessTokenCookie(newAccessToken);
      SetRefreshTokenCookie(newRawRefreshToken);

      return Ok(new LoginTokenDto { Token = newAccessToken });
    }

    [HttpPost("ValidateToken")]
    public async Task<IActionResult> ValidateToken()
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }

      var accessToken = GetAccessTokenFromRequest(null);
      if (string.IsNullOrWhiteSpace(accessToken))
      {
        return Unauthorized("Access token is missing.");
      }

      return await ValidateSession(accessToken);
    }

    [HttpPost("ValidateTokenWithToken")]
    public async Task<IActionResult> ValidateTokenWithToken(LoginTokenDto tokendto)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }

      var accessToken = GetAccessTokenFromRequest(tokendto);
      if (string.IsNullOrWhiteSpace(accessToken))
      {
        return Unauthorized("Access token is missing.");
      }

      return await ValidateSession(accessToken);
    }

    private async Task<IActionResult> ValidateSession(string accessToken)
    {
      var isValid = await TryValidateSession(accessToken);
      return isValid ? Ok() : Unauthorized();
    }

    private async Task<bool> TryValidateSession(string accessToken)
    {
      try
      {
        var principal = tokenService.ValidateExpiredAccessToken(accessToken);

        var username = principal.Identity?.Name;
        var sid = principal.FindFirst(ClaimTypes.Sid)?.Value;

        if (string.IsNullOrWhiteSpace(username) || !Guid.TryParse(sid, out var refreshTokenId))
        {
          return false;
        }

        var user = await userManager.GetUser(username);
        if (user == null)
        {
          return false;
        }

        var refreshToken = await refreshTokenManager.GetRefreshToken(refreshTokenId, user);
        if (refreshToken == null)
        {
          return false;
        }

        return !refreshToken.RefreshTokenIsUsed
            && refreshToken.RefreshTokenExpiry > DateTime.UtcNow;
      }
      catch (SecurityTokenException)
      {
        return false;
      }
    }

    [HttpPost("IsExpired")]
    public IActionResult IsTokenExpired()
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }

      var accessToken = GetAccessTokenFromRequest(null);
      if (string.IsNullOrWhiteSpace(accessToken))
      {
        return Unauthorized("Access token is missing.");
      }

      var isExpired = tokenService.IsTokenExpired(accessToken);
      return Ok(isExpired);
    }

    [HttpPost("IsExpiredWithToken")]
    public IActionResult IsTokenExpiredWithToken(LoginTokenDto tokendto)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }

      var accessToken = GetAccessTokenFromRequest(tokendto);
      if (string.IsNullOrWhiteSpace(accessToken))
      {
        return Unauthorized("Access token is missing.");
      }

      var isExpired = tokenService.IsTokenExpired(accessToken);
      return Ok(isExpired);
    }

    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }

      await LogoutFromToken(null);

      ClearAuthCookies();
      return Ok("Logged out.");
    }

    [HttpPost("LogoutApp")]
    public async Task<IActionResult> LogoutApp(LoginTokenDto tokenDto)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }

      await LogoutFromToken(tokenDto);

      return Ok("Logged out.");
    }

    private async Task LogoutFromToken(LoginTokenDto? tokenDto)
    {
      var accessToken = GetAccessTokenFromRequest(tokenDto);

      if (string.IsNullOrWhiteSpace(accessToken))
      {
        return;
      }

      try
      {
        var principal = tokenService.ValidateExpiredAccessToken(accessToken);
        var username = principal.Identity?.Name;
        var sid = principal.FindFirst(ClaimTypes.Sid)?.Value;

        if (string.IsNullOrWhiteSpace(username) || !Guid.TryParse(sid, out var refreshTokenId))
        {
          return;
        }

        var user = await userManager.GetUser(username);
        if (user == null)
        {
          return;
        }

        await refreshTokenManager.Update(refreshTokenId, true);
      }
      catch (SecurityTokenException)
      {
        return;
      }
    }

    private async Task<UserModel?> TryGetUserFromAccessToken(string accessToken)
    {
      try
      {
        var principal = tokenService.ValidateToken(accessToken);
        var username = principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username)) return null;

        return await userManager.GetUser(username);
      }
      catch (SecurityTokenExpiredException)
      {
        try
        {
          var principal = tokenService.ValidateExpiredAccessToken(accessToken);
          var username = principal.Identity?.Name;
          if (string.IsNullOrWhiteSpace(username)) return null;

          return await userManager.GetUser(username);
        }
        catch (SecurityTokenException)
        {
          return null;
        }
      }
      catch (SecurityTokenException)
      {
        return null;
      }
    }

    private void ClearAuthCookies()
    {
      // access token cookie path
      Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions
      {
        Path = "/",
        Secure = true,
        HttpOnly = true,
        SameSite = SameSiteMode.None
      });

      // refresh token cookie path
      Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
      {
        Path = "/api/Token",
        Secure = true,
        HttpOnly = true,
        SameSite = SameSiteMode.None
      });

      // optional compatibility delete in case old path/domain was used
      Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
      {
        Path = "/"
      });
    }

    private async Task<IActionResult> SaveResfreshToken(UserModel user, RefreshTokens usedRefreshToken)
    {
      var accessToken = tokenService.CreateToken(user, out Guid newAccessTokenId);

      var newRawRefreshToken = tokenService.CreateRefreshToken();
      var newRefreshHash = tokenService.HashRefreshToken(newRawRefreshToken);

      var newRefreshToken = new RefreshTokens
      {
        ID = newAccessTokenId,
        RefreshToken = newRefreshHash,
        RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
        RefreshTokenIsUsed = false,
        UserId = user.Id
      };

      var saved = await refreshTokenManager.Add(newRefreshToken);
      if (saved) saved = await refreshTokenManager.Update(usedRefreshToken.ID, true);

      if (!saved) return StatusCode(500, "Failed while saving");

      SetAccessTokenCookie(accessToken);
      SetRefreshTokenCookie(newRawRefreshToken);

      return Ok(new LoginTokenDto { Token = accessToken });
    }

    private string? GetAccessTokenFromRequest(LoginTokenDto? dto)
    {
      if (Request.Cookies.TryGetValue(AccessTokenCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
      {
        return cookieToken;
      }

      if (!string.IsNullOrWhiteSpace(dto?.Token))
      {
        return dto.Token;
      }

      return null;
    }

    private string? GetRefreshTokenFromRequest(LoginTokenDto? dto)
    {
      if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
        return cookieToken;

      if (!string.IsNullOrWhiteSpace(dto?.RefreshToken))
        return dto.RefreshToken;

      return null;
    }

    private void SetAccessTokenCookie(string token)
    {
      Response.Cookies.Append(AccessTokenCookieName, token, new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = DateTimeOffset.UtcNow.AddDays(7),
        Path = "/"
      });
    }

    private void SetRefreshTokenCookie(string token)
    {
      Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
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
