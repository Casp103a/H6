using ClassLibrary.Dtos;
using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ServerServiceController : ControllerBase
  {
    private readonly IServiceClientManager serviceClientManager;

    public ServerServiceController(IServiceClientManager serviceClientManager)
    {
      this.serviceClientManager = serviceClientManager;
    }

    [HttpPost("Add")]
    public async Task<IActionResult> AddServiceClient([FromBody] CreateServerServiceDto client)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);
      if (client == null)
        return BadRequest("Client cannot be null.");
      var result = await serviceClientManager.AddAsync(client);
      if (result != null)
      {
        var dto = new ServerCreatedDto
        {
          ClientId = result.Value.ClientId.ToString(),
          ClientSecret = result.Value.ClientSecret
        };

        return Ok(dto);
      }
      else
        return StatusCode(500, "An error occurred while adding the service client.");
    }
  }

  public class ServerCreatedDto
  {
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
  }
}
