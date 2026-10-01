using ClassLibrary.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ServiceStatusController : ControllerBase
  {
    private readonly ICompanyServiceStatusManager serviceStatusManager;

    public ServiceStatusController(ICompanyServiceStatusManager serviceStatusManager)
    {
      this.serviceStatusManager = serviceStatusManager;
    }

    [HttpPost("Add/{companyId}")]
    [Authorize(Roles = "Service")]
    public async Task<IActionResult> AddServiceStatusAsync([FromRoute] string companyId, [FromBody] ServiceReportDto status)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (string.IsNullOrEmpty(companyId))
        return BadRequest("CompanyId cannot be null or empty.");

      if (status == null)
        return BadRequest("Status cannot be null.");

      var result = await serviceStatusManager.AddServiceReportAsync(companyId, status);
      if (result)
        return Ok();
      else
        return StatusCode(500, "An error occurred while adding the service status.");
    }

    [HttpGet("GetAll/{companyId}")]
    [Authorize]
    public async Task<IActionResult> GetAllServiceReportsAsync([FromRoute] string companyId)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (string.IsNullOrEmpty(companyId))
        return BadRequest("CompanyId cannot be null or empty.");

      var result = await serviceStatusManager.GetAllServiceStatusesAsync(companyId);
      if (result != null)
        return Ok(result);
      else
        return StatusCode(500, "An error occurred while retrieving the service statuses.");
    }

    [HttpGet("Get/{companyId}")]
    [Authorize]
    public async Task<IActionResult> GetServiceStatusAsync([FromRoute] string companyId, [FromQuery] int serverId)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (string.IsNullOrEmpty(companyId))
        return BadRequest("CompanyId cannot be null or empty.");

      if (serverId <= 0)
        return BadRequest("ServerId must be a positive integer.");

      var result = await serviceStatusManager.GetServerServicesStatusesAsync(companyId, serverId);
      if (result != null)
        return Ok(result);
      else
        return StatusCode(500, "An error occurred while retrieving the service statuses.");
    }
  }
}
