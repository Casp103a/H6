using ClassLibrary.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class CompanyController : ControllerBase
  {
    private readonly ICompanyManager companyManager;
    private readonly IUserManager userManager;

    public CompanyController(ICompanyManager companyManager, IUserManager userManager)
    {
      this.companyManager = companyManager;
      this.userManager = userManager;
    }

    [HttpPost("create")]
    public async Task<ActionResult> CreateCompany([FromBody] CreateCompanyRequestDto request)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var result = await companyManager.CreateCompany(request);

      if (result == null)
      {
        return BadRequest("Failed to create company.");
      }

      return Ok(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUsersCompany()
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var username = User.Identity?.Name;

      if (username == null)
        return BadRequest("Token missing");

      var user = await userManager.GetUserModel(username);

      if (user == null)
        return NotFound();

      var company = await companyManager.GetCompany(user.CompanyId);

      if (company == null)
        return NotFound();

      return Ok(company);
    }
  }
}
