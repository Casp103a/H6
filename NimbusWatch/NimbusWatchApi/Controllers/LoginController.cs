using Microsoft.AspNetCore.Mvc;
using NimbusWatchApi.Dtos;

namespace NimbusWatchApi.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class LoginController : ControllerBase
  {
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
      // Implement your login logic here
      // For example, validate the username and password against a database
      if (request.Username == "admin" && request.Password == "password")
      {
        return Ok(new { Token = "your_generated_token" });
      }
      else
      {
        return Unauthorized(new { Message = "Invalid username or password" });
      }
    }
  }
}
