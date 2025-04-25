using HackerNewsAPI.Core.Models;

namespace HackerNewsAPI.Core.Interfaces
{
    /// <summary>
    /// Provides methods to interact with the Hacker News API
    /// </summary>
    public interface IHackerNewsService
    {
        /// <summary>
        /// Retrieves the newest stories from Hacker News with pagination support
        /// </summary>
        /// <param name="page">The page number to retrieve (1-based index)</param>
        /// <param name="pageSize">The number of stories per page</param>
        /// <returns>A list of stories for the requested page</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when page or pageSize is less than 1</exception>
        /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
        Task<IEnumerable<StoryModel>> GetNewestStories(int page, int pageSize);

        /// <summary>
        /// Searches stories by matching the search term against story titles
        /// </summary>
        /// <param name="searchTerm">The term to search for in story titles</param>
        /// <returns>A list of stories containing the search term in their title</returns>
        /// <exception cref="ArgumentException">Thrown when searchTerm is null or whitespace</exception>
        /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
        Task<IEnumerable<StoryModel>> SearchStories(string searchTerm);
    }
}
