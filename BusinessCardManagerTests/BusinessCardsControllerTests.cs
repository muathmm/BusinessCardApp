using BusinessCardManager.Controllers;
using BusinessCardManager.Repositories.Interfaces;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using BusinessCardManager.Models.DTO;

namespace BusinessCardManagerTests
{
    public class BusinessCardsControllerTests
    {
        private readonly Mock<IBusinessCard> _mockService;
        private readonly BusinessCardsController _controller;

        public BusinessCardsControllerTests()
        {
            // Create a Mock for IBusinessCard
            _mockService = new Mock<IBusinessCard>();

            // Pass the Mock to the Controller
            _controller = new BusinessCardsController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfBusinessCards()
        {
            // Arrange: Set up initial data
            var mockCards = new List<BusinessCardResDto>
            {
                new BusinessCardResDto { Id = 1, Name = "John Doe",
                    Email = "john@example.com",
                    Gender = "Male",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    Phone = "123456789",
                    Address = "123 Main St",
                    PhotoBase64 = null},
                new BusinessCardResDto { Id = 2, Name = "John Doe",
                    Email = "john@example.com",
                    Gender = "Male",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    Phone = "123456789",
                    Address = "123 Main St",
                    PhotoBase64 = null}
            };

            // Set up the Mock to return the cards
            _mockService.Setup(s => s.GetAllAsync(null)).ReturnsAsync(mockCards);

            // Act: Call the GetAll method
            var result = await _controller.GetAll();

            // Assert: Verify the result
            var okResult = Assert.IsType<OkObjectResult>(result); // Ensure the result is OK
            var returnedCards = Assert.IsType<List<BusinessCardResDto>>(okResult.Value); // Ensure the result type is correct
            Assert.Equal(2, returnedCards.Count); // Check the number of cards
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenCardDoesNotExist()
        {
            // Arrange: Set up the Mock to return null when the card doesn't exist
            _mockService.Setup(s => s.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((BusinessCardResDto)null);

            // Act: Call GetById with a non-existing ID
            var result = await _controller.GetById(1);

            // Assert: Verify the result is NotFound
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithBusinessCard()
        {
            // Arrange: Set up initial data
            var mockCard = new BusinessCardResDto { Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 1),
                Phone = "123456789",
                Address = "123 Main St",
                PhotoBase64 = null
            };

            // Set up the Mock to return the card
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(mockCard);

            // Act: Call GetById with an existing ID
            var result = await _controller.GetById(1);

            // Assert: Verify the result is OK and the card is returned
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCard = Assert.IsType<BusinessCardResDto>(okResult.Value);
            Assert.Equal(1, returnedCard.Id);
        }

        [Fact]
        public async Task Create_ReturnsOk_WithAllProperties_WhenCardIsAddedSuccessfully()
        {
            // Arrange: Set up the Mock to return "ok" when adding the card
            var cardDto = new BusinessCardReqDto
            {
                Name = "John Doe",
                Email = "john@example.com",
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 1),
                Phone = "123456789",
                Address = "123 Main St",
                PhotoBase64 = null
            };
            _mockService.Setup(s => s.AddBusinessCardAsync(cardDto)).ReturnsAsync("ok");

            // Act: Call Create
            var result = await _controller.Create(cardDto);

            // Assert: Verify the result is OK
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Verify that the object contains the "Message" property
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty); // Ensure the property exists
            Assert.Equal("Add card Successfully", messageProperty.GetValue(response)?.ToString()); // Check the value
        }

        [Fact]
        public async Task Update_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange: No need to set up the returned value since it is void
            var cardDto = new BusinessCardReqDto { Name = "Updated Name", Email = "updated@example.com" };

            // Act: Call Update
            var result = await _controller.Update(1, cardDto);

            // Assert: Verify the result is NoContent
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeletionIsSuccessful()
        {
            // Arrange: Set up the Mock to delete the card
            _mockService.Setup(s => s.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            // Act: Call Delete
            var result = await _controller.Delete(1);

            // Assert: Verify the result is NoContent
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ExportToXml_ReturnsFileResult_WithXmlData()
        {
            // Arrange: Set up the Mock to return XML data
            var xmlData = "<BusinessCards><Card><Name>John</Name></Card></BusinessCards>";
            _mockService.Setup(s => s.ExportToXmlAsync()).ReturnsAsync(xmlData);

            // Act: Call ExportToXml
            var result = await _controller.ExportToXml();

            // Assert: Verify the result is File
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/xml", fileResult.ContentType);
            Assert.Equal("BusinessCards.xml", fileResult.FileDownloadName);
        }
    }
}
