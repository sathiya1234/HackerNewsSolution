using HackerNewsAPI.Core.Models;
using HackerNewsAPI.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;
using System.Text.Json;

namespace HackerNewsAPI.Tests.Services
{
    [TestFixture]
    public class HackerNewsServiceTests
    {
        private HackerNewsService _service;
        private Mock<IMemoryCache> _memoryCacheMock;
        private HttpClient _httpClient;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Mock<ICacheEntry> _cacheEntryMock;

        [SetUp]
        public void Setup()
        {
            _memoryCacheMock = new Mock<IMemoryCache>();
            _cacheEntryMock = new Mock<ICacheEntry>();
            _cacheEntryMock.SetupAllProperties();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);

            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
            };

            _service = new HackerNewsService(_httpClient, _memoryCacheMock.Object);

            _memoryCacheMock
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(_cacheEntryMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        [Test]
        public void GetNewestStories_InvalidPageOrPageSize_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _service.GetNewestStories(0, 10));
            Assert.ThrowsAsync<ArgumentException>(() => _service.GetNewestStories(1, 0));
        }

        [Test]
        public async Task SearchStories_EmptySearchTerm_ReturnsEmptyList()
        {
            // Act
            var result = await _service.SearchStories("");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetNewestStories_ValidRequest_ReturnsStories()
        {
            // Arrange
            var storyIds = new[] { 1, 2, 3 };

            SetupMemoryCache(HackerNewsServiceTestHelper.NewStoriesCacheKey, storyIds);
            SetupHttpClientGet("newstories.json", storyIds);

            var stories = new List<Story>
            {
                new Story { Id = 1, Title = "Story One" },
                new Story { Id = 2, Title = "Story Two" },
                new Story { Id = 3, Title = "Story Three" }
            };

            foreach (var story in stories)
            {
                SetupHttpClientGet($"item/{story.Id}.json", story);
            }

            // Act
            var result = await _service.GetNewestStories(1, 2);

            // Assert
            Assert.IsNotNull(result);
            Assert.That(result.Stories.Count, Is.EqualTo(2));
            Assert.That(result.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public async Task SearchStories_WithValidSearchTerm_ReturnsFilteredStories()
        {
            // Arrange
            var storyIds = new[] { 1, 2 };

            SetupMemoryCache(HackerNewsServiceTestHelper.NewStoriesCacheKey, storyIds);
            SetupHttpClientGet("newstories.json", storyIds);

            var stories = new List<Story>
            {
                new Story { Id = 1, Title = "Angular is awesome" },
                new Story { Id = 2, Title = "Learning NUnit" }
            };

            foreach (var story in stories)
            {
                SetupHttpClientGet($"item/{story.Id}.json", story);
            }

            // Act
            var result = await _service.SearchStories("Angular");

            // Assert
            Assert.IsNotNull(result);
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().Title, Is.EqualTo("Angular is awesome"));
        }

        // --- Helpers ---

        private void SetupMemoryCache<T>(string key, T value)
        {
            object temp = value;

            _memoryCacheMock
                .Setup(m => m.TryGetValue(key, out temp))
                .Returns(true); // simulate cache hit

            _memoryCacheMock
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(_cacheEntryMock.Object);
        }

        private void SetupHttpClientGet<T>(string url, T content)
        {
            _httpMessageHandlerMock
                .SetupRequest(HttpMethod.Get, new Uri(_httpClient.BaseAddress, url))
                .ReturnsResponse(JsonSerializer.Serialize(content), "application/json");
        }
    }

    internal static class HackerNewsServiceTestHelper
    {
        public const string NewStoriesCacheKey = "NewStories";
    }
}


