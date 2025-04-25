using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsAPI.Controllers
{
    /// <summary>
    /// API controller for interacting with Hacker News stories.
    /// Provides endpoints to retrieve the newest stories and perform search operations.
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HackerNewsController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HackerNewsController"/> class.
        /// </summary>
        /// <param name="hackerNewsService">Service used to fetch and manage Hacker News stories.</param>
        public HackerNewsController(IHackerNewsService hackerNewsService)
        {
            _hackerNewsService = hackerNewsService;
        }

        /// <summary>
        /// Retrieves the newest Hacker News stories with pagination support.
        /// </summary>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Number of stories to include per page.</param>
        /// <returns>List of the newest stories for the requested page.</returns>
        /// <response code="200">Returns the list of stories.</response>
        /// <response code="400">If the input parameters are invalid.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoryModel>>> GetNewestStories(int page, int pageSize)
        {
            var stories = await _hackerNewsService.GetNewestStories(page, pageSize);
            return Ok(stories);
        }

        /// <summary>
        /// Searches Hacker News stories by a term in the title.
        /// </summary>
        /// <param name="term">Search term to look for in story titles.</param>
        /// <returns>List of stories containing the search term in their title.</returns>
        /// <response code="200">Returns the list of matching stories.</response>
        /// <response code="400">If the search term is null or empty.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoryModel>>> SearchStories(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest("Search term is required");
            }

            var stories = await _hackerNewsService.SearchStories(term);
            return Ok(stories);
        }
    }
}
