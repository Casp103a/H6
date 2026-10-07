using ClassLibrary.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class UserProfileController : ControllerBase
  {
    private readonly IUserProfileManager userProfileManager;
    private readonly IUserManager userManager;

    public UserProfileController(IUserProfileManager userProfileManager, IUserManager userManager)
    {
      this.userProfileManager = userProfileManager;
      this.userManager = userManager;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUserProfile()
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var username = User.Identity?.Name;

      if (username == null)
        return BadRequest("Token missing");

      var user = await userManager.GetUser(username);

      if (user == null)
        return NotFound();

      var profile = await userProfileManager.GetUserProfile(user.Id);

      if (profile == null)
        return NotFound();

      return Ok(profile);
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateUserProfile([FromBody] UserProfileRequestsDto request)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var username = User.Identity?.Name;

      if (username == null)
        return BadRequest("Token missing");

      var user = await userManager.GetUser(username);

      if (user == null)
        return NotFound();

      var result = await userProfileManager.CreateUserProfile(user.Id, request);

      if (result == null)
        return BadRequest("Failed to create user profile.");

      return Ok(result);
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileRequestsDto request)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var username = User.Identity?.Name;

      if (username == null)
        return BadRequest("Token missing");

      var user = await userManager.GetUser(username);

      if (user == null)
        return NotFound();

      var result = await userProfileManager.UpdateUserProfile(user.Id, request);

      if (result == null)
        return BadRequest("Failed to update user profile.");

      return Ok(result);
    }
  }
}
