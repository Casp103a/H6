using ClassLibrary.Dtos;
using ClassLibrary.Emuns;
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
    private readonly ICompanyManager companyManager;

    public UserController(IUserManager userManager, ICompanyManager companyManager)
    {
      this.userManager = userManager;
      this.companyManager = companyManager;
    }

    [HttpPost("signuponcompanycreate")]
    public async Task<IActionResult> SignUpOnCompanyCreate([FromBody] SignupWithCompanyDto userDto)
    {
      try
      {
        if (!ModelState.IsValid)
          return BadRequest(ModelState);

        if (string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password) || string.IsNullOrEmpty(userDto.CompanyId))
        {
          return BadRequest("All fields are required.");
        }

        var doesUserExistingInCompany = await userManager.CheckIfCompanyHasAUser(userDto.CompanyId);

        if (doesUserExistingInCompany)
        {
          return BadRequest("A user already exists in this company.");
        }

        var doesComapanyExist = await companyManager.GetCompany(userDto.CompanyId);

        if (doesComapanyExist == null)
        {
          return BadRequest("The specified company does not exist.");
        }

        var registeruserdto = new RegisterUserDto
        {
          Email = userDto.Email,
          Password = userDto.Password,
          CompanyId = userDto.CompanyId,
          RoleId = (int)RoleEnums.Admin
        };

        var createdUser = await userManager.CreateAsync(registeruserdto);

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
        if (!ModelState.IsValid)
          return BadRequest(ModelState);

        var users = await userManager.GetAllUsers(companyId);

        return Ok(users);
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

      return Ok(user);
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

        var existingUser = await userManager.GetUserModel(id);

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
