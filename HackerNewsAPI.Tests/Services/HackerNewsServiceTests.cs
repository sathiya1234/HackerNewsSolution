using HackerNewsAPI.Core.Models;
using HackerNewsAPI.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using System.Linq;

namespace HackerNewsAPI.Tests.Services
{
    [TestFixture]
    public class HackerNewsServiceTests
    {
        private HackerNewsService _service;
        private Mock<IMemoryCache> _cacheMock;
        private Mock<HttpMessageHandler> _httpHandlerMock;
        private MemoryCache _realCache;
        private HttpClient _httpClient;

        [SetUp]
        public void Setup()
        {
            _realCache = new MemoryCache(new MemoryCacheOptions());
            _cacheMock = new Mock<IMemoryCache>();
            _httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            // Allow HttpMessageHandler.Dispose to be called without errors
            _httpHandlerMock.Protected()
                .Setup("Dispose", ItExpr.IsAny<bool>());

            _httpClient = new HttpClient(_httpHandlerMock.Object)
            {
                BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
            };
        }

        [TearDown]
        public void TearDown()
        {
            _realCache?.Dispose();
            _httpClient?.Dispose();
        }

        private void SetupHttpClientWithResponse<T>(string url, T content)
        {
            _httpHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(m => m.RequestUri.ToString().Contains(url)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(content)
                });

            _httpClient = new HttpClient(_httpHandlerMock.Object)
            {
                BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
            };
        }

        [Test]
        public async Task GetNewestStories_ReturnsPaginatedResults()
        {
            // Arrange
            var storyIds = Enumerable.Range(1, 5).ToArray();
            var stories = storyIds.Select(id => new Story
            {
                Id = id,
                Title = $"Title {id}",
                Url = $"https://story{id}.com"
            }).ToList();

            SetupHttpClientWithResponse("newstories.json", storyIds);
            foreach (var story in stories)
            {
                SetupHttpClientWithResponse($"item/{story.Id}.json", story);
            }

            _service = new HackerNewsService(_httpClient, _realCache);

            // Act
            var result = await _service.GetNewestStories(page: 1, pageSize: 2, searchTerm: "Title");

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(5));
            Assert.That(result.Stories.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetNewestStories_ReturnsFilteredResults()
        {
            // Arrange
            var storyIds = new[] { 1, 2, 3 };
            var stories = new List<Story>
            {
                new Story { Id = 1, Title = "C# advanced tips" },
                new Story { Id = 2, Title = "Java basics" },
                new Story { Id = 3, Title = "C# testing strategies" }
            };

            SetupHttpClientWithResponse("newstories.json", storyIds);
            foreach (var story in stories)
            {
                SetupHttpClientWithResponse($"item/{story.Id}.json", story);
            }

            _service = new HackerNewsService(_httpClient, _realCache);

            // Act
            var result = await _service.GetNewestStories(1, 10, "C#");

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Stories.All(s => s.Title.Contains("C#")));
        }

        [Test]
        public async Task GetNewestStories_AppliesMaxLimitOf200()
        {
            // Arrange
            var storyIds = Enumerable.Range(1, 300).ToArray();
            var stories = storyIds.Take(200).Select(id => new Story
            {
                Id = id,
                Title = $"Story {id}"
            }).ToList();

            SetupHttpClientWithResponse("newstories.json", storyIds);
            foreach (var story in stories)
            {
                SetupHttpClientWithResponse($"item/{story.Id}.json", story);
            }

            _service = new HackerNewsService(_httpClient, _realCache);

            // Act
            var result = await _service.GetNewestStories(1, 20);

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(200));
            Assert.That(result.Stories.Count, Is.EqualTo(20));
        }

        [Test]
        public async Task GetNewestStories_ReturnsAll_WhenSearchTermIsEmpty()
        {
            // Arrange
            var storyIds = new[] { 1, 2 };
            var stories = new List<Story>
            {
                new Story { Id = 1, Title = "Test 1" },
                new Story { Id = 2, Title = "Test 2" }
            };

            SetupHttpClientWithResponse("newstories.json", storyIds);
            foreach (var story in stories)
            {
                SetupHttpClientWithResponse($"item/{story.Id}.json", story);
            }

            _service = new HackerNewsService(_httpClient, _realCache);

            // Act
            var result = await _service.GetNewestStories(1, 10, "");

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(2));
        }
    }
}
