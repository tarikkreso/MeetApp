using MeetApp.Database;
using MeetApp.Database.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MeetApp.Backend.Controllers.Api.V1
{

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    [Route("/api/v1/activity")]
    [Route("/api/v1/activities")]
    public class ActivityController(
        AppDbContext appDbContext
    ) : ControllerBase
    {

        private readonly AppDbContext appDbContext = appDbContext;

        //[AllowAnonymous]
        //[HttpGet]
        //[Route("/getActivitiesByDate")]
        //[Produces(MediaTypeNames.Application.Json)]
        //[ProducesResponseType<ICollection<ActivityGetResponse>>(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<IActionResult> GetActivitesByDateAsync([FromQuery]DateTime? from, [FromQuery]DateTime? to ,CancellationToken cancellationToken=default)
        //{
        //    var query = appDbContext.Activities.AsQueryable();
        //    if (from.HasValue)
        //    {
        //        query=query.Where(x => x.DateTime >= from.Value.ToUniversalTime());
        //    }
        //    if (to.HasValue)
        //    {
        //        query=query.Where(x => x.DateTime <= to.Value.ToUniversalTime());   
        //    }
        //    var activites = await query.ToListAsync(cancellationToken);
        //    return this.Ok(activites);
        //}


        [Authorize]
        [HttpDelete("leave/{activityId}")]
        public async Task<IActionResult> LeaveActivity([FromRoute] Guid activityId, CancellationToken ct=default)
        {
            var userStr = User.FindFirst(ClaimTypes.Sid)?.Value;
            if (!Guid.TryParse(userStr,out var userId))
            {
                return BadRequest();
            }
            var user = await appDbContext.Users.FindAsync(userId);
            if (user == null) { return BadRequest(); }
            var activity= await appDbContext.Activities.FindAsync(activityId);
            if (activity == null) { return BadRequest(); }

           var ua=await appDbContext.UserActivities
                .Where(x=>x.ActivityId==activity.Id && x.UserId==user.Id).FirstOrDefaultAsync();
            if (ua == null) { return BadRequest(); }
            _=appDbContext.UserActivities.Remove(ua);
            _=await appDbContext.SaveChangesAsync();
            return Ok(true);    
        }



        [AllowAnonymous]
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<ICollection<ActivityGetResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
        {
            var activity = await this.Read().ToListAsync(cancellationToken);
            return this.Ok(activity);
        }

        [AllowAnonymous]
        [HttpGet("{date}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<ActivityGetResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByDate([FromRoute][Required] DateTime date, CancellationToken cancellationToken = default)
        {
            var activities = await this.Read()
                .Where(x => x.DateTime.Date == date.Date)
                .ToListAsync(cancellationToken);
            return this.Ok(activities);
        }
        [AllowAnonymous]
        [HttpGet("date")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<ICollection<ActivityGetByDateResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ICollection<ActivityGetByDateResponse>>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetByDateAsync(DateTime dateTimeFrom, CancellationToken cancellationToken = default)
        {
            var startDate = dateTimeFrom.ToUniversalTime();
            var endDate = startDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59).ToUniversalTime();

            var results = await appDbContext.Offers.SelectMany(x => x.Activities)
                .Where(activity => activity.DateTime >= startDate && activity.DateTime <= endDate && activity.OfferId.HasValue)
                .Select(x => new ActivityGetByDateResponse
                {
                    Title = x.Title,
                    Description = x.Description,
                    DateTime = x.DateTime.ToString("o"),
                    PeopleLimit = x.PeopleLimit,
                    BusinessName = x.Offer != null ? x.Offer.Bussines.BussinesName : " "
                }).ToListAsync(cancellationToken);
            return Ok(results);
        }
        public record ActivityGetByDateResponse
        {
            public required string Title { get; init; }
            public required string Description { get; init; }
            public required string DateTime { get; init; }
            public uint? PeopleLimit { get; init; }
            public required string BusinessName { get; init; }
        }

        [AllowAnonymous]
        [Consumes(MediaTypeNames.Application.Json)]
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<ICollection<ActivityCreateRequest>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ICollection<ActivityCreateRequest>>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAsync([FromBody][Required] ActivityCreateRequest activityCreateRequest, CancellationToken cancellationToken = default)
        {
            var offer = await this.appDbContext.Offers
                .Include(x => x.Bussines)
                .Where(x => x.Id == activityCreateRequest.OfferId)
                .SingleOrDefaultAsync(cancellationToken);
            
            var activity = new Activity
            {
                OfferId = activityCreateRequest.OfferId,
                OwnerId = activityCreateRequest.OwnerId,
                Title = activityCreateRequest.Title,
                Description = activityCreateRequest.Description,
                DateTime = activityCreateRequest.DateTime,
                PeopleLimit = activityCreateRequest.PeopleLimit,
                Location = activityCreateRequest.Location,
                Latitude = activityCreateRequest.Latitude,
                Longitude = activityCreateRequest.Longitude
            };
            
            this.appDbContext.Activities.Add(activity);
            await this.appDbContext.SaveChangesAsync(cancellationToken);

            // Add the owner as a Creator
            var ownerUserActivity = new UserActivity
            {
                ActivityId = activity.Id,
                UserId = activityCreateRequest.OwnerId,
                JoinedAt = DateTime.UtcNow,
                UserRole = UserActivity.Role.Creator
            };
            this.appDbContext.UserActivities.Add(ownerUserActivity);

            if (activityCreateRequest.SelectedPeople != null && activityCreateRequest.SelectedPeople.Count != 0)
            {
                foreach (var personIdString in activityCreateRequest.SelectedPeople)
                {
                    if (Guid.TryParse(personIdString, out var personId) && personId != activityCreateRequest.OwnerId)
                    {
                        var userExists = await this.appDbContext.Users.AnyAsync(u => u.Id == personId, cancellationToken);
                        if (userExists)
                        {
                            var userActivity = new UserActivity
                            {
                                ActivityId = activity.Id,
                                UserId = personId,
                                JoinedAt = DateTime.UtcNow,
                                UserRole = UserActivity.Role.Member
                            };
                            this.appDbContext.UserActivities.Add(userActivity);
                        }
                    }
                }
            }

            await this.appDbContext.SaveChangesAsync(cancellationToken);
            
            return this.Ok(new ActivityCreateRequest
            {
                OfferId = activity.OfferId,
                OwnerId = activity.OwnerId,
                Title = activity.Title,
                Description = activity.Description,
                DateTime = activity.DateTime,
                PeopleLimit = activity.PeopleLimit,
                Location = activityCreateRequest.Location,
                Latitude = activityCreateRequest.Latitude,
                Longitude = activityCreateRequest.Longitude,
                SelectedPeople = activityCreateRequest.SelectedPeople
            });
        }

        [AllowAnonymous]
        [HttpDelete("{id}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync([FromRoute][Required] Guid id, CancellationToken cancellationToken = default)
        {
            var activity = await this.appDbContext.Activities
                .Where(x => x.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (activity is null)
            {
                return NotFound();
            }
            this.appDbContext.Activities.Remove(activity);
            _ = await this.appDbContext.SaveChangesAsync(cancellationToken);
            return this.Ok(activity);
        }

        [AllowAnonymous]
        [HttpGet("{activityid}/participants/count")]
        public async Task<IActionResult> GetCountAsync([FromRoute] string activityid, CancellationToken ct=default)
        {
            var count = await appDbContext.UserActivities.CountAsync(x => x.ActivityId.ToString() == activityid,ct);
            return Ok(count);
        }


        [Authorize]
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinActivity([FromRoute] string id, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(id, out var activityId)) 
            { 
                Console.WriteLine($"Invalid GUID format: {id}");
                return NotFound(); 
            }

            var userIdstr = User.FindFirst(ClaimTypes.Sid)?.Value;
            
            if (string.IsNullOrEmpty(userIdstr))
            {
                return Unauthorized();
            }
            var userId = Guid.Parse(userIdstr);

            var activitiy = await appDbContext.Activities
                .FirstOrDefaultAsync(x=> x.Id == activityId, cancellationToken);
            if (activitiy == null) 
            { 
                return NotFound(); 
            }

            bool isMember = await appDbContext.UserActivities
                .AnyAsync(x => x.ActivityId == activityId && x.UserId == userId, cancellationToken);
            
            if (isMember) 
            { 
                return Ok(false); 
            } 

            int countOfUsersInActivitiy = await appDbContext.UserActivities
                .CountAsync(x => x.ActivityId == activitiy.Id, cancellationToken);
            
            if (activitiy.PeopleLimit is not null 
                && countOfUsersInActivitiy + 1  > activitiy.PeopleLimit ) 
            { 
                return BadRequest("Activity is full."); 
            }

            var userActivity = new UserActivity
            {
                ActivityId = activitiy.Id,
                UserId = userId,
                JoinedAt = DateTime.Now.ToUniversalTime(),
                UserRole = UserActivity.Role.Member
            };

            appDbContext.UserActivities.Add(userActivity);

            try
            {
                await appDbContext.SaveChangesAsync(cancellationToken);
                return Ok(true); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to database: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [AllowAnonymous]
        [HttpPut("{id}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<ActivityUpdateRequest>(StatusCodes.Status200OK)]
        [ProducesResponseType<ActivityUpdateRequest>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAsync([FromRoute][Required] Guid id, [FromBody][Required] ActivityUpdateRequest activityUpdateRequest, CancellationToken cancellationToken = default)
        {
            var activity = await this.appDbContext.Activities
                .Where(x => x.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (activity is null)
            {
                return NotFound();
            }
            activity.OfferId = activityUpdateRequest.OfferId;
            activity.OwnerId = activityUpdateRequest.OwnerId;
            activity.Title = activityUpdateRequest.Title;
            activity.Description = activityUpdateRequest.Description;
            activity.DateTime = activityUpdateRequest.DateTime;
            activity.PeopleLimit = activityUpdateRequest.PeopleLimit;
            _ = await this.appDbContext.SaveChangesAsync(cancellationToken);
            return Ok(activity);
        }

        private IQueryable<ActivityGetResponse> Read()
        {
            return this.appDbContext.Activities
                .Select(x => new ActivityGetResponse()
                {
                    DateTime = x.DateTime,
                    Description = x.Description,
                    Id = x.Id,
                    Latitude = x.Latitude,
                    Location = x.Location,
                    Longitude = x.Longitude,
                    OfferId = x.OfferId,
                    OwnerId = x.OwnerId,
                    PeopleLimit = x.PeopleLimit,
                    Title = x.Title,
                    ParticipantCount = x.Users.Count()
                });
        }

        [AllowAnonymous]
        [HttpPost("checkQrCode")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckQrCodeAsync([FromBody][Required] CheckQrCodeRequest checkQrCodeRequest, CancellationToken cancellationToken = default)
        {
            // Qr is valid when the activityId exists, 
            //the activity has an offer associated, 
            //the businessId given is the same as the offfer's businessId 
            //and the offer is valid and not expired.
            // Return a Json object with the status of the Qr code (valid = true/false)
            var activity = await this.appDbContext.Activities
                .Include(x => x.Offer)
                .ThenInclude(x => x!.Bussines)
                .Where(x => x.Id == checkQrCodeRequest.ActivityId)
                .SingleOrDefaultAsync(cancellationToken);
            if (activity is null)
            {
                return NotFound("Activity does not exist");
            }
            if (activity.Offer is null)
            {
                return NotFound("Offer does not exist: " + activity.Offer);
            }
            if (activity.Offer.Bussines.Id != checkQrCodeRequest.BusinessId)
            {
                return NotFound("Offer does not belong to the business");
            }
            if (activity.Offer.ExpirationDate.ToDateTime(TimeOnly.MaxValue) < DateTimeOffset.Now)
            {
                return NotFound("Offer is expired");
            }
            return Ok("The QR code is valid");
        }

        [AllowAnonymous]
        [HttpGet("users")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType<ICollection<ActivityUserResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await this.appDbContext.Users
                .Where(u => u.Type == Database.Models.User.UserType.Student) // Only get students for activities
                .Select(u => new ActivityUserResponse
                {
                    Id = u.Id.ToString(),
                    Name = u.Name ?? u.UserName ?? "Unknown",
                    Email = u.Email ?? "No email"
                })
                .ToListAsync(cancellationToken);
            
            return this.Ok(users);
        }

        public record ActivityUserResponse
        {
            public required string Id { get; init; }
            public required string Name { get; init; }
            public required string Email { get; init; }
        }

        public record CheckQrCodeRequest
        {
            public Guid ActivityId { get; set; }
            public Guid BusinessId { get; set; }
        }

        public record ActivityUpdateRequest
        {
            public Guid? OfferId { get; set; }

            public Guid OwnerId { get; set; }

            public required string Title { get; set; }

            public required string Description { get; set; }

            public DateTimeOffset DateTime { get; set; }

            public uint? PeopleLimit { get; set; }
        }

        public record ActivityGetResponse
        {
            public required Guid Id { get; init; }

            public required Guid? OfferId { get; init; }

            public required Guid OwnerId { get; init; }

            public required string Title { get; init; }

            public required string Description { get; init; }

            public required DateTimeOffset DateTime { get; init; }

            public required uint? PeopleLimit { get; init; }

            public required string? Location { get; init; }

            public required decimal? Latitude { get; init; }

            public required decimal? Longitude { get; init; }

            public required int ParticipantCount { get; init; }

        }

        public record ActivityCreateRequest
        {
            public Guid? OfferId { get; set; }

            public Guid OwnerId { get; set; }

            public required string Title { get; set; }

            public required string Description { get; set; }

            public DateTimeOffset DateTime { get; set; }

            public uint? PeopleLimit { get; set; }

            public required string? Location { get; set; }

            public required decimal? Latitude { get; set; }

            public required decimal? Longitude { get; set; }

            public List<string>? SelectedPeople { get; set; } = new List<string>();
        }

    }

}
