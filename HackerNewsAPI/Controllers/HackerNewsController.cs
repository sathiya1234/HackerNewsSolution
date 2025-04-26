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

        /// <summary>
        /// Initializes a new instance of the <see cref="HackerNewsController"/> class.
        /// </summary>
        /// <param name="hackerNewsService">Service to interact with Hacker News data.</param>
        public HackerNewsController(IHackerNewsService hackerNewsService)
        {
            _hackerNewsService = hackerNewsService;
        }

        /// <summary>
        /// Retrieves the newest Hacker News stories with pagination support.
        /// </summary>
        /// <param name="page">The page number to retrieve (1-based index).</param>
        /// <param name="pageSize">The number of stories to include per page.</param>
        /// <returns>An <see cref="ActionResult{TValue}"/> containing a <see cref="StoryModel"/> with the list of stories and total count.</returns>
        [HttpGet]
        public async Task<ActionResult<StoryModel>> GetNewestStories(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Page and pageSize must be greater than 0.");
            }

            var storyModel = await _hackerNewsService.GetNewestStories(page, pageSize);
            return Ok(storyModel);
        }

        /// <summary>
        /// Searches Hacker News stories by a term in their titles.
        /// </summary>
        /// <param name="term">The search term to find in story titles.</param>
        /// <returns>An <see cref="ActionResult{TValue}"/> containing a collection of matching <see cref="Story"/> objects.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Story>>> SearchStories(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest("Search term is required.");
            }

            var stories = await _hackerNewsService.SearchStories(term);
            return Ok(stories);
        }
    }
}
