using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace NinetiesWebApp.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string GoogleEventId { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string CreatedByUserId { get; set; }
        public IdentityUser CreatedByUser { get; set; }
        public List<EventResponse> EventResponses { get; set; }
    }
}
