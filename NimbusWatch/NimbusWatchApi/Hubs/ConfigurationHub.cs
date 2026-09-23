using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NimbusWatchApi.Hubs
{
  [Authorize(Roles = "Service,Admin")]
  public class ConfigurationHub : Hub
  {
    public override async Task OnConnectedAsync()
    {
      var serviceId = Context.GetHttpContext()?.Request.Query["serviceId"].ToString();

      if (!string.IsNullOrWhiteSpace(serviceId))
      {
        await Groups.AddToGroupAsync(Context.ConnectionId, serviceId);
      }

      await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var serviceId = Context.GetHttpContext()?.Request.Query["serviceId"].ToString();

      if (!string.IsNullOrWhiteSpace(serviceId))
      {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, serviceId);
      }

      await base.OnDisconnectedAsync(exception);
    }
  }
}
