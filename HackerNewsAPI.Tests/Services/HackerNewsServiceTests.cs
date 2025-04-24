using HackerNewsAPI.Core.Models;
using HackerNewsAPI.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace HackerNewsAPI.Tests.Services
{
    [TestFixture]
    public class HackerNewsServiceTests
    {
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Mock<IMemoryCache> _memoryCacheMock;
        private HackerNewsService _service;

        [SetUp]
        public void Setup()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _memoryCacheMock = new Mock<IMemoryCache>();

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
            };

            _service = new HackerNewsService(httpClient, _memoryCacheMock.Object);
        }

        [Test]
        public async Task GetNewestStories_ReturnsStories()
        {
            // Arrange
            var storyIds = new[] { 1, 2, 3 };
            var mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(storyIds))
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

            // Act
            var result = await _service.GetNewestStories(1, 10);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any());
        }

        [Test]
        public async Task SearchStories_WithEmptyTerm_ReturnsEmptyCollection()
        {
            // Act
            var result = await _service.SearchStories("");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }

        [Test]
        public async Task GetNewestStories_CachesStoryIds()
        {
            // Arrange
            var storyIds = new[] { 1, 2, 3 };
            var mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(storyIds))
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            object cachedValue = null;
            _memoryCacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);
            _memoryCacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

            // Act
            var result1 = await _service.GetNewestStories(1, 1);
            var result2 = await _service.GetNewestStories(1, 1);

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1), // Should only call once due to caching
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task SearchStories_WithValidTerm_ReturnsMatchingStories()
        {
            // Arrange
            var storyIds = new[] { 1, 2, 3 };
            var mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(storyIds))
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri.AbsoluteUri.Contains("newstories.json")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            // Mock story responses
            var stories = new Dictionary<int, StoryModel>
        {
            { 1, new StoryModel { Id = 1, Title = "Test story about programming" } },
            { 2, new StoryModel { Id = 2, Title = "Another tech story" } },
            { 3, new StoryModel { Id = 3, Title = "Non-tech related story" } }
        };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri.AbsoluteUri.Contains("item/")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
                {
                    var id = int.Parse(request.RequestUri.Segments.Last().Replace(".json", ""));
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(stories[id]))
                    };
                });

            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

            // Act
            var result = await _service.SearchStories("tech");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("Another tech story", result.First().Title);
        }
    }
}
