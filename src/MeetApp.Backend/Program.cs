using Azure;
using MeetApp.Backend.Extensions;
using MeetApp.Backend.Hubs;
using MeetApp.Database;
using MeetApp.Database.Models;
using MeetApp.Database.SeedData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stripe;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MeetApp.Backend
{

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var webApplicationBuilder = WebApplication.CreateBuilder(args);
            //webApplicationBuilder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddJwtBearer();
            var jwtConfig = webApplicationBuilder.Configuration.GetSection("JwtBearer");

            webApplicationBuilder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        ValidateActor = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(jwtConfig["Secret"])
                        )
                    };
                });



            webApplicationBuilder.Services.AddAuthorization();
            webApplicationBuilder.Services.AddControllers()
                .AddJsonOptions(jsonOptions =>
                {
                    jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            webApplicationBuilder.Services.AddHttpClient(); // Add HttpClient for Google Places API
            webApplicationBuilder.Services.AddMemoryCache(); // Add Memory Cache for Google Places API results
            webApplicationBuilder.Services.AddCors(corsOptions =>
            {
                corsOptions.AddPolicy("AllowSpecificOrigins", corsPolicyBuilder =>
                {
                    corsPolicyBuilder.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithOrigins("http://localhost:5000")
                        .WithOrigins("https://localhost:5001")
                        .WithOrigins("http://localhost:5173")
                        .WithOrigins("http://localhost:5174")
                        .WithOrigins("https://meet-app-2.azurewebsites.net");
                });
            });
            
            // TODO: Uncomment when Azure Translation API key is configured
            //webApplicationBuilder.Services.AddAzureClients(azureClientFactoryBuilder =>
            //{
            //    var azureApiKey = webApplicationBuilder.Configuration["Azure:TranslationApiKey"];
            //    var azureRegion = webApplicationBuilder.Configuration["Azure:TranslationRegion"] ?? "westeurope";
            //    azureClientFactoryBuilder.AddTextTranslationClient(new AzureKeyCredential(azureApiKey), azureRegion);
            //});
            
            webApplicationBuilder.Services.AddDbContextPool<AppDbContext>(dbContextOptionsBuilder =>
            {
                // Only enable detailed errors and sensitive data logging in development
                if (webApplicationBuilder.Environment.IsDevelopment())
                {
                    dbContextOptionsBuilder.EnableDetailedErrors();
                    dbContextOptionsBuilder.EnableSensitiveDataLogging();
                }
                dbContextOptionsBuilder.UseNpgsql(
    webApplicationBuilder.Configuration.GetConnectionString(nameof(AppDbContext)),
    npgsqlOptionsAction =>
    {
        npgsqlOptionsAction.CommandTimeout(120);
        npgsqlOptionsAction.EnableRetryOnFailure();
        npgsqlOptionsAction.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });

                //dbContextOptionsBuilder.UseAzureSql(webApplicationBuilder.Configuration.GetConnectionString(nameof(AppDbContext)), npgsqlOptionsAction =>
                //{
                //    npgsqlOptionsAction.CommandTimeout(120);
                //    npgsqlOptionsAction.EnableRetryOnFailure();
                //    npgsqlOptionsAction.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                //});
            });
            webApplicationBuilder.Services.AddEndpointsApiExplorer();
            webApplicationBuilder.Services.AddIdentity<User, Role>(identityOptions =>
            {
                identityOptions.Password.RequireDigit = true;
                identityOptions.Password.RequireLowercase = true;
                identityOptions.Password.RequireNonAlphanumeric = true;
                identityOptions.Password.RequireUppercase = true;
                identityOptions.Password.RequiredLength = 8;
            })
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<AppDbContext>();
            
            // TODO: Uncomment when Stripe API key is configured
            //webApplicationBuilder.Services.AddScoped(serviceProvider =>
            //{
            //    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            //    var stripeApiKey = configuration["Stripe:SecretKey"];
            //    return new StripeClient(apiKey: stripeApiKey);
            //});

            // Register the database seeder
            // webApplicationBuilder.Services.AddScoped<DatabaseSeeder>();

            webApplicationBuilder.Services.AddSignalR();
            webApplicationBuilder.Services.AddSwaggerGen();
            var webApplication = webApplicationBuilder.Build();
            webApplication.UseSwagger();
            webApplication.UseSwaggerUI();
            //webApplication.UseHttpsRedirection();
            webApplication.UseStaticFiles();
            webApplication.UseRouting();
            webApplication.UseCors("AllowSpecificOrigins");
            webApplication.UseAuthentication();
            webApplication.UseAuthorization();
            webApplication.MapControllers();
            webApplication.MapHub<ChatHub>("/hubs/chat-hub");
            webApplication.MapFallbackToFile("index.html");

            {
                using var asyncServiceScope = webApplication.Services.CreateAsyncScope();
                var appDbContext = asyncServiceScope.ServiceProvider.GetRequiredService<AppDbContext>();
               // await appDbContext.Database.MigrateAsync();
            }

            await webApplication.RunAsync();
        }
    }
}
