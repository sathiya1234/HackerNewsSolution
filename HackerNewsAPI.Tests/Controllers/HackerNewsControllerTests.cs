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
        private Mock<IHackerNewsService> _mockHackerNewsService;
        private HackerNewsController _controller;

        [SetUp]
        public void Setup()
        {
            _mockHackerNewsService = new Mock<IHackerNewsService>();
            _controller = new HackerNewsController(_mockHackerNewsService.Object);
        }

        [Test]
        public async Task GetNewestStories_ReturnsBadRequest_WhenInvalidPageOrPageSize()
        {
            // Arrange
            int invalidPage = 0;
            int invalidPageSize = 0;

            // Act
            var result = await _controller.GetNewestStories(invalidPage, invalidPageSize);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
            var badRequest = (BadRequestObjectResult)result.Result;
            Assert.AreEqual("Page and pageSize must be greater than 0.", badRequest.Value);
        }

        [Test]
        public async Task GetNewestStories_ReturnsOkResult_WithStoryModel()
        {
            // Arrange
            var expectedModel = new StoryModel
            {
                Stories = new List<Story> { new Story { Id = 1, Title = "Test Story", Url = "http://example.com" } },
                TotalCount = 1
            };

            _mockHackerNewsService
                .Setup(service => service.GetNewestStories(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(expectedModel);

            // Act
            var result = await _controller.GetNewestStories(1, 10);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var model = okResult.Value as StoryModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(expectedModel.TotalCount, model.TotalCount);
        }

        [Test]
        public async Task SearchStories_ReturnsBadRequest_WhenTermIsEmpty()
        {
            // Arrange
            string emptyTerm = " ";

            // Act
            var result = await _controller.SearchStories(emptyTerm);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
            var badRequest = (BadRequestObjectResult)result.Result;
            Assert.AreEqual("Search term is required.", badRequest.Value);
        }

        [Test]
        public async Task SearchStories_ReturnsOkResult_WithStories()
        {
            // Arrange
            var expectedStories = new List<Story>
            {
                new Story { Id = 1, Title = "Test Search Story", Url = "http://example.com" }
            };

            _mockHackerNewsService
                .Setup(service => service.SearchStories(It.IsAny<string>()))
                .ReturnsAsync(expectedStories);

            // Act
            var result = await _controller.SearchStories("test");

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var stories = okResult.Value as IEnumerable<Story>;
            Assert.IsNotNull(stories);
            Assert.AreEqual(1, ((List<Story>)stories).Count);
        }
    }
}
