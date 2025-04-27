using NinetiesWebApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace NinetiesWebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; } 
        public DbSet<GuestbookEntry> GuestbookEntries { get; set; }

        public DbSet<Event> Events { get; set; }
        public DbSet<PrivateEvent> PrivateEvents { get; set; }
        public DbSet<EventResponse> EventResponses { get; set; }
    }
}
