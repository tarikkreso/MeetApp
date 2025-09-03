using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using MeetApp.Database;

namespace MeetApp.Database.Internal
{

    internal class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {

        public AppDbContext CreateDbContext(string[] args)
        {
            // Build configuration to read from appsettings files
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "MeetApp.Backend"))
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("AppDbContext");
            
            // Fallback to localhost if no connection string found
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Host=localhost;Port=5432;Database=MeetApp;Username=postgres;Password=1234;SslMode=Disable";
            }

            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            dbContextOptionsBuilder.UseNpgsql(
                connectionString,
                options =>
                {
                    options.CommandTimeout(120);
                    options.EnableRetryOnFailure();
                    options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

            return new AppDbContext(dbContextOptionsBuilder.Options);
        }

    }

}
