using ClassLibrary.Dtos.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NimbusWatchApi.Hubs;
using NimbusWatchApi.Services;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ConfigurationController : ControllerBase
  {
    private readonly IHubContext<ConfigurationHub> hubContext;
    private readonly ServiceConnectionStore connectionStore;

    public ConfigurationController(IHubContext<ConfigurationHub> hubContext, ServiceConnectionStore connectionStore)
    {
      this.hubContext = hubContext;
      this.connectionStore = connectionStore;
    }

    [HttpPost("update/{clientId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateConfiguration([FromRoute] string clientId, [FromBody] ConfigurationDto configuration, CancellationToken cancellationToken)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      if (!connectionStore.TryGet(clientId, out var connectionId) || connectionId is null)
        return NotFound(new { message = "Service is not connected." });

      using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      cts.CancelAfter(TimeSpan.FromSeconds(10));

      try
      {
        var applied = await hubContext.Clients.Client(connectionId)
            .InvokeAsync<ConfigurationDto>("UpdateConfiguration", configuration, cts.Token);

        return Ok(applied);
      }
      catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
      {
        return StatusCode(StatusCodes.Status504GatewayTimeout, new { message = "Service did not respond in time." });
      }
      catch (Exception ex) when (ex is IOException or HubException)
      {
        return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
      }
    }

    [HttpGet("get/{clientId}")]
    [Authorize]
    public async Task<IActionResult> GetConfiguration([FromRoute] string clientId, CancellationToken cancellationToken)
    {
      if (!connectionStore.TryGet(clientId, out var connectionId) || connectionId is null)
        return NotFound(new { message = "Service is not connected." });

      using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      cts.CancelAfter(TimeSpan.FromSeconds(10));

      try
      {
        var config = await hubContext.Clients.Client(connectionId)
            .InvokeAsync<ConfigurationDto>("GetConfiguration", cts.Token);
        return Ok(config);
      }
      catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
      {
        return StatusCode(StatusCodes.Status504GatewayTimeout, new { message = "Service did not respond in time." });
      }
      catch (Exception ex) when (ex is IOException or HubException)
      {
        return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
      }
    }
  }
}
