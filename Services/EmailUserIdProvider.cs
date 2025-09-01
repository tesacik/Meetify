using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Meetify.Services;

public class EmailUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? connection.User?.FindFirst("email")?.Value;
    }
}

