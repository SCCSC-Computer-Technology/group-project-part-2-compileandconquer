namespace NinetiesWebApp.Models
{
    public class DeezerTrack
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string PreviewUrl { get; set; }
        public string AlbumCover { get; set; }
        public int Duration { get; set; }
        public int ReleaseYear { get; set; }

    }

    public class DeezerPlaylist
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public List<DeezerTrack> Tracks { get; set; }
    }

    public class SongGame
    {
        public List<DeezerTrack> Tracks { get; set; }
        public int CurrentTrackIndex { get; set; } = 0;
        public int Score { get; set; } = 0;
        public bool IsGameOver => CurrentTrackIndex >= Tracks.Count;
        public DeezerTrack CurrentTrack => IsGameOver ? null : Tracks[CurrentTrackIndex];
        public List<string> PossibleAnswers { get; set; } = new List<string>();
    }
}
