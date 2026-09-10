using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class RoleController : ControllerBase
  {
    private readonly IRoleManager roleManager;

    public RoleController(IRoleManager roleManager)
    {
      this.roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var roles = await roleManager.GetAllRoles();
      return Ok(roles);
    }
  }
}
