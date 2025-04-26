using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace HackerNewsAPI.Core.Services
{
    /// <summary>
    /// Service to interact with Hacker News API for fetching and searching stories.
    /// </summary>
    public class HackerNewsService : IHackerNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        private const string NewStoriesCacheKey = "NewStories";
        private const string StoryCachePrefix = "Story_";
        private const int CacheExpirationMinutes = 5;
        private const int MaxConcurrentRequests = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="HackerNewsService"/> class.
        /// </summary>
        /// <param name="httpClient">HTTP client to interact with the Hacker News API.</param>
        /// <param name="cache">Memory cache for storing fetched stories and IDs.</param>
        public HackerNewsService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
            _cache = cache;
        }

        /// <summary>
        /// Retrieves a paginated list of the newest stories from Hacker News.
        /// </summary>
        /// <param name="page">Page number (starting from 1).</param>
        /// <param name="pageSize">Number of stories per page.</param>
        /// <returns>A <see cref="StoryModel"/> containing a list of stories and total count.</returns>
        public async Task<StoryModel> GetNewestStories(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ArgumentException("Page and pageSize must be positive integers");

            return await GetProcessedStoriesAsync(page, pageSize);
        }

        /// <summary>
        /// Searches stories containing the specified search term in their title.
        /// </summary>
        /// <param name="searchTerm">The keyword to search for in story titles.</param>
        /// <returns>A collection of matching <see cref="Story"/> objects.</returns>
        public async Task<IEnumerable<Story>> SearchStories(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<Story>();

            var storiesModel = await GetProcessedStoriesAsync(searchTerm: searchTerm);
            return storiesModel.Stories;
        }

        /// <summary>
        /// Fetches, filters, paginates, and returns a set of stories.
        /// </summary>
        /// <param name="page">Optional page number for pagination.</param>
        /// <param name="pageSize">Optional page size for pagination.</param>
        /// <param name="searchTerm">Optional search term for filtering stories by title.</param>
        /// <returns>A <see cref="StoryModel"/> containing the processed stories.</returns>
        private async Task<StoryModel> GetProcessedStoriesAsync(int? page = null, int? pageSize = null, string? searchTerm = null)
        {
            var storyIds = await GetCachedStoryIdsAsync();

            var allStories = await GetStoriesAsync(storyIds);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                allStories = allStories.Where(s => s.Title.ToLower().Contains(lowerSearch)).ToList();
            }

            var totalCount = allStories.Count;

            if (page.HasValue && pageSize.HasValue)
            {
                allStories = allStories
                    .Skip((page.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToList();
            }

            return new StoryModel
            {
                Stories = allStories,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Retrieves the cached IDs of the newest stories, fetching from API if not available in cache.
        /// </summary>
        /// <returns>An array of story IDs.</returns>
        private async Task<int[]> GetCachedStoryIdsAsync()
        {
            return await _cache.GetOrCreateAsync(NewStoriesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);
                return await _httpClient.GetFromJsonAsync<int[]>("newstories.json") ?? Array.Empty<int>();
            });
        }

        /// <summary>
        /// Retrieves a single story by its ID, using cache when possible.
        /// </summary>
        /// <param name="id">The ID of the story.</param>
        /// <returns>The fetched <see cref="Story"/> object.</returns>
        private async Task<Story> GetStoryAsync(int id)
        {
            var cacheKey = $"{StoryCachePrefix}{id}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);
                var storyData = await _httpClient.GetFromJsonAsync<Story>("item/" + id + ".json");
                return storyData ?? new Story();
            });
        }

        /// <summary>
        /// Fetches multiple stories based on the provided IDs, limiting concurrent requests.
        /// </summary>
        /// <param name="ids">The collection of story IDs to fetch.</param>
        /// <returns>A list of fetched <see cref="Story"/> objects.</returns>
        private async Task<List<Story>> GetStoriesAsync(IEnumerable<int> ids)
        {
            var semaphore = new SemaphoreSlim(MaxConcurrentRequests);
            var storyTasks = ids.Select(async id =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await GetStoryAsync(id);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var stories = await Task.WhenAll(storyTasks);
            return stories.Where(s => s != null && !string.IsNullOrEmpty(s.Title)).ToList();
        }
    }
}
