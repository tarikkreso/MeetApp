using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetApp.Database.Models
{
    public class UserActivity
    {
        public Guid ActivityId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; }
        public Role UserRole { get; set; }
        public Activity Activity { get; set; } = default!;
        public User User { get; set; } = default;

        public enum Role
        {
            Creator = 0,
            Member = 1,
        }

    }
}
