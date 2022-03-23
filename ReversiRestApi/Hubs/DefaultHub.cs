using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ReversiRestApi.Hubs
{
    public class DefaultHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}