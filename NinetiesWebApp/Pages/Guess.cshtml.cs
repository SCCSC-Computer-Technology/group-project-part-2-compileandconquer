using NinetiesWebApp.Hubs;
using NinetiesWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NinetiesWebApp.Pages
{
    public class GuessModel : PageModel
    {
        private readonly DeezerService _deezerService;

        [BindProperty]
        public string SelectedAnswer { get; set; }

        public SongGame Game { get; set; }

        public string FeedbackMessage { get; set; }
        public string FeedbackClass { get; set; }
        public bool ShowTitle { get; set; }

        public GuessModel(DeezerService deezerService)
        {
            _deezerService = deezerService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Initialize a new game if not present in TempData
            if (TempData["SongGame"] == null)
            {
                Game = await _deezerService.CreateGameAsync(10);
                TempData["SongGame"] = System.Text.Json.JsonSerializer.Serialize(Game);
                TempData["AdditionalArtists"] = System.Text.Json.JsonSerializer.Serialize(
                    await _deezerService.GetAdditionalArtistsAsync(30));
            }
            else
            {
                // Retrieve existing game
                Game = System.Text.Json.JsonSerializer.Deserialize<SongGame>((string)TempData["SongGame"]);
                ShowTitle = TempData["ShowTitle"] != null && (bool)TempData["ShowTitle"];
            }

            return Page();
        }

        public IActionResult OnPostSubmitAnswerAsync()
        {
            if (TempData["SongGame"] == null)
            {
                return RedirectToPage();
            }

            Game = System.Text.Json.JsonSerializer.Deserialize<SongGame>((string)TempData["SongGame"]);
            var additionalArtists = System.Text.Json.JsonSerializer.Deserialize<List<string>>((string)TempData["AdditionalArtists"]);

            // Check if the answer is correct
            if (SelectedAnswer == Game.CurrentTrack.Artist)
            {
                Game.Score++;
                FeedbackMessage = "Correct! It's " + Game.CurrentTrack.Title + " by " + Game.CurrentTrack.Artist;
                FeedbackClass = "text-success";
            }
            else
            {
                FeedbackMessage = $"Wrong! It was {Game.CurrentTrack.Title} by {Game.CurrentTrack.Artist}";
                FeedbackClass = "text-danger";
            }

            // Show the song title now
            TempData["ShowTitle"] = true;

            // Save game state back to TempData
            TempData["SongGame"] = System.Text.Json.JsonSerializer.Serialize(Game);
            TempData["AdditionalArtists"] = System.Text.Json.JsonSerializer.Serialize(additionalArtists);

            return Page();
        }

        public IActionResult OnPostNextSong()
        {
            if (TempData["SongGame"] == null)
            {
                return RedirectToPage();
            }

            Game = System.Text.Json.JsonSerializer.Deserialize<SongGame>((string)TempData["SongGame"]);
            var additionalArtists = System.Text.Json.JsonSerializer.Deserialize<List<string>>((string)TempData["AdditionalArtists"]);

            // Move to next song
            Game.CurrentTrackIndex++;

            if (!Game.IsGameOver)
            {
                // Update possible answers for new song
                _deezerService.UpdatePossibleAnswers(Game, additionalArtists);
            }

            // Reset song title visibility
            TempData["ShowTitle"] = false;

            // Save game state back to TempData
            TempData["SongGame"] = System.Text.Json.JsonSerializer.Serialize(Game);
            TempData["AdditionalArtists"] = System.Text.Json.JsonSerializer.Serialize(additionalArtists);

            return Page();
        }

        public IActionResult OnPostNewGame()
        {
            // Clear the game from TempData to start a new one
            TempData.Remove("SongGame");
            TempData.Remove("AdditionalArtists");
            TempData.Remove("ShowTitle");
            return RedirectToPage();
        }
    }
}
