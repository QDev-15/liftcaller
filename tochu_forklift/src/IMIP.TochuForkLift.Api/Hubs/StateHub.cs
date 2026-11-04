using Microsoft.AspNetCore.SignalR;

namespace IMIP.TochuForkLift.Api.Hubs;

/// <summary>
/// Clients only ever receive — pushed to from StateBroadcaster after every write. No client-to-hub
/// methods needed since all commands go through the REST endpoints (plan.md §6).
/// </summary>
public class StateHub : Hub
{
}
