using System.ComponentModel.DataAnnotations;

namespace NinetiesWebApp.Models
{
    public class UserProfile
    {
        [Key]
        public string UserId { get; set; } = null!; // Required, primary key
        public string? DisplayName { get; set; } // Custom display name
        public string? BackgroundColor { get; set; } // Background color (e.g., hex code)
        public string? Font { get; set; } // Font choice
        public string? AvatarImage { get; set; }
        public string? CurrentMood { get; set; } // New: e.g., "Totally Rad"
        public string? MoodEmoticon { get; set; } // New: e.g., ":-D"


    }
}
