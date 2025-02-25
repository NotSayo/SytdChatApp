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
         var roomCode = Context.Items["room"] as string;
         if (string.IsNullOrEmpty(roomCode))
             roomCode = "";
         else
         {
             await Clients.Group(roomCode).SendAsync("UserChange", $"User disconnected: {username ?? "Unknown"}");
             await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
             await LeaveRoom(roomCode);
         }
         // await Clients.All.SendAsync("UserChange", $"User disconnected from server: {username ?? "Unknown"}");
         UsernameStorage.Storage.Where(s => s.Value == username)
             .ToList()
             .ForEach(s => UsernameStorage.Storage.Remove(s.Key));
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
        await Clients.Caller.SendAsync("NameSet", username);
    }

    // Rooms

    public async Task GetRooms()
    {
        await Clients.Caller.SendAsync("ReceiveRooms", RoomCodes.Codes.Keys.ToList());
    }

    public async Task CreateRoom()
    {
        var roomcode = Guid.NewGuid().ToString().Substring(0, 6);
        RoomCodes.Codes.Add(roomcode!, 1);
        Context.Items["room"] = roomcode;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomcode!);
        await Clients.Caller.SendAsync("MoveToRoom", roomcode);
        await Clients.Group(roomcode).SendAsync("UserChange", $"User connected: {Context.Items["username"] as string}");
        await Clients.All.SendAsync("ReceiveRooms", RoomCodes.Codes.Keys.ToList());
    }

    public async Task JoinRoom(string roomcode)
    {
        if (!RoomCodes.Codes.Any(s => s.Key == roomcode))
        {
            await Clients.Caller.SendAsync("RoomDoesNotExist");
            return;
        }

        RoomCodes.Codes[roomcode]++;
        Context.Items["room"] = roomcode;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomcode);
        await Clients.Caller.SendAsync("MoveToRoom", roomcode);
        await Clients.Group(roomcode).SendAsync("UserChange", $"User connected: {Context.Items["username"] as string}");
        await Clients.All.SendAsync("ReceiveRooms", RoomCodes.Codes.Keys.ToList());
    }

    public async Task LeaveRoom(string roomCode)
    {

        Context.Items.Remove("room");
        await Clients.Group(roomCode).SendAsync("UserChange", $"User disconnected from Room: {Context.Items["username"] as string}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
        RoomCodes.Codes[roomCode]--;
        if (RoomCodes.Codes[roomCode] == 0 && roomCode != "General")
        {
            RoomCodes.Codes.Remove(roomCode);
            await Clients.All.SendAsync("ReceiveRooms", RoomCodes.Codes.Keys.ToList());
        }
    }

    // Messages
    public async Task SendMessage(string code, Message message)
    {
        await Clients.Group(code).SendAsync("ReceiveMessage", message);
    }


}