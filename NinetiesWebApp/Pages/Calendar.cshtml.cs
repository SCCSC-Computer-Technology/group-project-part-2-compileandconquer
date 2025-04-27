using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Identity;
using NinetiesWebApp.Data;
using NinetiesWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2.Requests;

namespace NinetiesWebApp.Pages
{
    public class CalendarModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CalendarModel(UserManager<IdentityUser> userManager, ApplicationDbContext context, IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
        }

        public string UserName { get; set; }
        public bool IsGoogleAuthenticated { get; set; }
        public string AuthUrl { get; set; }
        public List<EventViewModel> Events { get; set; } = new();
        public string OnThisDay { get; set; }

        public class EventViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public bool IsShared { get; set; }
            public string CreatedByUserName { get; set; }
            public EventResponse UserResponse { get; set; }
        }

        public async Task OnGetAsync(string code = null)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(User);
            UserName = user.UserName;

            // Check Google Calendar authentication
            var credential = await GetUserCredentialAsync(user.Id, code);
            IsGoogleAuthenticated = credential != null;

            if (!IsGoogleAuthenticated && code == null)
            {
                try
                {
                    var clientId = _configuration["Google:ClientId"];
                    var clientSecret = _configuration["Google:ClientSecret"];
                    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                    {
                        throw new InvalidOperationException("Google ClientId or ClientSecret is missing in configuration.");
                    }

                    var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = clientId,
                            ClientSecret = clientSecret
                        },
                        Scopes = new[] { CalendarService.Scope.Calendar },
                        DataStore = new FileDataStore("GoogleCalendarTokens", true)
                    });

                    var redirectUri = Url.Page("/Calendar", pageHandler: "Callback", values: new { }, protocol: Request.Scheme);

                    if (string.IsNullOrEmpty(redirectUri))
                    {
                        throw new InvalidOperationException("Failed to generate redirect URI.");
                    }

                    Console.WriteLine($"Redirect URI: {redirectUri}");

                    var authorizationUrl = new GoogleAuthorizationCodeRequestUrl(new Uri("https://accounts.google.com/o/oauth2/auth"))
                    {
                        ClientId = clientId,
                        RedirectUri = redirectUri,
                        Scope = CalendarService.Scope.Calendar,
                        AccessType = "offline",
                        ResponseType = "code"
                    };

                    AuthUrl = authorizationUrl.Build().ToString();

                    // Load shared events
                    var sharedEvents = await _context.Events
                        .Include(e => e.CreatedByUser)
                        .Select(e => new EventViewModel
                        {
                            Id = e.Id,
                            Title = e.Title,
                            StartTime = e.StartTime,
                            EndTime = e.EndTime,
                            IsShared = true,
                            CreatedByUserName = e.CreatedByUser.UserName,
                            UserResponse = e.EventResponses.FirstOrDefault(r => r.UserId == user.Id)
                        })
                        .Where(e => e.StartTime >= DateTime.UtcNow)
                        .ToListAsync();

                    // Load private events if authenticated
                    var privateEvents = new List<EventViewModel>();
                    if (IsGoogleAuthenticated)
                    {
                        privateEvents = await _context.PrivateEvents
                            .Where(e => e.UserId == user.Id)
                            .Select(e => new EventViewModel
                            {
                                Id = e.Id,
                                Title = e.Title,
                                StartTime = e.StartTime,
                                EndTime = e.EndTime,
                                IsShared = false,
                                CreatedByUserName = user.UserName
                            })
                            .Where(e => e.StartTime >= DateTime.UtcNow)
                            .ToListAsync();
                    }

                    Events = sharedEvents.Concat(privateEvents).OrderBy(e => e.StartTime).ToList();

                    // Load On This Day
                    var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/data/nineties_events.json");
                    if (System.IO.File.Exists(jsonPath))
                    {
                        var json = await System.IO.File.ReadAllTextAsync(jsonPath);
                        var ninetiesEvents = JsonSerializer.Deserialize<List<NinetiesEvent>>(json);
                        var today = DateTime.Today.ToString("yyyy-MM-dd");
                        OnThisDay = ninetiesEvents.FirstOrDefault(e => e.Date == today)?.Description;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in OnGetAsync: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task<IActionResult> OnGetCallbackAsync(string code)
        {
            return RedirectToPage(new { code });
        }

        public async Task<IActionResult> OnPostCreateEventAsync(string Title, DateTime StartTime, DateTime EndTime, bool IsShared)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var user = await _userManager.GetUserAsync(User);

            if (IsShared)
            {
                var service = GetSharedCalendarService();
                var newEvent = new Google.Apis.Calendar.v3.Data.Event
                {
                    Summary = Title,
                    Start = new EventDateTime { DateTime = StartTime.ToUniversalTime() },
                    End = new EventDateTime { DateTime = EndTime.ToUniversalTime() }
                };

                var eventRequest = service.Events.Insert(newEvent, _configuration["Google:SharedCalendarId"]);
                var createdEvent = await eventRequest.ExecuteAsync();

                var dbEvent = new NinetiesWebApp.Models.Event
                {
                    GoogleEventId = createdEvent.Id,
                    Title = Title,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    CreatedByUserId = user.Id
                };
                _context.Events.Add(dbEvent);
            }
            else
            {
                var credential = await GetUserCredentialAsync(user.Id, null);
                if (credential == null)
                {
                    return RedirectToPage();
                }

                var service = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "NinetiesWebApp"
                });

                var newEvent = new Google.Apis.Calendar.v3.Data.Event
                {
                    Summary = Title,
                    Start = new EventDateTime { DateTime = StartTime.ToUniversalTime() },
                    End = new EventDateTime { DateTime = EndTime.ToUniversalTime() }
                };

                var eventRequest = service.Events.Insert(newEvent, "primary");
                var createdEvent = await eventRequest.ExecuteAsync();

                var privateEvent = new PrivateEvent
                {
                    UserId = user.Id,
                    GoogleEventId = createdEvent.Id,
                    Title = Title,
                    StartTime = StartTime,
                    EndTime = EndTime
                };
                _context.PrivateEvents.Add(privateEvent);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRespondAsync(int EventId, bool Accepted)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var user = await _userManager.GetUserAsync(User);
            var existingResponse = await _context.EventResponses
                .FirstOrDefaultAsync(r => r.EventId == EventId && r.UserId == user.Id);

            if (existingResponse == null)
            {
                var response = new EventResponse
                {
                    UserId = user.Id,
                    EventId = EventId,
                    Accepted = Accepted
                };
                _context.EventResponses.Add(response);
            }
            else
            {
                existingResponse.Accepted = Accepted;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetSignOutGoogleAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var user = await _userManager.GetUserAsync(User);
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _configuration["Google:ClientId"],
                    ClientSecret = _configuration["Google:ClientSecret"]
                },
                Scopes = new[] { CalendarService.Scope.Calendar },
                DataStore = new FileDataStore("GoogleCalendarTokens", true)
            });
            await flow.DeleteTokenAsync(user.Id, CancellationToken.None);
            return RedirectToPage();
        }

        private CalendarService GetSharedCalendarService()
        {
            var credential = GoogleCredential.FromFile(_configuration["Google:ServiceAccountCredentialsPath"])
                .CreateScoped(CalendarService.Scope.Calendar);
            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "NinetiesWebApp"
            });
        }

        private async Task<UserCredential> GetUserCredentialAsync(string userId, string code)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _configuration["Google:ClientId"],
                    ClientSecret = _configuration["Google:ClientSecret"]
                },
                Scopes = new[] { CalendarService.Scope.Calendar },
                DataStore = new FileDataStore("GoogleCalendarTokens", true)
            });

            try
            {
                if (code != null)
                {
                    var redirectUri = Url.Page("/Calendar", pageHandler: "Callback", values: new { }, protocol: Request.Scheme);
                    if (string.IsNullOrEmpty(redirectUri))
                    {
                        throw new InvalidOperationException("Failed to generate redirect URI for token exchange.");
                    }

                    var token = await flow.ExchangeCodeForTokenAsync(userId, code, redirectUri, CancellationToken.None);
                    return new UserCredential(flow, userId, token);
                }

                var existingToken = await flow.LoadTokenAsync(userId, CancellationToken.None);
                if (existingToken != null)
                {
                    return new UserCredential(flow, userId, existingToken);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserCredentialAsync: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private class NinetiesEvent
        {
            public string Date { get; set; }
            public string Description { get; set; }
        }
    }
}
