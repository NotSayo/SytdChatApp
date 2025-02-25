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
         if (!string.IsNullOrEmpty(roomCode))
         {
             await Clients.Group(roomCode).SendAsync("UserChange", $"User disconnected: {username ?? "Unknown"}");
             await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
             await LeaveRoom(roomCode);
             if(!string.IsNullOrEmpty(username))
                 RoomCodes.Codes[roomCode].Remove(username);
             await GetRoomsWithUsers(roomCode);
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

    public async Task GetRoomsWithUsers(string roomCode)
    {
        await Clients.Group(roomCode).SendAsync("ReceiveRoomsWithUsers", RoomCodes.Codes);
    }

    public async Task CreateRoom()
    {
        var roomcode = Guid.NewGuid().ToString().Substring(0, 6);
        var username = Context.Items["username"] as string ?? "user";
        RoomCodes.Codes.Add(roomcode, new List<string>() {username});
        Context.Items["room"] = roomcode;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomcode);
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

        var username = Context.Items["username"] as string;
        if(!string.IsNullOrEmpty(username))
            RoomCodes.Codes[roomcode].Add(username);

        Context.Items["room"] = roomcode;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomcode);
        await Clients.Caller.SendAsync("MoveToRoom", roomcode);
        await Clients.Group(roomcode).SendAsync("UserChange", $"User connected: {Context.Items["username"] as string}");
        await Clients.All.SendAsync("ReceiveRooms", RoomCodes.Codes.Keys.ToList());
        await GetRoomsWithUsers(roomcode);
    }

    public async Task LeaveRoom(string roomCode)
    {
        Context.Items.Remove("room");
        await Clients.Group(roomCode).SendAsync("UserChange", $"User disconnected from Room: {Context.Items["username"] as string}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
        var username = Context.Items["username"] as string;
        if(!string.IsNullOrEmpty(username))
            RoomCodes.Codes[roomCode].Remove(username);

        if (RoomCodes.Codes[roomCode].Count == 0 && roomCode != "General")
        {
            RoomCodes.Codes.Remove(roomCode);
            await Clients.All.SendAsync("ReceiveRooms", RoomCodes.Codes.Keys.ToList());
        }
        await GetRoomsWithUsers(roomCode);
    }

    // Messages
    public async Task SendMessage(string code, Message message)
    {
        await Clients.Group(code).SendAsync("ReceiveMessage", message);
    }


}