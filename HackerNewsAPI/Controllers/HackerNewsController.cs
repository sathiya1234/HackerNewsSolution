using HackerNewsAPI.Models;
using HackerNewsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HackerNewsController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;

        public HackerNewsController(IHackerNewsService hackerNewsService)
        {
            _hackerNewsService = hackerNewsService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoryModel>>> GetNewestStories(int page, int pageSize)
        {
            var stories = await _hackerNewsService.GetNewestStories(page, pageSize);
            return Ok(stories);
        }

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