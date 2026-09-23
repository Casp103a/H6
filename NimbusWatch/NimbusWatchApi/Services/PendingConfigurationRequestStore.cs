using System.Collections.Concurrent;

namespace NimbusWatchApi.Services;

public class PendingConfigurationRequestStore
{
  private readonly ConcurrentDictionary<string, string> _requests = new();

  public void Add(string requestId, string userId)
  {
    _requests[requestId] = userId;
  }

  public bool TryRemove(string requestId, out string userId)
  {
    var removed = _requests.TryRemove(requestId, out var value);
    userId = value;
    return removed;
  }
}
