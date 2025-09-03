using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MeetApp.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeetApp.Database.SeedData
{
    public class DatabaseSeeder
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            AppDbContext context,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding...");
                
                // Ensure database is created
                await _context.Database.MigrateAsync();

                // Seed roles first
                await SeedRolesAsync();
                
                // Seed users
                await SeedUsersAsync();
                
                // Seed offers
                await SeedOffersAsync();
                
                // Seed activities
                await SeedActivitiesAsync();
                
                // Seed user activities (join users to activities)
                await SeedUserActivitiesAsync();
                
                // Seed activity messages
                await SeedActivityMessagesAsync();
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { "Admin", "User", "Business" };
            
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new Role { Name = roleName };
                    await _roleManager.CreateAsync(role);
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
            }
        }

        private async Task SeedUsersAsync()
        {
            if (_context.Users.Any())
            {
                _logger.LogInformation("Users already exist, skipping user seeding.");
                return;
            }

            var users = new[]
            {
                // Student users
                new User
                {
                    UserName = "john.doe@student.com",
                    Email = "john.doe@student.com",
                    Name = "John Doe",
                    Type = User.UserType.Student,
                    City = "New York",
                    RegisterDateTime = DateTimeOffset.UtcNow.AddDays(-30),
                    EmailConfirmed = true
                },
                new User
                {
                    UserName = "jane.smith@student.com",
                    Email = "jane.smith@student.com",
                    Name = "Jane Smith",
                    Type = User.UserType.Student,
                    City = "Los Angeles",
                    RegisterDateTime = DateTimeOffset.UtcNow.AddDays(-25),
                    EmailConfirmed = true
                },
                new User
                {
                    UserName = "mike.johnson@student.com",
                    Email = "mike.johnson@student.com",
                    Name = "Mike Johnson",
                    Type = User.UserType.Student,
                    City = "Chicago",
                    RegisterDateTime = DateTimeOffset.UtcNow.AddDays(-20),
                    EmailConfirmed = true
                },
                new User
                {
                    UserName = "sarah.wilson@student.com",
                    Email = "sarah.wilson@student.com",
                    Name = "Sarah Wilson",
                    Type = User.UserType.Student,
                    City = "Miami",
                    RegisterDateTime = DateTimeOffset.UtcNow.AddDays(-15),
                    EmailConfirmed = true
                },
                
                // Business users
                new User
                {
                    UserName = "pizza.palace@business.com",
                    Email = "pizza.palace@business.com",
                    Name = "Mario Rossi",
                    Type = User.UserType.Bussines,
                    City = "New York",
                    BussinesName = "Pizza Palace",
                    BussinesAddress = "123 Main St, New York, NY 10001",
                    BussinesCategory = User.BussinesCategoryType.FoodAndDrink,
                    CIF = "US123456789",
                    GoogleMapsUrl = "https://goo.gl/maps/example1",
                    Latitude = 40.7128m,
                    Longitude = -74.0060m,
                    RegisterDateTime = DateTimeOffset.UtcNow.AddDays(-45),
                    EmailConfirmed = true
                },
                new User
                {
                    UserName = "cinema.central@business.com",
                    Email = "cinema.central@business.com",
                    Name = "Lisa Brown",
                    Type = User.UserType.Bussines,
                    City = "Los Angeles",
                    BussinesName = "Central Cinema",
                    BussinesAddress = "456 Hollywood Blvd, Los Angeles, CA 90028",
                    BussinesCategory = User.BussinesCategoryType.Cinema,
                    CIF = "US987654321",
                    GoogleMapsUrl = "https://goo.gl/maps/example2",
                    Latitude = 34.0522m,
                    Longitude = -118.2437m,
                    RegisterDateTime = DateTimeOffset.UtcNow.AddDays(-40),
                    EmailConfirmed = true
                },
                new User
                {
                    UserName = "burger.junction@business.com",
                    Email = "burger.junction@business.com",
                    Name = "David Miller",
                    Type = User.UserType.Bussines,
                    City = "Chicago",
                    BussinesName = "Burger Junction",
                    BussinesAddress = "789 State St, Chicago, IL 60604",
                    BussinesCategory = User.BussinesCategoryType.FoodAndDrink,
                    CIF = "US456789123",
                    GoogleMapsUrl = "https://goo.gl/maps/example3",
                    Latitude = 41.8781m,
                    Longitude = -87.6298m,
                    RegisterDateTime = DateTimeOffset.UtcNow.AddDays(-35),
                    EmailConfirmed = true
                }
            };

            foreach (var user in users)
            {
                var result = await _userManager.CreateAsync(user, "1234");
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created user: {Email}", user.Email);
                }
                else
                {
                    _logger.LogError("Failed to create user {Email}: {Errors}", 
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task SeedOffersAsync()
        {
            if (_context.Offers.Any())
            {
                _logger.LogInformation("Offers already exist, skipping offer seeding.");
                return;
            }

            var businessUsers = await _context.Users
                .Where(u => u.Type == User.UserType.Bussines)
                .ToListAsync();

            if (!businessUsers.Any())
            {
                _logger.LogWarning("No business users found, skipping offer seeding.");
                return;
            }

            var offers = new List<Offer>();
            
            foreach (var business in businessUsers)
            {
                if (business.BussinesCategory == User.BussinesCategoryType.FoodAndDrink)
                {
                    offers.AddRange(new[]
                    {
                        new Offer
                        {
                            Id = Guid.NewGuid(),
                            BusinessId = business.Id,
                            Title = "Happy Hour Special",
                            Description = "50% off all drinks from 5-7 PM",
                            ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                            Paid = false,
                            Tag = "drinks"
                        },
                        new Offer
                        {
                            Id = Guid.NewGuid(),
                            BusinessId = business.Id,
                            Title = "Student Discount",
                            Description = "20% off with valid student ID",
                            ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(60)),
                            Paid = true,
                            Tag = "student"
                        }
                    });
                }
                else if (business.BussinesCategory == User.BussinesCategoryType.Cinema)
                {
                    offers.AddRange(new[]
                    {
                        new Offer
                        {
                            Id = Guid.NewGuid(),
                            BusinessId = business.Id,
                            Title = "Tuesday Movie Night",
                            Description = "All tickets $5 on Tuesdays",
                            ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(45)),
                            Paid = false,
                            Tag = "movies"
                        },
                        new Offer
                        {
                            Id = Guid.NewGuid(),
                            BusinessId = business.Id,
                            Title = "Popcorn Combo Deal",
                            Description = "Buy one large popcorn, get one free",
                            ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(20)),
                            Paid = true,
                            Tag = "food"
                        }
                    });
                }
            }

            _context.Offers.AddRange(offers);
            _logger.LogInformation("Added {Count} offers", offers.Count);
        }

        private async Task SeedActivitiesAsync()
        {
            if (_context.Activities.Any())
            {
                _logger.LogInformation("Activities already exist, skipping activity seeding.");
                return;
            }

            var users = await _context.Users.ToListAsync();
            var offers = await _context.Offers.ToListAsync();

            if (!users.Any())
            {
                _logger.LogWarning("No users found, skipping activity seeding.");
                return;
            }

            var activities = new[]
            {
                new Activity
                {
                    Id = Guid.NewGuid(),
                    OwnerId = users.First(u => u.Type == User.UserType.Student).Id,
                    Title = "Study Group - Computer Science",
                    Description = "Weekly study group for computer science students. We'll be covering algorithms and data structures.",
                    DateTime = DateTimeOffset.UtcNow.AddDays(7),
                    PeopleLimit = 8,
                    Location = "Central Library, Room 201",
                    Latitude = 40.7589m,
                    Longitude = -73.9851m
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    OwnerId = users.First(u => u.Type == User.UserType.Student).Id,
                    Title = "Movie Night - Sci-Fi Marathon",
                    Description = "Join us for a night of classic sci-fi movies! Snacks and drinks provided.",
                    DateTime = DateTimeOffset.UtcNow.AddDays(3),
                    PeopleLimit = 15,
                    Location = "Student Center Auditorium",
                    Latitude = 40.7505m,
                    Longitude = -73.9934m,
                    OfferId = offers.FirstOrDefault(o => o.Tag == "movies")?.Id
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    OwnerId = users.Skip(1).First(u => u.Type == User.UserType.Student).Id,
                    Title = "Basketball Pickup Game",
                    Description = "Casual basketball game, all skill levels welcome. Bring your own water bottle.",
                    DateTime = DateTimeOffset.UtcNow.AddDays(2),
                    PeopleLimit = 10,
                    Location = "University Recreation Center",
                    Latitude = 40.7614m,
                    Longitude = -73.9776m
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    OwnerId = users.First(u => u.Type == User.UserType.Bussines).Id,
                    Title = "Pizza Making Workshop",
                    Description = "Learn to make authentic Italian pizza from scratch! All ingredients provided.",
                    DateTime = DateTimeOffset.UtcNow.AddDays(10),
                    PeopleLimit = 12,
                    Location = "Pizza Palace Kitchen",
                    Latitude = 40.7128m,
                    Longitude = -74.0060m,
                    OfferId = offers.FirstOrDefault(o => o.Tag == "student")?.Id
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    OwnerId = users.Skip(2).First(u => u.Type == User.UserType.Student).Id,
                    Title = "Photography Walk",
                    Description = "Explore the city and capture beautiful moments. Bring your camera or smartphone.",
                    DateTime = DateTimeOffset.UtcNow.AddDays(5),
                    PeopleLimit = 20,
                    Location = "Central Park Main Entrance",
                    Latitude = 40.7829m,
                    Longitude = -73.9654m
                }
            };

            _context.Activities.AddRange(activities);
            _logger.LogInformation("Added {Count} activities", activities.Length);
        }

        private async Task SeedUserActivitiesAsync()
        {
            if (_context.UserActivities.Any())
            {
                _logger.LogInformation("User activities already exist, skipping user activity seeding.");
                return;
            }

            var users = await _context.Users.Where(u => u.Type == User.UserType.Student).ToListAsync();
            var activities = await _context.Activities.ToListAsync();

            if (!users.Any() || !activities.Any())
            {
                _logger.LogWarning("No users or activities found, skipping user activity seeding.");
                return;
            }

            var userActivities = new List<UserActivity>();
            var random = new Random();

            foreach (var activity in activities)
            {
                // Owner is always a creator
                userActivities.Add(new UserActivity
                {
                    ActivityId = activity.Id,
                    UserId = activity.OwnerId,
                    JoinedAt = activity.DateTime.AddDays(-random.Next(1, 5)).DateTime,
                    UserRole = UserActivity.Role.Creator
                });

                // Add some random members to each activity
                var participantCount = random.Next(2, Math.Min(users.Count, (int)(activity.PeopleLimit ?? 5)));
                var availableUsers = users.Where(u => u.Id != activity.OwnerId).ToList();
                
                for (int i = 0; i < participantCount && i < availableUsers.Count; i++)
                {
                    var user = availableUsers[random.Next(availableUsers.Count)];
                    
                    // Avoid duplicates
                    if (!userActivities.Any(ua => ua.ActivityId == activity.Id && ua.UserId == user.Id))
                    {
                        userActivities.Add(new UserActivity
                        {
                            ActivityId = activity.Id,
                            UserId = user.Id,
                            JoinedAt = activity.DateTime.AddDays(-random.Next(1, 3)).DateTime,
                            UserRole = UserActivity.Role.Member
                        });
                    }
                }
            }

            _context.UserActivities.AddRange(userActivities);
            _logger.LogInformation("Added {Count} user activities", userActivities.Count);
        }

        private async Task SeedActivityMessagesAsync()
        {
            if (_context.ActivityMessages.Any())
            {
                _logger.LogInformation("Activity messages already exist, skipping message seeding.");
                return;
            }

            var userActivities = await _context.UserActivities
                .Include(ua => ua.User)
                .Include(ua => ua.Activity)
                .ToListAsync();

            if (!userActivities.Any())
            {
                _logger.LogWarning("No user activities found, skipping message seeding.");
                return;
            }

            var messages = new List<ActivityMessage>();
            var sampleMessages = new[]
            {
                "Looking forward to this event!",
                "What should I bring?",
                "Is parking available nearby?",
                "Count me in!",
                "This sounds amazing!",
                "Can't wait to meet everyone!",
                "Will there be refreshments?",
                "Thanks for organizing this!",
                "Perfect timing for me.",
                "See you all there!"
            };

            var random = new Random();
            var activitiesGrouped = userActivities.GroupBy(ua => ua.ActivityId);

            foreach (var activityGroup in activitiesGrouped)
            {
                var activityId = activityGroup.Key;
                var participants = activityGroup.ToList();
                
                // Add 2-5 messages per activity
                var messageCount = random.Next(2, 6);
                
                for (int i = 0; i < messageCount; i++)
                {
                    var participant = participants[random.Next(participants.Count)];
                    var messageText = sampleMessages[random.Next(sampleMessages.Length)];
                    
                    messages.Add(new ActivityMessage
                    {
                        Id = Guid.NewGuid(),
                        ActivityId = activityId,
                        UserId = participant.UserId,
                        Message = messageText,
                        Timestamp = participant.Activity.DateTime.AddDays(-random.Next(0, 3)).AddHours(-random.Next(0, 12))
                    });
                }
            }

            _context.ActivityMessages.AddRange(messages);
            _logger.LogInformation("Added {Count} activity messages", messages.Count);
        }
    }
}