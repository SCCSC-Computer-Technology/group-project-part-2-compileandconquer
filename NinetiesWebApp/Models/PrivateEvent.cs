using System;

namespace NinetiesWebApp.Models
{
    public class PrivateEvent
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string GoogleEventId { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
