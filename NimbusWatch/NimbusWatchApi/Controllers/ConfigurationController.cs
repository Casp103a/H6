using ClassLibrary.Dtos.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NimbusWatchApi.Hubs;
using NimbusWatchApi.Services;
using System.Security.Claims;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ConfigurationController : ControllerBase
  {
    private readonly IHubContext<ConfigurationHub> hubContext;
    private readonly PendingConfigurationRequestStore requestStore;

    public ConfigurationController(IHubContext<ConfigurationHub> hubContext, PendingConfigurationRequestStore requestStore)
    {
      this.hubContext = hubContext;
      this.requestStore = requestStore;
    }

    [HttpPost("update/{serviceId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateConfiguration([FromRoute] string serviceId, [FromBody] ConfigurationDto configuration)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      await hubContext.Clients.Group(serviceId)
          .SendAsync("ReceiveConfigurationUpdate", configuration);

      return Ok(new { message = "Configuration update broadcasted successfully." });
    }

    [HttpPost("request/{serviceId}")]
    [Authorize]
    public async Task<IActionResult> RequestConfiguration([FromRoute] string serviceId)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized(new { message = "User id is missing." });

      var requestId = Guid.NewGuid().ToString("N");
      requestStore.Add(requestId, userId);

      await hubContext.Clients.Group(serviceId)
          .SendAsync("ReceiveConfigurationRequest", requestId);

      return Ok(new
      {
        requestId,
        message = "Configuration request sent to service."
      });
    }

    [HttpPost("response/{requestId}")]
    [Authorize(Roles = "Service")]
    public async Task<IActionResult> RespondToConfigurationRequest([FromRoute] string requestId, [FromBody] ConfigurationDto configuration)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (!requestStore.TryRemove(requestId, out var userId) || string.IsNullOrWhiteSpace(userId))
        return NotFound(new { message = "Request not found or already completed." });

      await hubContext.Clients.User(userId)
          .SendAsync("ReceiveConfigurationResponse", requestId, configuration);

      return Ok(new { message = "Configuration response sent to requesting user." });
    }
  }
}
