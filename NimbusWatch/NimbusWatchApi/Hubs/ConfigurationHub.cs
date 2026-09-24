using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NimbusWatchApi.Services;

namespace NimbusWatchApi.Hubs
{
  [Authorize(Roles = "Service,Admin")]
  public class ConfigurationHub : Hub
  {
    private readonly ServiceConnectionStore _connections;

    public ConfigurationHub(ServiceConnectionStore connections) => _connections = connections;

    public override async Task OnConnectedAsync()
    {
      var serviceId = Context.User?.FindFirst("client_id")?.Value;

      if (!string.IsNullOrWhiteSpace(serviceId))
      {
        _connections.Set(serviceId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, serviceId);
      }

      await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var serviceId = Context.User?.FindFirst("client_id")?.Value;

      if (!string.IsNullOrWhiteSpace(serviceId))
        _connections.Remove(serviceId, Context.ConnectionId);

      await base.OnDisconnectedAsync(exception);
    }
  }
}
