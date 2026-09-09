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
    //[Authorize(Roles = "Admin")]
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
          RoleId = u.RoleId
        }).ToList();

        return Ok(userDtos);
      }
      catch (Exception ex)
      {
        return StatusCode(500, ex);
      }
    }
  }
}
