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
        private HttpClient _httpClient;
        private IMemoryCache _cache;
        private HackerNewsService _service;

        [SetUp]
        public void SetUp()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
            };

            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new HackerNewsService(_httpClient, _cache);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient.Dispose();
            _cache.Dispose();
        }

        [Test]
        public void GetNewestStories_InvalidPageOrPageSize_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(() => _service.GetNewestStories(0, 10));
            Assert.ThrowsAsync<ArgumentException>(() => _service.GetNewestStories(1, 0));
        }

        [Test]
        public async Task GetNewestStories_ReturnsPagedStories()
        {
            var storyIds = Enumerable.Range(1, 20).ToArray();
            var stories = storyIds.Select(id => new StoryModel { Id = id, Title = $"Story {id}" });

            SetupHttpResponse("newstories.json", storyIds);
            foreach (var id in storyIds)
                SetupHttpResponse($"item/{id}.json", new StoryModel { Id = id, Title = $"Story {id}" });

            var result = await _service.GetNewestStories(1, 5);

            Assert.That(result.Count(), Is.EqualTo(5));
        }

        [Test]
        public async Task SearchStories_EmptySearch_ReturnsEmptyList()
        {
            var result = await _service.SearchStories("");
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task SearchStories_ReturnsMatchingStories()
        {
            var storyIds = new[] { 101, 102, 103 };
            var stories = new[]
            {
                new StoryModel { Id = 101, Title = "NUnit tutorial" },
                new StoryModel { Id = 102, Title = "C# basics" },
                new StoryModel { Id = 103, Title = "Advanced NUnit" }
            };

            SetupHttpResponse("newstories.json", storyIds);
            foreach (var s in stories)
                SetupHttpResponse($"item/{s.Id}.json", s);

            var result = await _service.SearchStories("nunit");

            Assert.That(result.Count(), Is.EqualTo(2));
        }

        private void SetupHttpResponse<T>(string endpoint, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().EndsWith(endpoint)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }
    }
}

