using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Managers;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ServerTypesController : ControllerBase
  {
    private readonly IServerTypesManager serverTypesManager;

    public ServerTypesController(IServerTypesManager serverTypesManager)
    {
      this.serverTypesManager = serverTypesManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetServerTypes()
    {
      var serverTypes = await serverTypesManager.GetAllServerTypes();
      return Ok(serverTypes);
    }
  }
}
