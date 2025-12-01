using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Commands;
using CertiWeb.API.Certifications.Domain.Model.Queries;
using CertiWeb.API.Certifications.Domain.Services;
using CertiWeb.API.Certifications.Interfaces.REST;
using CertiWeb.API.Certifications.Interfaces.REST.Resources;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Certifications.Interfaces.REST;

[TestFixture]
public class CarsControllerTests
{
    private Mock<ICarCommandService> _carCommandServiceMock;
    private Mock<ICarQueryService> _carQueryServiceMock;
    private CarsController _controller;

    [SetUp]
    public void SetUp()
    {
        _carCommandServiceMock = new Mock<ICarCommandService>();
        _carQueryServiceMock = new Mock<ICarQueryService>();
        _controller = new CarsController(_carCommandServiceMock.Object, _carQueryServiceMock.Object);
    }

    #region UpdateCar Tests

    [Test]
    public async Task UpdateCar_WithValidData_ShouldReturnOkWithCarResource()
    {
        // Arrange
        var carId = 1;
        var updateResource = new UpdateCarResource(
            "Updated Title",
            "Updated Owner",
            "updated@email.com",
            2022,
            1,
            "Updated Model",
            "Updated Description",
            "dGVzdHBkZmNvbnRlbnQ=",
            "http://image.url",
            30000,
            "XYZ789"
        );

        var updatedCar = new Car(new CreateCarCommand(
            "Updated Title",
            "Updated Owner",
            "updated@email.com",
            2022,
            1,
            "Updated Model",
            "Updated Description",
            "dGVzdHBkZmNvbnRlbnQ=",
            "http://image.url",
            30000,
            "XYZ789",
            100
        ));

        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<UpdateCarCommand>()))
            .ReturnsAsync(updatedCar);

        // Act
        var result = await _controller.UpdateCar(carId, updateResource);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<UpdateCarCommand>()), Times.Once);
    }

    [Test]
    public async Task UpdateCar_WithInvalidYear_ShouldReturnBadRequest()
    {
        // Arrange
        var carId = 1;
        var updateResource = new UpdateCarResource(
            "Title",
            "Owner",
            "email@test.com",
            1800, // Invalid year
            1,
            "Model",
            null,
            "dGVzdHBkZg==",
            null,
            25000,
            "ABC123"
        );

        // Act
        var result = await _controller.UpdateCar(carId, updateResource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<UpdateCarCommand>()), Times.Never);
    }

    [Test]
    public async Task UpdateCar_WithNegativePrice_ShouldReturnBadRequest()
    {
        // Arrange
        var carId = 1;
        var updateResource = new UpdateCarResource(
            "Title",
            "Owner",
            "email@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZg==",
            null,
            -1000, // Negative price
            "ABC123"
        );

        // Act
        var result = await _controller.UpdateCar(carId, updateResource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<UpdateCarCommand>()), Times.Never);
    }

    [Test]
    public async Task UpdateCar_WithHyphenInLicensePlate_ShouldReturnBadRequest()
    {
        // Arrange
        var carId = 1;
        var updateResource = new UpdateCarResource(
            "Title",
            "Owner",
            "email@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZg==",
            null,
            25000,
            "ABC-123" // Contains hyphen
        );

        // Act
        var result = await _controller.UpdateCar(carId, updateResource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<UpdateCarCommand>()), Times.Never);
    }

    [Test]
    public async Task UpdateCar_WithInvalidPdfBase64_ShouldReturnBadRequest()
    {
        // Arrange
        var carId = 1;
        var updateResource = new UpdateCarResource(
            "Title",
            "Owner",
            "email@test.com",
            2020,
            1,
            "Model",
            null,
            "invalid!!!base64", // Invalid base64
            null,
            25000,
            "ABC123"
        );

        // Act
        var result = await _controller.UpdateCar(carId, updateResource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<UpdateCarCommand>()), Times.Never);
    }

    [Test]
    public async Task UpdateCar_WhenServiceReturnsNull_ShouldReturnBadRequest()
    {
        // Arrange
        var carId = 999;
        var updateResource = new UpdateCarResource(
            "Title",
            "Owner",
            "email@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZg==",
            null,
            25000,
            "ABC123"
        );

        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<UpdateCarCommand>()))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _controller.UpdateCar(carId, updateResource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdateCar_WhenArgumentException_ShouldReturnBadRequest()
    {
        // Arrange
        var carId = 1;
        var updateResource = new UpdateCarResource(
            "Title",
            "Owner",
            "email@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZg==",
            null,
            25000,
            "ABC123"
        );

        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<UpdateCarCommand>()))
            .ThrowsAsync(new ArgumentException("Invalid argument", "paramName"));

        // Act
        var result = await _controller.UpdateCar(carId, updateResource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdateCar_WhenInvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var carId = 1;
        var updateResource = new UpdateCarResource(
            "Title",
            "Owner",
            "email@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZg==",
            null,
            25000,
            "ABC123"
        );

        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<UpdateCarCommand>()))
            .ThrowsAsync(new InvalidOperationException("Business rule violation"));

        // Act
        var result = await _controller.UpdateCar(carId, updateResource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetCarPdf Tests

    [Test]
    public async Task GetCarPdf_WhenCarExists_ShouldReturnOkWithPdfData()
    {
        // Arrange
        var carId = 1;
        var car = new Car(new CreateCarCommand(
            "Test Car",
            "Owner",
            "owner@test.com",
            2020,
            1,
            "Model",
            "Description",
            "dGVzdHBkZmNvbnRlbnQ=",
            "http://image.url",
            25000,
            "ABC123",
            1
        ));

        _carQueryServiceMock.Setup(s => s.Handle(It.IsAny<GetCarByIdQuery>()))
            .ReturnsAsync(car);

        // Act
        var result = await _controller.GetCarPdf(carId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Test]
    public async Task GetCarPdf_WhenCarDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var carId = 999;
        _carQueryServiceMock.Setup(s => s.Handle(It.IsAny<GetCarByIdQuery>()))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _controller.GetCarPdf(carId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetCarPdf_WhenExceptionOccurs_ShouldReturnInternalServerError()
    {
        // Arrange
        var carId = 1;
        _carQueryServiceMock.Setup(s => s.Handle(It.IsAny<GetCarByIdQuery>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetCarPdf(carId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetCarsByOwner Tests

    [Test]
    public async Task GetCarsByOwner_WithValidEmail_ShouldReturnCars()
    {
        // Arrange
        var ownerEmail = "owner@test.com";
        var cars = new List<Car>
        {
            new Car(new CreateCarCommand(
                "Car 1",
                "Owner",
                ownerEmail,
                2020,
                1,
                "Model",
                null,
                "dGVzdHBkZg==",
                null,
                25000,
                "ABC123",
                1
            )),
            new Car(new CreateCarCommand(
                "Car 2",
                "Owner",
                ownerEmail,
                2021,
                1,
                "Model",
                null,
                "dGVzdHBkZg==",
                null,
                30000,
                "XYZ789",
                2
            ))
        };

        _carQueryServiceMock.Setup(s => s.Handle(It.IsAny<GetCarsByOwnerEmailQuery>()))
            .ReturnsAsync(cars);

        // Act
        var result = await _controller.GetCarsByOwner(ownerEmail);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Test]
    public async Task GetCarsByOwner_WhenNoCarsFound_ShouldReturnEmptyList()
    {
        // Arrange
        var ownerEmail = "nonexistent@test.com";
        _carQueryServiceMock.Setup(s => s.Handle(It.IsAny<GetCarsByOwnerEmailQuery>()))
            .ReturnsAsync(new List<Car>());

        // Act
        var result = await _controller.GetCarsByOwner(ownerEmail);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region DeleteCar Tests

    [Test]
    public async Task DeleteCar_WhenCarExists_ShouldReturnNoContent()
    {
        // Arrange
        var carId = 1;
        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<DeleteCarCommand>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCar(carId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<DeleteCarCommand>()), Times.Once);
    }

    [Test]
    public async Task DeleteCar_WhenCarDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var carId = 999;
        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<DeleteCarCommand>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCar(carId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task DeleteCar_WhenExceptionOccurs_ShouldReturnInternalServerError()
    {
        // Arrange
        var carId = 1;
        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<DeleteCarCommand>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteCar(carId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region CreateCar Additional Tests

    [Test]
    public async Task CreateCar_WithYearTooOld_ShouldReturnBadRequest()
    {
        // Arrange
        var resource = new CreateCarResource(
            "Test Car",
            "Owner",
            "owner@test.com",
            1899, // Too old
            1,
            "Model",
            null,
            "dGVzdHBkZmNvbnRlbnQ=",
            null,
            25000,
            "ABC123",
            1
        );

        // Act
        var result = await _controller.CreateCar(resource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<CreateCarCommand>()), Times.Never);
    }

    [Test]
    public async Task CreateCar_WithYearTooFuture_ShouldReturnBadRequest()
    {
        // Arrange
        var resource = new CreateCarResource(
            "Test Car",
            "Owner",
            "owner@test.com",
            DateTime.Now.Year + 2, // Too far in future
            1,
            "Model",
            null,
            "dGVzdHBkZmNvbnRlbnQ=",
            null,
            25000,
            "ABC123",
            1
        );

        // Act
        var result = await _controller.CreateCar(resource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<CreateCarCommand>()), Times.Never);
    }

    [Test]
    public async Task CreateCar_WithNegativeBrandId_ShouldReturnBadRequest()
    {
        // Arrange
        var resource = new CreateCarResource(
            "Test Car",
            "Owner",
            "owner@test.com",
            2020,
            -1, // Negative brand ID
            "Model",
            null,
            "dGVzdHBkZmNvbnRlbnQ=",
            null,
            25000,
            "ABC123",
            1
        );

        // Act
        var result = await _controller.CreateCar(resource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<CreateCarCommand>()), Times.Never);
    }

    [Test]
    public async Task CreateCar_WithNegativePrice_ShouldReturnBadRequest()
    {
        // Arrange
        var resource = new CreateCarResource(
            "Test Car",
            "Owner",
            "owner@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZmNvbnRlbnQ=",
            null,
            -100, // Negative price
            "ABC123",
            1
        );

        // Act
        var result = await _controller.CreateCar(resource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _carCommandServiceMock.Verify(s => s.Handle(It.IsAny<CreateCarCommand>()), Times.Never);
    }

    [Test]
    public async Task CreateCar_WhenServiceReturnsNull_ShouldReturnBadRequest()
    {
        // Arrange
        var resource = new CreateCarResource(
            "Test Car",
            "Owner",
            "owner@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZmNvbnRlbnQ=",
            null,
            25000,
            "ABC123",
            1
        );

        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<CreateCarCommand>()))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _controller.CreateCar(resource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreateCar_WhenInvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var resource = new CreateCarResource(
            "Test Car",
            "Owner",
            "owner@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZmNvbnRlbnQ=",
            null,
            25000,
            "ABC123",
            1
        );

        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<CreateCarCommand>()))
            .ThrowsAsync(new InvalidOperationException("Business rule violation"));

        // Act
        var result = await _controller.CreateCar(resource);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task CreateCar_WhenUnexpectedException_ShouldReturnInternalServerError()
    {
        // Arrange
        var resource = new CreateCarResource(
            "Test Car",
            "Owner",
            "owner@test.com",
            2020,
            1,
            "Model",
            null,
            "dGVzdHBkZmNvbnRlbnQ=",
            null,
            25000,
            "ABC123",
            1
        );

        _carCommandServiceMock.Setup(s => s.Handle(It.IsAny<CreateCarCommand>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.CreateCar(resource);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}
