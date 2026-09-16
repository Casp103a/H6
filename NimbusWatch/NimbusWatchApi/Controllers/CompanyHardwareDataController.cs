using ClassLibrary.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class CompanyHardwareDataController : ControllerBase
  {
    private readonly ICompanyHardwareDataManager companyHardwareDataManager;

    public CompanyHardwareDataController(ICompanyHardwareDataManager companyHardwareDataManager)
    {
      this.companyHardwareDataManager = companyHardwareDataManager;
    }

    [HttpGet("GetAll/{companyId}")]
    public async Task<IActionResult> GetAllByCompanyIdAsync([FromRoute] string companyId)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (string.IsNullOrEmpty(companyId))
        return BadRequest("CompanyId cannot be null or empty.");

      var result = await companyHardwareDataManager.GetAllByCompanyIdAsync(companyId);
      return Ok(result);
    }

    [HttpPost("Add/{companyId}")]
    [Authorize(Roles = "Service")]
    public async Task<IActionResult> AddAsync([FromRoute] string companyId, [FromBody] CompanyHardwareDataDto data)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (string.IsNullOrEmpty(companyId))
        return BadRequest("CompanyId cannot be null or empty.");

      if (data == null)
        return BadRequest("Data cannot be null.");

      var result = await companyHardwareDataManager.AddAsync(data);

      if (result)
        return Ok();
      else
        return StatusCode(500, "An error occurred while adding the data.");
    }
  }
}
