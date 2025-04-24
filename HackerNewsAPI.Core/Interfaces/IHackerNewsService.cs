using HackerNewsAPI.Core.Models;

namespace HackerNewsAPI.Core.Interfaces
{
    /// <summary>
    /// Service for interacting with Hacker News API
    /// </summary>
    public interface IHackerNewsService
    {
        /// <summary>
        /// Gets the newest stories with pagination
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of stories per page</param>
        /// <returns>List of stories</returns>
        Task<IEnumerable<StoryModel>> GetNewestStories(int page, int pageSize);

        /// <summary>
        /// Searches stories by title
        /// </summary>
        /// <param name="searchTerm">Term to search in story titles</param>
        /// <returns>List of matching stories</returns>
        Task<IEnumerable<StoryModel>> SearchStories(string searchTerm);
    }
}
