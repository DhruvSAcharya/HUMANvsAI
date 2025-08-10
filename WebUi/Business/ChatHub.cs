using Microsoft.AspNetCore.SignalR;
using WebUi.Business;

public class ChatHub : Hub
{
    private readonly BotManager _botManager;
    private readonly PlayerManager _playerManager;
    private readonly RoomManager _roomManager;

    public ChatHub(BotManager botManager, PlayerManager playerManager, RoomManager roomManager)
    {
        _botManager = botManager;
        _playerManager = playerManager;
        _roomManager = roomManager;
    }

    public async Task SendMessageToGroup(string groupName, int playerId, string message)
    {
        _botManager.RecordMessage(groupName, _playerManager.GetPlayerName(playerId), message);
        await Clients.Group(groupName).SendAsync("ReceiveMessage", playerId, message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        if ((_roomManager.GetRoomById(Convert.ToInt32(groupName))?.Players.Count ?? 0) < 5)
        {
            _botManager.StartBotsForGroup(groupName);
        }

    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
