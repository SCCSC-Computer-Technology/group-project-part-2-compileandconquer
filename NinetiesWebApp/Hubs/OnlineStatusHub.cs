using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NinetiesWebApp.Hubs
{
    public class OnlineStatusHub : Hub
    {
        // Static dictionary to store online users (UserId -> ConnectionId)
        private static readonly ConcurrentDictionary<string, string> OnlineUsers = new ConcurrentDictionary<string, string>();

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = Context.UserIdentifier; // Gets the UserId from Identity
                if (userId != null)
                {
                    OnlineUsers.TryAdd(userId, Context.ConnectionId);
                    // Broadcast updated online users list
                    await Clients.All.SendAsync("UpdateOnlineUsers", OnlineUsers.Keys.ToList());
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = Context.UserIdentifier;
                if (userId != null)
                {
                    OnlineUsers.TryRemove(userId, out _);
                    // Broadcast updated online users list
                    await Clients.All.SendAsync("UpdateOnlineUsers", OnlineUsers.Keys.ToList());
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Method to get the current online users
        public Task GetOnlineUsers()
        {
            return Clients.Caller.SendAsync("UpdateOnlineUsers", OnlineUsers.Keys.ToList());
        }
    }
}
