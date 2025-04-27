using NinetiesWebApp.Data;
using NinetiesWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;


/* Nineties Web App
 * Group Project 2
 * Kara Crumpton, Brandon Hines, Caleb Thompson
 */


namespace NinetiesWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public List<UserProfile> AllUsers { get; set; } = new List<UserProfile>();
        public string CurrentUserId { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            CurrentUserId = user?.Id;

            AllUsers = await _context.UserProfiles.ToListAsync(); // Load all user profiles
        }
    }
}
