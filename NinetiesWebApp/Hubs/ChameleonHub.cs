using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NinetiesWebApp.Hubs
{
    public class ChameleonHub : Hub
    {
        private static Dictionary<string, string> players = new();
        private static string currentCategory = "";
        private static List<string> currentWordGrid = new();
        private static string secretWord = "";
        private static string chameleonConnectionId = "";
        private static string currentClueTurn = "";
        private static Queue<string> clueTurnOrder = new();
        private static List<(string connectionId, string clue)> submittedClues = new();
        private static bool gameStarted = false;
        private static Dictionary<string, string> playerVotes = new();
        private static string chosenCategory = "";
        private static Dictionary<string, string> categoryVotes = new();

        private static readonly Dictionary<string, List<string>> categories = new()
        {
            { "Toys and Games", new() { "Yo-Yo", "Pogs", "Tamagotchi", "Furby", "Nerf", "Legos", "Skip-It", "Gameboy", "Etch-A-Sketch", "Hot Wheels", "Super Soaker", "Polly Pocket", "Slinky", "Easy-Bake", "Bop It", "Twister" } },
            { "Cartoons and TV", new() { "Rugrats", "Doug", "Power Rangers", "Hey Arnold", "Arthur", "Animaniacs", "Dexter", "Pokemon", "Sailor Moon", "Recess", "CatDog", "X-Men", "All That", "Goosebumps", "Friends", "Seinfeld" } },
            { "Coding", new() { "Loop", "Variable", "Function", "Array", "Boolean", "Class", "String", "Integer", "Object", "If Statement", "Null", "For Loop", "Try Catch", "Switch", "Public", "Static" } },
            { "Basic Words", new() { "Tree", "Phone", "Chair", "Dog", "Pizza", "House", "River", "Ball", "Moon", "Book", "Table", "Road", "Hat", "Sun", "Star", "Pen" } }
        };

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task JoinGame(string playerName)
        {
            players[Context.ConnectionId] = playerName;
            await Clients.All.SendAsync("UpdatePlayerList", players.Values.ToList());
        }

        public async Task StartGame()
        {
            categoryVotes.Clear();
            chosenCategory = categories.Keys.First();
            currentWordGrid = categories[chosenCategory];
            var rand = new Random();
            secretWord = currentWordGrid[rand.Next(currentWordGrid.Count)];

            var playerIds = players.Keys.ToList();
            chameleonConnectionId = playerIds[rand.Next(playerIds.Count)];

            var ordered = players.Keys.Where(id => id != chameleonConnectionId).OrderBy(_ => rand.Next()).ToList();
            ordered.Insert(rand.Next(1, ordered.Count + 1), chameleonConnectionId);
            clueTurnOrder = new Queue<string>(ordered);
            currentClueTurn = clueTurnOrder.Peek();
            gameStarted = true;

            foreach (var player in players)
            {
                bool isCham = player.Key == chameleonConnectionId;
                await Clients.Client(player.Key).SendAsync("ReceiveSecretWordAndGrid", secretWord, currentWordGrid, isCham);

                if (isCham)
                {
                    await Clients.Client(player.Key).SendAsync("ReceiveChameleonNotification");
                }
            }

            await Clients.All.SendAsync("StartClueSubmissionPhase", players[currentClueTurn]);
        }

        public async Task NotifyNextTurn(string nextPlayerName)
        {
            // Helper method to let client-side know who's turn it is
            var match = players.FirstOrDefault(p => p.Value == nextPlayerName);
            if (!string.IsNullOrEmpty(match.Key))
            {
                await Clients.All.SendAsync("NextClueTurn", nextPlayerName);
            }
        }

        public async Task SubmitClue(string clue)
        {
            submittedClues.Add((Context.ConnectionId, clue));
            await Clients.All.SendAsync("ReceiveClue", players[Context.ConnectionId], clue);

            clueTurnOrder.Dequeue();
            if (clueTurnOrder.Count > 0)
            {
                currentClueTurn = clueTurnOrder.Peek();
                await Clients.All.SendAsync("NextClueTurn", players[currentClueTurn]);
            }
            else
            {
                await Clients.All.SendAsync("AllCluesSubmitted");
            }
        }

        public async Task CastVote(string votedPlayerName)
        {
            playerVotes[Context.ConnectionId] = votedPlayerName;

            if (playerVotes.Count == players.Count)
            {
                var voteCounts = playerVotes.Values
                    .GroupBy(v => v)
                    .ToDictionary(g => g.Key, g => g.Count());

                var maxVotes = voteCounts.Values.Max();
                var tiedPlayers = voteCounts.Where(v => v.Value == maxVotes).Select(v => v.Key).ToList();

                if (tiedPlayers.Count == 1)
                {
                    var votedOut = tiedPlayers[0];
                    bool wasChameleon = players.Any(p => p.Value == votedOut && p.Key == chameleonConnectionId);
                    await Clients.All.SendAsync("RevealVoteResult", votedOut, wasChameleon);
                }
                else
                {
                    await Clients.All.SendAsync("TieBreakerVote", tiedPlayers);
                }
            }
        }

        public async Task ResetGame()
        {
            currentCategory = "";
            currentWordGrid.Clear();
            secretWord = "";
            chameleonConnectionId = "";
            clueTurnOrder.Clear();
            submittedClues.Clear();
            playerVotes.Clear();
            categoryVotes.Clear();
            chosenCategory = "";
            currentClueTurn = "";
            gameStarted = false;

            await Clients.All.SendAsync("GameReset");
        }
    }
}
