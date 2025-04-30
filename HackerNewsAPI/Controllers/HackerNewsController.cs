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

        public HackerNewsController(IHackerNewsService hackerNewsService)
        {
            _hackerNewsService = hackerNewsService;
        }

        /// <summary>
        /// Retrieves paginated newest stories with optional search filtering
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="searchTerm">Optional title search term</param>
        /// <returns>Paginated story results</returns>
        [HttpGet]
        public async Task<ActionResult<StoryModel>> GetNewestStories(int page = 1, int pageSize = 10, [FromQuery] string? searchTerm = null)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                    return BadRequest("Page and pageSize must be greater than 0");

                var result = await _hackerNewsService.GetNewestStories(page, pageSize, searchTerm);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

