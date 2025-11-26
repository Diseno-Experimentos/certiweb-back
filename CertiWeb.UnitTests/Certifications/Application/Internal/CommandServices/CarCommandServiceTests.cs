using CertiWeb.API.Certifications.Application.Internal.CommandServices;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Commands;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Repositories;
using CertiWeb.API.Shared.Domain.Repositories;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CertiWeb.UnitTests.Certifications.Application.Internal.CommandServices;

/// <summary>
/// Unit tests for Car command services
/// </summary>
public class CarCommandServiceTests
{
    private Mock<ICarRepository> _carRepositoryMock;
    private Mock<IBrandRepository> _brandRepositoryMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;

    [SetUp]
    public void SetUp()
    {
        _carRepositoryMock = new Mock<ICarRepository>();
        _brandRepositoryMock = new Mock<IBrandRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    [Test]
    public async Task Handle_CreateCarCommand_WhenValidCommand_ShouldCreateCar()
    {
        // Arrange
        var command = new CreateCarCommand(
            "Test Title",
            "Test Owner",
            "owner@example.com",
            2020,
            1,
            "Test Model",
            null,
            Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            null,
            25000m,
            "ABC-123",
            0
        );
        var brand = CreateTestBrand(1);
        var expectedCar = CreateTestCar(1);

        _brandRepositoryMock.Setup(repo => repo.FindBrandByIdAsync(command.BrandId))
            .ReturnsAsync(brand);
        
        _carRepositoryMock.Setup(repo => repo.FindCarByLicensePlateAsync(It.IsAny<string>()))
            .ReturnsAsync((Car?)null);
        
        _carRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Car>()))
            .Callback<Car>(c =>
            {
                var idProp = typeof(Car).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                idProp?.SetValue(c, expectedCar.Id);
            })
            .Returns(Task.CompletedTask);
        
        _unitOfWorkMock.Setup(uow => uow.CompleteAsync())
            .Returns(Task.CompletedTask);

        var service = CreateCommandService();

        // Act
        var result = await service.Handle(command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedCar.Id, result.Id);
        Assert.AreEqual(command.Model, result.Model);
        
        _brandRepositoryMock.Verify(repo => repo.FindBrandByIdAsync(command.BrandId), Times.Once);
        _carRepositoryMock.Verify(repo => repo.FindCarByLicensePlateAsync(It.IsAny<string>()), Times.Once);
        _carRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Car>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_CreateCarCommand_WhenBrandDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var command = new CreateCarCommand(
            Title: "Test Model",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 999,
            Model: "Test Model",
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: "ABC-123",
            OriginalReservationId: 0
        );

        _brandRepositoryMock.Setup(repo => repo.FindBrandByIdAsync(command.BrandId))
            .ReturnsAsync((Brand?)null);

