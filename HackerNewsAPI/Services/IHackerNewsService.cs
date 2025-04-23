using HackerNewsAPI.Models;

namespace HackerNewsAPI.Services
{
    public interface IHackerNewsService
    {
        Task<IEnumerable<StoryModel>> GetNewestStories(int page, int pageSize);
        Task<IEnumerable<StoryModel>> SearchStories(string searchTerm);
    }
}
