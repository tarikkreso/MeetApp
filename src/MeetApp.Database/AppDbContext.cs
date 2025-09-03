using MeetApp.Database.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace MeetApp.Database
{

    public class AppDbContext : IdentityDbContext<User, Role, Guid>
    {

        public virtual DbSet<Activity> Activities { get; set; }

        public virtual DbSet<ActivityMessage> ActivityMessages { get; set; }

        public virtual DbSet<Offer> Offers { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<UserActivity>( e =>
            {
                e.HasKey(x => new { x.ActivityId, x.UserId });

                e.HasOne(x => x.Activity)
                 .WithMany(a => a.Users)
                 .HasForeignKey(x => x.ActivityId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.User)
                 .WithMany(u => u.Activities)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions) : base(dbContextOptions) { }
        
    }

}
