using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using NinetiesWebApp.Data;
using NinetiesWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;


namespace NinetiesWebApp.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfileModel> _logger; // Add logger

        public ProfileModel(UserManager<IdentityUser> userManager, ApplicationDbContext context, ILogger<ProfileModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public IdentityUser CurrentUser { get; set; }
        public UserProfile CustomProfile { get; set; }
        public List<GuestbookEntry> GuestbookEntries { get; set; } = new List<GuestbookEntry>();

        [BindProperty]
        public GuestbookInputModel GuestbookInput { get; set; }

        public class GuestbookInputModel
        {
            [Required]
            [StringLength(500, ErrorMessage = "Keep it short, dude! 500 chars max.")]
            public string Message { get; set; }

            public string Emoticon { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string userId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user found, redirecting to login.");
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            _logger.LogInformation($"Received userId: {userId}, CurrentUser.Id: {currentUser.Id}");

            var targetUserId = string.IsNullOrEmpty(userId) ? currentUser.Id : userId;
            _logger.LogInformation($"TargetUserId set to: {targetUserId}");

            var targetUser = await _userManager.FindByIdAsync(targetUserId);
            if (targetUser == null)
            {
                _logger.LogWarning($"User with ID {targetUserId} not found.");
                return NotFound($"User with ID {targetUserId} not found!");
            }

            CurrentUser = currentUser;
            CustomProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == targetUserId);
            if (CustomProfile == null)
            {
                _logger.LogInformation($"No UserProfile found for {targetUserId}, creating new.");
                CustomProfile = new UserProfile
                {
                    UserId = targetUserId,
                    DisplayName = targetUser.UserName
                };
                _context.UserProfiles.Add(CustomProfile);
                await _context.SaveChangesAsync();
            }

            GuestbookEntries = await _context.GuestbookEntries
                .Where(e => e.ProfileUserId == targetUserId)
                .Include(e => e.SenderUser)
                .OrderByDescending(e => e.PostedAt)
                .ToListAsync();

            _logger.LogInformation($"Loaded profile for {CustomProfile.DisplayName} with {GuestbookEntries.Count} guestbook entries.");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("No authenticated user found, redirecting to login.");
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            _logger.LogInformation($"Posting guestbook entry for userId: {userId}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid, reloading profile.");
                CustomProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                GuestbookEntries = await _context.GuestbookEntries
                    .Where(e => e.ProfileUserId == userId)
                    .Include(e => e.SenderUser)
                    .OrderByDescending(e => e.PostedAt)
                    .ToListAsync();
                return Page();
            }

            var entry = new GuestbookEntry
            {
                ProfileUserId = userId,
                SenderUserId = currentUser.Id,
                Message = GuestbookInput.Message,
                Emoticon = GuestbookInput.Emoticon,
                PostedAt = DateTime.UtcNow
            };

            _context.GuestbookEntries.Add(entry);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Guestbook entry saved for {userId} by {currentUser.Id}.");

            return RedirectToPage(new { userId });
        }
    }
}


