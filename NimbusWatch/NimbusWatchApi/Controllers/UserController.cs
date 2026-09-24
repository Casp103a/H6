using ClassLibrary.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class UserController : ControllerBase
  {
    private readonly IUserManager userManager;

    public UserController(IUserManager userManager)
    {
      this.userManager = userManager;
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto userDto)
    {
      try
      {
        if (!ModelState.IsValid)
          return BadRequest(ModelState);

        if (string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password) || string.IsNullOrEmpty(userDto.CompanyId))
        {
          return BadRequest("All fields are required.");
        }

        var createdUser = await userManager.CreateAsync(userDto);

        if (createdUser != null)
        {
          return Ok();
        }

        return StatusCode(500);
      }
      catch (Exception ex)
      {
        return StatusCode(500, ex);
      }
    }

    [HttpGet("GetAllUsers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers([FromQuery] string companyId)
    {
      try
      {
        var users = await userManager.GetAllUsers(companyId);

        var userDtos = users.Select(u => new UserInfoDto
        {
          Id = u.Id,
          Email = u.Email,
          RoleId = u.RoleId,
          PublicId = u.PublicId,
        }).ToList();

        return Ok(userDtos);
      }
      catch (Exception ex)
      {
        return StatusCode(500, ex);
      }
    }

    [HttpGet("GetCurrentUserInfo")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserInfo()
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var username = User.Identity?.Name;

      if (username == null)
        return BadRequest("Token missing");

      var user = await userManager.GetUser(username);

      if (user == null)
        return NotFound();

      var userInfoDto = new UserInfoDto
      {
        Id = user.Id,
        Email = user.Email,
        RoleId = user.RoleId,
        PublicId = user.PublicId,
      };

      return Ok(userInfoDto);
    }

    [HttpGet("GetUser/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUser([FromRoute] int id)
    {
      try
      {
        var user = await userManager.GetUser(id);

        if (user == null)
          return NotFound();

        return Ok(new UserInfoDto { Id = user.Id, Email = user.Email, RoleId = user.RoleId, PublicId = user.PublicId });
      }
      catch (Exception ex)
      {
        return StatusCode(500, ex);
      }
    }

    [HttpPut("UpdateUser/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromBody] UpdateUserDto dto)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      try
      {
        if (string.IsNullOrEmpty(dto.Email) || dto.RoleId < 0)
          return BadRequest();

        var existingUser = await userManager.GetUser(id);

        if (existingUser == null)
          return NotFound();

        existingUser.Email = dto.Email;
        existingUser.RoleId = dto.RoleId;

        var passwordChanged = false;

        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
          existingUser.Password = dto.NewPassword;
          passwordChanged = true;
        }

        var result = await userManager.UpdateAsync(existingUser, passwordChanged);

        if (result != true)
          return StatusCode(500);

        return Ok();
      }
      catch (Exception ex)
      {
        return StatusCode(500, ex);
      }
    }

  }
}
