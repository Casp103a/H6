using ClassLibrary.Configuration;
using ClassLibrary.Dtos;
using ClassLibrary.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;
using System.Text.Json;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ServerServiceController : ControllerBase
  {
    private readonly IServiceClientManager serviceClientManager;
    private readonly IEncryptingService encryptingService;

    public ServerServiceController(IServiceClientManager serviceClientManager, IEncryptingService encryptingService)
    {
      this.serviceClientManager = serviceClientManager;
      this.encryptingService = encryptingService;
    }

    [HttpPost("Add")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddServiceClient([FromBody] CreateServerServiceDto client)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);
      if (client == null)
        return BadRequest("Client cannot be null.");
      var result = await serviceClientManager.AddAsync(client);
      if (result != null)
      {
        var config = new ServiceConfiguration()
        {
          ClientId = result.Value.ClientId.ToString(),
          ClientSecret = result.Value.ClientSecret,
          CompanyId = Guid.Parse(client.CompanyId),
          ServerName = client.Name,
          ReportFrequencyInMilliSec = 60000, // Default to 60 secondss

          Services = client.ServicesToMonitor.Select(s => new ServerServicesInfo
          {
            Name = s.Name,
          }).ToList(),
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
          WriteIndented = true
        });

        var encryptedJson = encryptingService.EncryptForFile(json);

        var file = System.Text.Encoding.UTF8.GetBytes(encryptedJson);

        return File(file, "application/octet-stream", "Service_Config.config");
      }
      else
        return StatusCode(500, "An error occurred while adding the service client.");
    }

    [HttpGet("GetServerInfo/{companyId}")]
    [Authorize]
    public async Task<IActionResult> GetServerInfo([FromRoute] string companyId, [FromQuery] string clientId)
    {
      if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(companyId))
        return BadRequest("ClientId or CompanyId cannot be null or empty.");

      var result = await serviceClientManager.GetByClientIdAsync(companyId, clientId);
      if (result != null)
      {
        return Ok(result);
      }
      else
        return NotFound("Service client not found.");
    }

    [HttpGet("GetAll/{companyId}")]
    [Authorize]
    public async Task<IActionResult> GetAll([FromRoute] string companyId)
    {
      if (string.IsNullOrEmpty(companyId))
        return BadRequest("CompanyId cannot be null or empty.");

      var result = await serviceClientManager.GetAllByCompanyIdAsync(companyId);
      if (result != null && result.Any())
      {
        return Ok(result);
      }
      else
        return NotFound("No service clients found for the specified company.");
    }

    [HttpGet("GetServiceInfo/{companyId}")]
    [Authorize(Roles = "Service")]
    public async Task<IActionResult> GetServiceInfo([FromRoute] string companyId, [FromQuery] string clientId)
    {
      if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(companyId))
        return BadRequest("ClientId or CompanyId cannot be null or empty.");

      var result = await serviceClientManager.GetByClientIdAsync(companyId, clientId);
      if (result != null)
      {
        return Ok(result);
      }
      else
        return NotFound("Service client not found.");
    }
  }
}
