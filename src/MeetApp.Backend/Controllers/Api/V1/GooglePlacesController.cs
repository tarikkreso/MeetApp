using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MeetApp.Backend.Controllers.Api.V1
{
    [AllowAnonymous]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    [Route("/api/v1/places")]
    public class GooglePlacesController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private const string GOOGLE_API_KEY = "AIzaSyC3TAQZDGGcLAOE0j58E1ooqRYv_Tq0M7Y";

        public GooglePlacesController(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpGet("autocomplete")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAutocompleteAsync([FromQuery] string input, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 3)
            {
                return Ok(new { predictions = new object[0] });
            }

            // Create cache key based on input (normalized to lowercase for consistency)
            var cacheKey = $"places_autocomplete_{input.ToLowerInvariant().Trim()}";
            
            // Check if result is already cached
            if (_cache.TryGetValue(cacheKey, out var cachedResult) && cachedResult is string cachedContent)
            {
                return Content(cachedContent, "application/json");
            }

            try
            {
                var encodedInput = Uri.EscapeDataString(input);
                var url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={encodedInput}&key={GOOGLE_API_KEY}&types=address";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    // Cache the result for 10 minutes
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                        SlidingExpiration = TimeSpan.FromMinutes(5), // Refresh cache if accessed within 5 minutes
                        Priority = CacheItemPriority.Normal
                    };
                    
                    _cache.Set(cacheKey, content, cacheOptions);
                    
                    return Content(content, "application/json");
                }
                else
                {
                    return BadRequest(new { error = "Failed to fetch places data" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}
