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
    private readonly IServiceClientManager serviceClientManager;

    public CompanyHardwareDataController(ICompanyHardwareDataManager companyHardwareDataManager, IServiceClientManager serviceClientManager)
    {
      this.companyHardwareDataManager = companyHardwareDataManager;
      this.serviceClientManager = serviceClientManager;
    }

    [HttpGet("GetAll/{companyId}")]
    [Authorize]
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

    [HttpGet("GetHardwareData/{companyId}")]
    [Authorize]
    public async Task<IActionResult> GetHardwareDataAsync([FromRoute] string companyId, [FromQuery] string clientId)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (string.IsNullOrEmpty(companyId))
        return BadRequest("CompanyId cannot be null or empty.");

      if (string.IsNullOrEmpty(clientId))
        return BadRequest("ClientId cannot be null or empty.");

      var serviceClient = await serviceClientManager.GetByClientIdAsync(companyId, Guid.Parse(clientId));

      if (serviceClient == null)
        return NotFound();

      var result = await companyHardwareDataManager.GetAllByServiceIdAsync(companyId, serviceClient.Id);
      return Ok(result);
    }
  }
}
