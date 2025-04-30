using HackerNewsAPI.Core.Models;

namespace HackerNewsAPI.Core.Interfaces
{
    /// <summary>
    /// Defines methods for interacting with Hacker News stories.
    /// </summary>
    public interface IHackerNewsService
    {
        /// <summary>
        /// Gets paginated newest stories with optional search
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="searchTerm">Optional title search term</param>
        /// <returns>StoryModel containing results and total count</returns>
        Task<StoryModel> GetNewestStories(int page, int pageSize, string? searchTerm = null);
    }
}
