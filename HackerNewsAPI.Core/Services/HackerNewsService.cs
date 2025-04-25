using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace HackerNewsAPI.Core.Services
{
    /// <summary>
    /// Implementation of <see cref="IHackerNewsService"/> that provides access to Hacker News stories
    /// with caching and limited concurrency support.
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
        /// Prefix used to create unique cache keys for individual stories.
        /// </summary>
        private const string StoryCachePrefix = "Story_";

        /// <summary>
        /// Time duration (in minutes) after which cache entries expire.
        /// </summary>
        private const int CacheExpirationMinutes = 5;

        /// <summary>
        /// Maximum number of concurrent requests allowed to the Hacker News API.
        /// </summary>
        private const int MaxConcurrentRequests = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="HackerNewsService"/> class.
        /// </summary>
        /// <param name="httpClient">HTTP client used to communicate with Hacker News API.</param>
        /// <param name="cache">Memory cache used to store responses and improve performance.</param>
        public HackerNewsService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
            _cache = cache;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<StoryModel>> GetNewestStories(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ArgumentException("Page and pageSize must be positive integers");

            var storyIds = await GetCachedStoryIdsAsync();
            var currentPageIds = storyIds.Skip((page - 1) * pageSize).Take(pageSize);

            return await GetStoriesAsync(currentPageIds);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<StoryModel>> SearchStories(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<StoryModel>();

            var storyIds = await GetCachedStoryIdsAsync();
            var searchLower = searchTerm.ToLower();

            var stories = await GetStoriesAsync(storyIds);
            return stories.Where(s => s.Title.ToLower().Contains(searchLower));
        }

        /// <summary>
        /// Retrieves a cached list of new story IDs from memory,
        /// or fetches from the Hacker News API if not available in cache.
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
        /// Retrieves a specific story by its ID, using cached data if available.
        /// </summary>
        /// <param name="id">The ID of the story to retrieve.</param>
        /// <returns>A <see cref="StoryModel"/> representing the story details.</returns>
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
        /// Retrieves multiple stories given their IDs, limiting concurrent API requests.
        /// </summary>
        /// <param name="ids">A collection of story IDs.</param>
        /// <returns>A collection of <see cref="StoryModel"/> objects representing the stories.</returns>
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
