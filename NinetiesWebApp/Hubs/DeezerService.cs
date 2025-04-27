using NinetiesWebApp.Models;
using System.Text.Json;

namespace NinetiesWebApp.Hubs
{
    public class DeezerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.deezer.com";
        private readonly Random _random = new Random();

        public DeezerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DeezerPlaylist> Get90sPlaylistAsync(long playlistId)
        {
            // Fetch a specific 90s playlist from Deezer
            var response = await _httpClient.GetAsync($"{_baseUrl}/playlist/{playlistId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var playlistData = JsonDocument.Parse(content).RootElement;

            var playlist = new DeezerPlaylist
            {
                Id = playlistData.GetProperty("id").GetInt64(),
                Title = playlistData.GetProperty("title").GetString(),
                Tracks = new List<DeezerTrack>()
            };

            // Get the tracks
            var tracksResponse = await _httpClient.GetAsync($"{_baseUrl}/playlist/{playlistId}/tracks");
            tracksResponse.EnsureSuccessStatusCode();

            var tracksContent = await tracksResponse.Content.ReadAsStringAsync();
            var tracksData = JsonDocument.Parse(tracksContent).RootElement;

            foreach (var track in tracksData.GetProperty("data").EnumerateArray())
            {
                // Only include tracks with preview URLs
                var previewUrl = track.GetProperty("preview").GetString();
                if (!string.IsNullOrEmpty(previewUrl))
                {
                    var album = track.GetProperty("album");
                    var artist = track.GetProperty("artist");

                    playlist.Tracks.Add(new DeezerTrack
                    {
                        Id = track.GetProperty("id").GetInt64(),
                        Title = track.GetProperty("title").GetString(),
                        Artist = artist.GetProperty("name").GetString(),
                        Album = album.GetProperty("title").GetString(),
                        PreviewUrl = previewUrl,
                        AlbumCover = album.GetProperty("cover_medium").GetString(),
                        Duration = track.GetProperty("duration").GetInt32()
                    });
                }
            }

            return playlist;
        }

        public async Task<List<string>> GetAdditionalArtistsAsync(int count)
        {
            // Get some random 90s artists for wrong answers
            var artists = new List<string>();
            var response = await _httpClient.GetAsync($"{_baseUrl}/chart/0/artists");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var artistsData = JsonDocument.Parse(content).RootElement;

            foreach (var artist in artistsData.GetProperty("data").EnumerateArray())
            {
                artists.Add(artist.GetProperty("name").GetString());
                if (artists.Count >= count)
                    break;
            }

            return artists;
        }

        public async Task<SongGame> CreateGameAsync(int numberOfSongs = 10)
        {
            // Some popular 90s playlists on Deezer
            long[] ninetyPlaylistIds = { 878989033, 1026896351, 2322259622 }; // Playlist IDs
            var playlistId = ninetyPlaylistIds[_random.Next(ninetyPlaylistIds.Length)];

            var playlist = await Get90sPlaylistAsync(playlistId);
            var tracks = playlist.Tracks.OrderBy(x => _random.Next()).Take(numberOfSongs).ToList();
            var additionalArtists = await GetAdditionalArtistsAsync(30);

            var game = new SongGame { Tracks = tracks };

            // Create possible answers for first question
            UpdatePossibleAnswers(game, additionalArtists);

            return game;
        }

        public void UpdatePossibleAnswers(SongGame game, List<string> additionalArtists)
        {
            if (game.IsGameOver)
                return;

            game.PossibleAnswers.Clear();
            game.PossibleAnswers.Add(game.CurrentTrack.Artist); // Correct answer

            // Add 3 wrong answers
            var wrongAnswers = additionalArtists
                .Where(a => a != game.CurrentTrack.Artist)
                .OrderBy(x => _random.Next())
                .Take(3);

            game.PossibleAnswers.AddRange(wrongAnswers);

            // Shuffle answers
            game.PossibleAnswers = game.PossibleAnswers.OrderBy(x => _random.Next()).ToList();
        }
    }
}
    

