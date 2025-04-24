using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace HackerNewsAPI.Core.Services
{
    /// <summary>
    /// Service implementation for retrieving and searching Hacker News stories
    /// </summary>
    public class HackerNewsService : IHackerNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private const string NewStoriesCacheKey = "NewStories";
        private const string StoryCachePrefix = "Story_";
        private const int CacheExpirationMinutes = 5;
        private const int MaxConcurrentRequests = 10; // Limit concurrent API calls

        public HackerNewsService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
            _cache = cache;
        }

        /// <summary>
        /// Retrieves the newest stories with pagination support
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of stories per page</param>
        /// <returns>List of stories for the requested page</returns>
        public async Task<IEnumerable<StoryModel>> GetNewestStories(int page, int pageSize)
        {
            if (page < 1 || pageSize < 1)
                throw new ArgumentException("Page and pageSize must be positive integers");

            var storyIds = await GetCachedStoryIdsAsync();
            var currentPageIds = storyIds.Skip((page - 1) * pageSize).Take(pageSize);

            return await GetStoriesAsync(currentPageIds);
        }

        /// <summary>
        /// Searches stories by title containing the search term
        /// </summary>
        /// <param name="searchTerm">Term to search in story titles</param>
        /// <returns>List of stories matching the search term</returns>
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
        /// Gets story IDs from cache or fetches from API if not cached
        /// </summary>
        private async Task<int[]> GetCachedStoryIdsAsync()
        {
            return await _cache.GetOrCreateAsync(NewStoriesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);
                return await _httpClient.GetFromJsonAsync<int[]>("newstories.json");
            });
        }

        /// <summary>
        /// Gets story details either from cache or API
        /// </summary>
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
        /// Gets multiple stories with limited concurrency
        /// </summary>
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
