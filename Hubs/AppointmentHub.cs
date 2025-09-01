using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Meetify.Hubs;

[Authorize]
public class AppointmentHub : Hub
{
}
