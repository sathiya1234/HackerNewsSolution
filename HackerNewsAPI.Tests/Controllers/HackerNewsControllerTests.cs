using HackerNewsAPI.Controllers;
using HackerNewsAPI.Core.Interfaces;
using HackerNewsAPI.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HackerNewsAPI.Tests.Controllers
{

    [TestFixture]
    public class HackerNewsControllerTests
    {
        private Mock<IHackerNewsService> _mockService;
        private HackerNewsController _controller;

        [SetUp]
        public void SetUp()
        {
            _mockService = new Mock<IHackerNewsService>();
            _controller = new HackerNewsController(_mockService.Object);
        }

        [Test]
        public async Task GetNewestStories_ReturnsOkWithStories()
        {
            // Arrange
            var sampleStories = new List<StoryModel>
            {
                new StoryModel { Id = 1, Title = "Test Story 1" },
                new StoryModel { Id = 2, Title = "Test Story 2" }
            };

            _mockService.Setup(s => s.GetNewestStories(1, 2))
                        .ReturnsAsync(sampleStories);

            // Act
            var result = await _controller.GetNewestStories(1, 2);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult!.Value, Is.EqualTo(sampleStories));
        }

        [Test]
        public async Task SearchStories_ValidTerm_ReturnsOkWithMatchingStories()
        {
            // Arrange
            var term = "Angular";
            var expected = new List<StoryModel>
            {
                new StoryModel { Id = 3, Title = "Angular 18 Released" }
            };

            _mockService.Setup(s => s.SearchStories(term)).ReturnsAsync(expected);

            // Act
            var result = await _controller.SearchStories(term);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult!.Value, Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public async Task SearchStories_InvalidTerm_ReturnsBadRequest(string? term)
        {
            // Act
            var result = await _controller.SearchStories(term);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
            var badRequest = result.Result as BadRequestObjectResult;
            Assert.That(badRequest!.Value, Is.EqualTo("Search term is required"));
        }
    }
}
