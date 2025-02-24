using Microsoft.AspNetCore.SignalR;
using Server.Classes;
using Server.Storages;

namespace Server.Hubs;

public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
         await base.OnDisconnectedAsync(exception);
         var username = Context.Items["username"] as string;
         await Clients.All.SendAsync("UserChange", $"User disconnected: {username ?? "Unknown"}");
         UsernameStorage.Storage.Where(s => s.Value == username).ToList().ForEach(s => UsernameStorage.Storage.Remove(s.Key));
    }

    public async Task SetName(string username)
    {
        if(UsernameStorage.Storage.ContainsValue(username))
        {
            await Clients.Caller.SendAsync("NameExistsError");
            return;
        }
        UsernameStorage.Storage[Context.ConnectionId] = username;
        Context.Items.Add("username", username);
        await Clients.All.SendAsync("UserChange", $"User connected: {username}");
        await Clients.Caller.SendAsync("NameSet", username);
    }

    // Messages
    public async Task SendMessage(string username, Message message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }


}