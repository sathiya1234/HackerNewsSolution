using HackerNewsAPI.Core.Models;

namespace HackerNewsAPI.Core.Interfaces
{
    /// <summary>
    /// Defines methods for interacting with Hacker News stories.
    /// </summary>
    public interface IHackerNewsService
    {
        /// <summary>
        /// Retrieves a paginated list of the newest stories from Hacker News.
        /// </summary>
        /// <param name="page">The page number to retrieve (starting from 1).</param>
        /// <param name="pageSize">The number of stories per page.</param>
        /// <returns>A <see cref="StoryModel"/> containing the stories and total count.</returns>
        Task<StoryModel> GetNewestStories(int page, int pageSize);

        /// <summary>
        /// Searches stories by a specified search term in their titles.
        /// </summary>
        /// <param name="searchTerm">The keyword or phrase to search for in story titles.</param>
        /// <returns>A collection of <see cref="Story"/> objects that match the search term.</returns>
        Task<IEnumerable<Story>> SearchStories(string searchTerm);
    }
}
