using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace NinetiesWebApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _connectedUsers = new ConcurrentDictionary<string, string>();
        private readonly UserManager<IdentityUser> _userManager;

        public static List<string> GetOnlineUsers()
        {
            return _connectedUsers.Values.ToList();
        }


        public ChatHub(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                throw new HubException("Unauthorized access.");
            }

            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null)
            {
                throw new HubException("User not found.");
            }

            var displayName = user.UserName;
            _connectedUsers.TryAdd(Context.ConnectionId, displayName);
            await UpdateUserList();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectedUsers.TryRemove(Context.ConnectionId, out _))
            {
                await UpdateUserList();
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string message)
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                throw new HubException("Unauthorized access.");
            }

            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null)
            {
                throw new HubException("User not found.");
            }

            var displayName = user.UserName;
            await Clients.All.SendAsync("ReceiveMessage", displayName, message);
        }

        private async Task UpdateUserList()
        {
            var userList = _connectedUsers.Values.ToList();
            await Clients.All.SendAsync("UpdateUserList", userList);
        }
    }
}
