using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace HackerNewsAPI.Core.Services
{

    /// <summary>
    /// Service implementation for Hacker News API operations
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
        /// Initializes a new instance of the Hacker News service
        /// </summary>
        /// <param name="httpClient">Configured HttpClient</param>
        /// <param name="cache">Memory cache instance</param>
        public HackerNewsService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
            _cache = cache;
        }

        /// <inheritdoc/>
        public async Task<StoryModel> GetNewestStories(int page, int pageSize, string? searchTerm = null)
        {
            var storyIds = await GetCachedStoryIdsAsync();
            // Enforce the maximum of 200 stories
            var limitedStoryIds = storyIds.Take(200);
            var allStories = await GetStoriesAsync(limitedStoryIds);
            var filtered = FilterStories(allStories, searchTerm);
            return PaginateResults(filtered, page, pageSize);
        }

        private List<Story> FilterStories(List<Story> stories, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return stories;

            var lowerTerm = searchTerm.ToLower();
            return stories.Where(s => s.Title.ToLower().Contains(lowerTerm)).ToList();
        }

        private StoryModel PaginateResults(List<Story> stories, int page, int pageSize)
        {
            var paginated = stories.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new StoryModel
            {
                Stories = paginated,
                TotalCount = stories.Count
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
