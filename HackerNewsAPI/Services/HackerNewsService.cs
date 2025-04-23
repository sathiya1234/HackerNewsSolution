using HackerNewsAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsAPI.Services
{
    public class HackerNewsService : IHackerNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private const string NewStoriesCacheKey = "NewStories";

        public HackerNewsService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
            _cache = cache;
        }

        public async Task<IEnumerable<StoryModel>> GetNewestStories(int page, int pageSize)
        {
            if (!_cache.TryGetValue(NewStoriesCacheKey, out int[] storyIds))
            {
                storyIds = await _httpClient.GetFromJsonAsync<int[]>("newstories.json");
                _cache.Set(NewStoriesCacheKey, storyIds, TimeSpan.FromMinutes(5));
            }

            var currentPageIds = storyIds.Skip((page - 1) * pageSize).Take(pageSize);
            var stories = new List<StoryModel>();

            foreach (var id in currentPageIds)
            {
                var story = await _httpClient.GetFromJsonAsync<StoryModel>($"item/{id}.json");
                if (story != null && !string.IsNullOrEmpty(story.Title))
                {
                    stories.Add(story);
                }
            }
            return stories;
        }

        public async Task<IEnumerable<StoryModel>> SearchStories(string searchTerm)
        {
            if (!_cache.TryGetValue(NewStoriesCacheKey, out int[] storyIds))
            {
                storyIds = await _httpClient.GetFromJsonAsync<int[]>("newstories.json");
                _cache.Set(NewStoriesCacheKey, storyIds, TimeSpan.FromMinutes(5));
            }

            var stories = new List<StoryModel>();
            var searchLower = searchTerm.ToLower();

            foreach (var id in storyIds.Take(100))
            {
                var story = await _httpClient.GetFromJsonAsync<StoryModel>($"item/{id}.json");
                if (story != null && !string.IsNullOrEmpty(story.Title) &&
                    story.Title.ToLower().Contains(searchLower))
                {
                    stories.Add(story);
                }
            }
            return stories;
        }
    }
}