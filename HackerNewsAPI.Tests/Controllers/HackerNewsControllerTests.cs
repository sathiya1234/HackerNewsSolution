using HackerNewsAPI.Controllers;
using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsAPI.Tests.Controllers
{

    [TestFixture]
    public class HackerNewsControllerTests
    {
        private Mock<IHackerNewsService> _mockService;
        private Mock<ILogger<HackerNewsController>> _mockLogger;
        private HackerNewsController _controller;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IHackerNewsService>();
            _mockLogger = new Mock<ILogger<HackerNewsController>>();
            _controller = new HackerNewsController(_mockService.Object, _mockLogger.Object);
        }

        [Test]
        public async Task GetNewestStories_InvalidPageOrPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetNewestStories(0, 10);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            Assert.AreEqual(400, badRequestResult.StatusCode);
        }

        [Test]
        public async Task GetNewestStories_NoStoriesFound_ReturnsNotFound()
        {
            // Arrange
            var model = new StoryModel { Stories = new List<Story>(), TotalCount = 0 };
            _mockService.Setup(s => s.GetNewestStories(1, 10, null)).ReturnsAsync(model);

            // Act
            var result = await _controller.GetNewestStories(1, 10);

            // Assert
            var notFoundResult = result.Result as NotFoundObjectResult;
            Assert.IsNotNull(notFoundResult);
            Assert.AreEqual(404, notFoundResult.StatusCode);
        }

        [Test]
        public async Task GetNewestStories_ValidRequest_ReturnsOk()
        {
            // Arrange
            var model = new StoryModel
            {
                Stories = new List<Story> { new Story { Id = 1, Title = "Story", Url = "https://test.com" } },
                TotalCount = 1
            };
            _mockService.Setup(s => s.GetNewestStories(1, 10, null)).ReturnsAsync(model);

            // Act
            var result = await _controller.GetNewestStories(1, 10);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreSame(model, okResult.Value);
        }

        [Test]
        public async Task GetNewestStories_HttpRequestException_ReturnsServiceUnavailable()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetNewestStories(1, 10, null))
                .ThrowsAsync(new HttpRequestException("API error"));

            // Act
            var result = await _controller.GetNewestStories(1, 10);

            // Assert
            var serviceUnavailableResult = result.Result as ObjectResult;
            Assert.IsNotNull(serviceUnavailableResult);
            Assert.AreEqual(503, serviceUnavailableResult.StatusCode);
        }

        [Test]
        public async Task GetNewestStories_TimeoutException_ReturnsGatewayTimeout()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetNewestStories(1, 10, null))
                .ThrowsAsync(new TimeoutException("Timeout"));

            // Act
            var result = await _controller.GetNewestStories(1, 10);

            // Assert
            var timeoutResult = result.Result as ObjectResult;
            Assert.IsNotNull(timeoutResult);
            Assert.AreEqual(504, timeoutResult.StatusCode);
        }

        [Test]
        public async Task GetNewestStories_UnknownException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetNewestStories(1, 10, null))
                .ThrowsAsync(new Exception("Unknown error"));

            // Act
            var result = await _controller.GetNewestStories(1, 10);

            // Assert
            var errorResult = result.Result as ObjectResult;
            Assert.IsNotNull(errorResult);
            Assert.AreEqual(500, errorResult.StatusCode);
        }
    }
}
