using System;
using System.ComponentModel.DataAnnotations;
using NinetiesWebApp.Models;

namespace NinetiesWebApp.Models
{
    public class GuestbookEntry
    {
        public int Id { get; set; }

        [Required]
        public string ProfileUserId { get; set; }
        public UserProfile ProfileUser { get; set; }

        [Required]
        public string SenderUserId { get; set; }
        public UserProfile SenderUser { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Keep it short, dude! 500 chars max.")]
        public string Message { get; set; }

        public string Emoticon { get; set; }

        [Required]
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    }
}
