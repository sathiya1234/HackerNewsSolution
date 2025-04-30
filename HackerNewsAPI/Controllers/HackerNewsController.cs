using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsAPI.Controllers
{
    /// <summary>
    /// API controller for retrieving and searching Hacker News stories.
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HackerNewsController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;
        private readonly ILogger<HackerNewsController> _logger;

        public HackerNewsController(IHackerNewsService hackerNewsService, ILogger<HackerNewsController> logger)
        {
            _hackerNewsService = hackerNewsService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves paginated newest stories with optional search filtering
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="searchTerm">Optional title search term</param>
        /// <returns>Paginated story results</returns>
        [HttpGet]
        public async Task<ActionResult<StoryModel>> GetNewestStories(int page, int pageSize, string? searchTerm = null)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Page and pageSize must be greater than 0" });

            try
            {
                var result = await _hackerNewsService.GetNewestStories(page, pageSize, searchTerm);

                if (result.Stories.Count == 0)
                    return NotFound(new { message = "No stories found." });

                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching stories from external API.");
                return StatusCode(503, new { message = "Failed to retrieve data from Hacker News API." });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout occurred while retrieving stories.");
                return StatusCode(504, new { message = "The request timed out while retrieving stories." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error occurred.");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}
