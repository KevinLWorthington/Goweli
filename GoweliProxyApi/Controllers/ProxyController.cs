using Microsoft.AspNetCore.Mvc;
using OpenLibraryNET.Loader;

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
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            // Request headers not needed
           // _httpClient.DefaultRequestHeaders.Add("User-Agent", "Goweli Book Application/1.0");
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string title, [FromQuery] int limit = 20)
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

        [HttpGet("covers")]
        public async Task<IActionResult> GetBookCovers([FromQuery] string title)
        {
            try
            {
                _logger.LogInformation($"Searching for book covers with title: {title}");

                // Search for the book title in Open Library API using OpenLibraryNET library
                var searchResults = await OLSearchLoader.GetSearchResultsAsync(
                    _httpClient,
                    title,
                    new KeyValuePair<string, string>("limit", "20")
                );

                if (searchResults == null || searchResults.Length == 0)
                {
                    return NotFound("No book covers found");
                }

                // Extract cover edition keys and cover IDs
                // Cover IDs are used when cover_edition_key is not available
                var coverSources = new List<CoverSource>();

                foreach (var result in searchResults)
                {
                    // Check for cover_edition_key
                    if (result.ExtensionData.TryGetValue("cover_edition_key", out var coverEditionKeyObj) &&
                        coverEditionKeyObj != null && !string.IsNullOrEmpty(coverEditionKeyObj.ToString()))
                    {
                        string coverEditionKey = coverEditionKeyObj.ToString();
                        coverSources.Add(new CoverSource
                        {
                            Type = "olid",
                            Key = coverEditionKey,
                            Url = $"https://covers.openlibrary.org/b/olid/{coverEditionKey}-M.jpg"
                        });
                    }
                    // Check for cover_i (ID)
                    else if (result.ExtensionData.TryGetValue("cover_i", out var coverId) &&
                            coverId != null && !string.IsNullOrEmpty(coverId.ToString()))
                    {
                        string coverIdKey = coverId.ToString();
                        coverSources.Add(new CoverSource
                        {
                            Type = "id",
                            Key = coverIdKey,
                            Url = $"https://covers.openlibrary.org/b/id/{coverIdKey}-M.jpg"
                        });
                    }
                }

                if (!coverSources.Any())
                {
                    return NotFound("No book covers found");
                }

                return Ok(coverSources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBookCovers endpoint");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("validateCover")]
        public async Task<IActionResult> ValidateCover([FromQuery] string coverUrl)
        {
            try
            {
                _logger.LogInformation($"Validating cover URL: {coverUrl}");

                var response = await _httpClient.GetAsync(coverUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Cover validation failed - URL returned status code: {response.StatusCode}");
                    return BadRequest("Invalid cover URL");
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();

                // Check if the image is valid (some minimal size)
                if (imageBytes.Length < 1000)
                {
                    _logger.LogWarning($"Cover validation failed - Image too small ({imageBytes.Length} bytes)");
                    return Ok(new { IsValid = false, ImageBytes = Array.Empty<byte>() });
                }

                _logger.LogInformation($"Cover validated successfully - {imageBytes.Length} bytes");
                return Ok(new { IsValid = true, ImageBytes = imageBytes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateCover endpoint");
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

        [HttpGet("coverById/{coverId}")]
        public async Task<IActionResult> GetCoverById(string coverId, [FromQuery] string size = "M")
        {
            try
            {
                _logger.LogInformation($"Fetching cover for ID: {coverId}");

                // Size can be S, M, or L
                if (!new[] { "S", "M", "L" }.Contains(size))
                {
                    size = "M"; // Default to medium if an invalid size is provided
                }

                var response = await _httpClient.GetAsync(
                    $"https://covers.openlibrary.org/b/id/{coverId}-{size}.jpg");

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
                _logger.LogError(ex, "Error in GetCoverById proxy endpoint");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    // Helper class for cover sources


    public class CoverSource
    {
        public string Type { get; set; } // "olid" or "id"
        public string Key { get; set; }
        public string Url { get; set; }
    }
}