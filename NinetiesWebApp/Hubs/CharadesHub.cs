using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CharadesHub : Hub
{
    private static List<string> Players = new();
    private static Dictionary<string, string> playerConnections = new();
    private static string King = "";
    private static string TextClue = "";
    private static string EmojiClue = "";
    private static Dictionary<string, string> PlayerGuesses = new();

    public async Task JoinGame(string playerName)
    {
        if (!Players.Contains(playerName))
        {
            Players.Add(playerName);
        }

        if (!playerConnections.ContainsKey(playerName))
        {
            playerConnections[playerName] = Context.ConnectionId;
        }

        await Clients.All.SendAsync("PlayerListUpdated", Players);
    }

    public async Task StartGame()
    {
        if (Players.Count == 0) return;

        var random = new Random();
        King = Players[random.Next(Players.Count)];

        PlayerGuesses.Clear();
        TextClue = "";
        EmojiClue = "";

        foreach (var player in Players)
        {
            if (playerConnections.TryGetValue(player, out var connectionId))
            {
                if (player == King)
                    await Clients.Client(connectionId).SendAsync("YouAreKing");
                else
                    await Clients.Client(connectionId).SendAsync("WaitingForClue");
            }
        }
    }


    public async Task SubmitClue(string text, string emoji)
    {
        TextClue = text;
        EmojiClue = emoji;
        await Clients.All.SendAsync("ClueSubmitted", text, emoji);
    }

    public async Task SubmitGuess(string playerName, string guess)
    {
        PlayerGuesses[playerName] = guess;
        await Clients.All.SendAsync("ReceiveGuess", playerName, guess);

        if (PlayerGuesses.Count >= Players.Count - 1)
        {
            if (playerConnections.TryGetValue(King, out var kingConnectionId))
            {
                await Clients.Client(kingConnectionId).SendAsync("PromptWinnerSelection", Players);
            }
        }
    }

    public async Task SendBuzz()
    {
        await Clients.All.SendAsync("ReceiveBuzz");
    }


    public async Task DeclareWinner(string winnerName)
    {
        await Clients.All.SendAsync("WinnerDeclared", winnerName);
        PlayerGuesses.Clear();
    }

    public override async Task OnConnectedAsync()
    {
        string name = Context.GetHttpContext()?.Request.Query["username"];
        if (!string.IsNullOrEmpty(name))
        {
            playerConnections[name] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string nameToRemove = playerConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        if (!string.IsNullOrEmpty(nameToRemove))
        {
            Players.Remove(nameToRemove);
            playerConnections.Remove(nameToRemove);
            await Clients.All.SendAsync("PlayerListUpdated", Players);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
