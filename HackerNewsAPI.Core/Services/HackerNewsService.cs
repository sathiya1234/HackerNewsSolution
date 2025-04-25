using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace HackerNewsAPI.Core.Services
{
    /// <summary>
    /// Provides methods to retrieve and cache Hacker News stories.
    /// Supports pagination, search, and limits concurrent API requests.
    /// </summary>
    public class HackerNewsService : IHackerNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Cache key used for storing the list of new story IDs.
        /// </summary>
        private const string NewStoriesCacheKey = "NewStories";

        /// <summary>
        /// Prefix used to generate cache keys for individual stories.
        /// </summary>
        private const string StoryCachePrefix = "Story_";

        /// <summary>
        /// Number of minutes before a cached item expires.
        /// </summary>
        private const int CacheExpirationMinutes = 5;

        /// <summary>
        /// Maximum number of concurrent API requests when retrieving multiple stories.
        /// </summary>
        private const int MaxConcurrentRequests = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="HackerNewsService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to access the Hacker News API.</param>
        /// <param name="cache">The memory cache used for caching API responses.</param>
        public HackerNewsService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
            _cache = cache;
        }

        /// <summary>
        /// Retrieves the newest Hacker News stories using pagination.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of stories to return per page.</param>
        /// <returns>A collection of <see cref="StoryModel"/> objects for the requested page.</returns>
        /// <exception cref="ArgumentException">Thrown if page or pageSize are less than 1.</exception>
        public async Task<IEnumerable<StoryModel>> GetNewestStories(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ArgumentException("Page and pageSize must be positive integers");

            return await GetProcessedStoriesAsync(page, pageSize);
        }

        /// <summary>
        /// Searches all cached Hacker News stories for a specific term in their titles.
        /// </summary>
        /// <param name="searchTerm">The search term to look for in story titles.</param>
        /// <returns>A collection of <see cref="StoryModel"/> objects that match the search term.</returns>
        public async Task<IEnumerable<StoryModel>> SearchStories(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<StoryModel>();

            return await GetProcessedStoriesAsync(searchTerm: searchTerm);
        }

        /// <summary>
        /// Retrieves and processes stories from the Hacker News API, with optional pagination and filtering.
        /// </summary>
        /// <param name="page">Optional page number (1-based).</param>
        /// <param name="pageSize">Optional page size.</param>
        /// <param name="searchTerm">Optional search term to filter stories by title.</param>
        /// <returns>A filtered and/or paginated list of <see cref="StoryModel"/> objects.</returns>
        private async Task<IEnumerable<StoryModel>> GetProcessedStoriesAsync(int? page = null, int? pageSize = null, string? searchTerm = null)
        {
            var storyIds = await GetCachedStoryIdsAsync();

            if (page.HasValue && pageSize.HasValue)
            {
                storyIds = storyIds.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToArray();
            }

            var stories = await GetStoriesAsync(storyIds);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                stories = stories.Where(s => s.Title.ToLower().Contains(lowerSearch));
            }

            return stories;
        }

        /// <summary>
        /// Retrieves a list of new story IDs from the cache,
        /// or fetches from the Hacker News API if not present.
        /// </summary>
        /// <returns>An array of new story IDs.</returns>
        private async Task<int[]> GetCachedStoryIdsAsync()
        {
            return await _cache.GetOrCreateAsync(NewStoriesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);
                return await _httpClient.GetFromJsonAsync<int[]>("newstories.json");
            });
        }

        /// <summary>
        /// Retrieves an individual story by ID from the cache or API.
        /// </summary>
        /// <param name="id">The story ID to retrieve.</param>
        /// <returns>A <see cref="StoryModel"/> representing the story, or null if not found.</returns>
        private async Task<StoryModel> GetStoryAsync(int id)
        {
            var cacheKey = $"{StoryCachePrefix}{id}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);
                return await _httpClient.GetFromJsonAsync<StoryModel>($"item/{id}.json");
            });
        }

        /// <summary>
        /// Retrieves multiple stories by their IDs, enforcing a limit on concurrent requests.
        /// </summary>
        /// <param name="ids">The collection of story IDs to retrieve.</param>
        /// <returns>A collection of <see cref="StoryModel"/> objects.</returns>
        private async Task<IEnumerable<StoryModel>> GetStoriesAsync(IEnumerable<int> ids)
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
            return stories.Where(s => s != null && !string.IsNullOrEmpty(s.Title));
        }
    }
}