        var service = CreateCommandService();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.Handle(command).GetAwaiter().GetResult());

        StringAssert.Contains("Brand not found", exception.Message);
        _brandRepositoryMock.Verify(repo => repo.FindBrandByIdAsync(command.BrandId), Times.Once);
        _carRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Car>()), Times.Never);
    }

    [Test]
    public async Task Handle_CreateCarCommand_WhenLicensePlateExists_ShouldThrowException()
    {
        // Arrange
        var command = new CreateCarCommand(
            Title: "Test Model",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: "Test Model",
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: "ABC-123",
            OriginalReservationId: 0
        );
        var brand = CreateTestBrand(1);
        var existingCar = CreateTestCar(2);

        _brandRepositoryMock.Setup(repo => repo.FindBrandByIdAsync(command.BrandId))
            .ReturnsAsync(brand);
        
        _carRepositoryMock.Setup(repo => repo.FindCarByLicensePlateAsync(It.IsAny<string>()))
            .ReturnsAsync(existingCar);

        var service = CreateCommandService();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.Handle(command).GetAwaiter().GetResult());

        StringAssert.Contains("License plate already exists", exception.Message);
        _carRepositoryMock.Verify(repo => repo.FindCarByLicensePlateAsync(It.IsAny<string>()), Times.Once);
        _carRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Car>()), Times.Never);
    }

    [Test]
    public async Task Handle_UpdateCarCommand_WhenValidCommand_ShouldUpdateCar()
    {
        // Arrange
        var command = new UpdateCarCommand(
            Id: 1,
            Title: "Updated Model",
            Owner: null,
            OwnerEmail: null,
            Year: 2021,
            BrandId: 1,
            Model: "Updated Model",
            Description: null,
            PdfCertification: null,
            ImageUrl: null,
            Price: 30000m,
            LicensePlate: "UPD-123"
        );
        var existingCar = CreateTestCar(1);
        var updatedCar = CreateTestCar(1, "Updated Model");

        _carRepositoryMock.Setup(repo => repo.FindByIdAsync(command.Id))
            .ReturnsAsync(existingCar);
        
        _carRepositoryMock.Setup(repo => repo.FindCarByLicensePlateAsync(It.IsAny<string>()))
            .ReturnsAsync((Car?)null);
        
        _carRepositoryMock.Setup(repo => repo.Update(It.IsAny<Car>()));
        
        _unitOfWorkMock.Setup(uow => uow.CompleteAsync())
            .Returns(Task.CompletedTask);

        var service = CreateCommandService();

        // Act
        var result = await service.Handle(command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(command.Id, result.Id);
        Assert.AreEqual(command.Model, result.Model);
        
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(command.Id), Times.Once);
        _carRepositoryMock.Verify(repo => repo.Update(It.IsAny<Car>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_UpdateCarCommand_WhenCarDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var command = new UpdateCarCommand(
            Id: 999,
            Title: "Updated Model",
            Owner: null,
            OwnerEmail: null,
            Year: 2021,
            BrandId: null,
            Model: "Updated Model",
            Description: null,
            PdfCertification: null,
            ImageUrl: null,
            Price: 30000m,
            LicensePlate: "UPD-123"
        );

        _carRepositoryMock.Setup(repo => repo.FindByIdAsync(command.Id))
            .ReturnsAsync((Car?)null);

        var service = CreateCommandService();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.Handle(command).GetAwaiter().GetResult());

        StringAssert.Contains("Car not found", exception.Message);
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(command.Id), Times.Once);
        _carRepositoryMock.Verify(repo => repo.Update(It.IsAny<Car>()), Times.Never);
    }

    [Test]
    public async Task Handle_DeleteCarCommand_WhenValidCommand_ShouldDeleteCar()
    {
        // Arrange
        var command = new DeleteCarCommand(1);
        var existingCar = CreateTestCar(1);

        _carRepositoryMock.Setup(repo => repo.FindByIdAsync(command.Id))
            .ReturnsAsync(existingCar);
        
        _carRepositoryMock.Setup(repo => repo.Remove(It.IsAny<Car>()));
        
        _unitOfWorkMock.Setup(uow => uow.CompleteAsync())
            .Returns(Task.CompletedTask);

        var service = CreateCommandService();

        // Act
        var result = await service.Handle(command);

        // Assert
        Assert.IsTrue(result);
        
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(command.Id), Times.Once);
        _carRepositoryMock.Verify(repo => repo.Remove(It.IsAny<Car>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Handle_CreateCarCommand_WhenInvalidModel_ShouldThrowException(string invalidModel)
    {
        // Arrange
        var command = new CreateCarCommand(
            Title: invalidModel,
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: invalidModel,
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: "ABC-123",
            OriginalReservationId: 0
        );
        var service = CreateCommandService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Handle(command).GetAwaiter().GetResult());
    }

    [TestCase(1800)] // Too old
    [TestCase(2030)] // Future year
    public async Task Handle_CreateCarCommand_WhenInvalidYear_ShouldThrowException(int invalidYear)
    {
        // Arrange
        var command = new CreateCarCommand(
            Title: "Test Model",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: invalidYear,
            BrandId: 1,
            Model: "Test Model",
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: "ABC-123",
            OriginalReservationId: 0
        );
        var service = CreateCommandService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Handle(command).GetAwaiter().GetResult());
    }

    [TestCase(-1000)]
    public async Task Handle_CreateCarCommand_WhenInvalidPrice_ShouldThrowException(decimal invalidPrice)
    {
        // Arrange
        var command = new CreateCarCommand(
            Title: "Test Model",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: "Test Model",
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: invalidPrice,
            LicensePlate: "ABC-123",
            OriginalReservationId: 0
        );
        var service = CreateCommandService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Handle(command).GetAwaiter().GetResult());

    }

    [Test]
    public async Task Handle_CreateCarCommand_WithZeroPrice_ShouldNotThrow()
    {
        // Arrange
        var command = new CreateCarCommand(
            Title: "Test Model",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: "Test Model",
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 0m,
            LicensePlate: "ABC-123",
            OriginalReservationId: 0
        );
        var brand = CreateTestBrand(1);
        _brandRepositoryMock.Setup(repo => repo.FindBrandByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(brand);

        _carRepositoryMock.Setup(repo => repo.FindCarByLicensePlateAsync(It.IsAny<string>()))
            .ReturnsAsync((Car?)null);

        _carRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Car>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(uow => uow.CompleteAsync())
            .Returns(Task.CompletedTask);

        var service = CreateCommandService();

        // Act & Assert
        Assert.DoesNotThrow(() => service.Handle(command).GetAwaiter().GetResult());
    }

    private ICarCommandService CreateCommandService()
    {
        // Instantiate the real command service with mocked dependencies
        return new CarCommandServiceImpl(_carRepositoryMock.Object, _brandRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    private static Car CreateTestCar(int id, string model = "Test Model")
    {
        var cmd = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: $"Test Title {id}",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: model,
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 25000m,
            LicensePlate: $"TST-{id:000}",
            OriginalReservationId: 0
        );

        var car = new Car(cmd);
        var idProp = typeof(Car).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(car, id);
        return car;
    }

    private static Brand CreateTestBrand(int id)
    {
        return new Brand(id, "Test Brand");
    }
}

// NOTE: Commands are defined in the API project; do not shadow them here.
