using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Goweli.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProxyController> _logger;

        public ProxyController(IHttpClientFactory httpClientFactory, ILogger<ProxyController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("OpenLibraryClient");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Goweli Book Application/1.0");
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string title, [FromQuery] int limit = 1)
        {
            try
            {
                _logger.LogInformation($"Searching for book with title: {title}");

                var response = await _httpClient.GetAsync(
                    $"https://openlibrary.org/search.json?title={Uri.EscapeDataString(title)}&limit={limit}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"OpenLibrary API returned status: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, "Error communicating with OpenLibrary API");
                }

                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search proxy endpoint");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("cover/{coverEditionKey}")]
        public async Task<IActionResult> GetCover(string coverEditionKey, [FromQuery] string size = "M")
        {
            try
            {
                _logger.LogInformation($"Fetching cover for edition key: {coverEditionKey}");

                // Size can be S, M, or L
                if (!new[] { "S", "M", "L" }.Contains(size))
                {
                    size = "M"; // Default to medium if an invalid size is provided
                }

                var response = await _httpClient.GetAsync(
                    $"https://covers.openlibrary.org/b/olid/{coverEditionKey}-{size}.jpg");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"OpenLibrary API returned status: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, "Error fetching book cover");
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                return File(imageBytes, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCover proxy endpoint");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
