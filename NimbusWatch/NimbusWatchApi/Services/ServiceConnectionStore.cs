using System.Collections.Concurrent;

namespace NimbusWatchApi.Services;

public class ServiceConnectionStore
{
  private readonly ConcurrentDictionary<string, string> _connections = new();

  public void Set(string serviceId, string connectionId) => _connections[serviceId] = connectionId;

  public void Remove(string serviceId, string connectionId) =>
    _connections.TryRemove(new KeyValuePair<string, string>(serviceId, connectionId));

  public bool TryGet(string serviceId, out string? connectionId) =>
    _connections.TryGetValue(serviceId, out connectionId);
}
