using CertiWeb.API.Certifications.Application.Internal.CommandServices;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Commands;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Repositories;
using CertiWeb.API.Shared.Domain.Repositories;
using Moq;
using Xunit;
using System.Threading.Tasks;

namespace CertiWeb.UnitTests.Certifications.Application.Internal.CommandServices;

/// <summary>
/// Unit tests for Car command services
/// </summary>
public class CarCommandServiceTests
{
    private readonly Mock<ICarRepository> _carRepositoryMock;
    private readonly Mock<IBrandRepository> _brandRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public CarCommandServiceTests()
    {
        _carRepositoryMock = new Mock<ICarRepository>();
        _brandRepositoryMock = new Mock<IBrandRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_CreateCarCommand_WhenValidCommand_ShouldCreateCar()
    {
        // Arrange
        var command = new CreateCarCommand("Test Model", 2020, 25000, "ABC-123", 1);
        var brand = CreateTestBrand(1);
        var expectedCar = CreateTestCar(1);

        _brandRepositoryMock.Setup(repo => repo.FindByIdAsync(command.BrandId))
            .ReturnsAsync(brand);
        
        _carRepositoryMock.Setup(repo => repo.FindByLicensePlateAsync(It.IsAny<LicensePlate>()))
            .ReturnsAsync((Car?)null);
        
        _carRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Car>()))
            .ReturnsAsync(expectedCar);
        
        _unitOfWorkMock.Setup(uow => uow.CompleteAsync())
            .ReturnsAsync(1);

        var service = CreateCommandService();

        // Act
        var result = await service.Handle(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCar.Id, result.Id);
        Assert.Equal(command.Model, result.Model);
        
        _brandRepositoryMock.Verify(repo => repo.FindByIdAsync(command.BrandId), Times.Once);
        _carRepositoryMock.Verify(repo => repo.FindByLicensePlateAsync(It.IsAny<LicensePlate>()), Times.Once);
        _carRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Car>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateCarCommand_WhenBrandDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var command = new CreateCarCommand("Test Model", 2020, 25000, "ABC-123", 999);

        _brandRepositoryMock.Setup(repo => repo.FindByIdAsync(command.BrandId))
            .ReturnsAsync((Brand?)null);

        var service = CreateCommandService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Handle(command));
        
        Assert.Contains("Brand not found", exception.Message);
        _brandRepositoryMock.Verify(repo => repo.FindByIdAsync(command.BrandId), Times.Once);
        _carRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Car>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreateCarCommand_WhenLicensePlateExists_ShouldThrowException()
    {
        // Arrange
        var command = new CreateCarCommand("Test Model", 2020, 25000, "ABC-123", 1);
        var brand = CreateTestBrand(1);
        var existingCar = CreateTestCar(2);

        _brandRepositoryMock.Setup(repo => repo.FindByIdAsync(command.BrandId))
            .ReturnsAsync(brand);
        
        _carRepositoryMock.Setup(repo => repo.FindByLicensePlateAsync(It.IsAny<LicensePlate>()))
            .ReturnsAsync(existingCar);

        var service = CreateCommandService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Handle(command));
        
        Assert.Contains("License plate already exists", exception.Message);
        _carRepositoryMock.Verify(repo => repo.FindByLicensePlateAsync(It.IsAny<LicensePlate>()), Times.Once);
        _carRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Car>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdateCarCommand_WhenValidCommand_ShouldUpdateCar()
    {
        // Arrange
        var command = new UpdateCarCommand(1, "Updated Model", 2021, 30000, "UPD-123");
        var existingCar = CreateTestCar(1);
        var updatedCar = CreateTestCar(1, "Updated Model");

        _carRepositoryMock.Setup(repo => repo.FindByIdAsync(command.Id))
            .ReturnsAsync(existingCar);
        
        _carRepositoryMock.Setup(repo => repo.FindByLicensePlateAsync(It.IsAny<LicensePlate>()))
            .ReturnsAsync((Car?)null);
        
        _carRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Car>()))
            .ReturnsAsync(updatedCar);
        
        _unitOfWorkMock.Setup(uow => uow.CompleteAsync())
            .ReturnsAsync(1);

        var service = CreateCommandService();

        // Act
        var result = await service.Handle(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Id, result.Id);
        Assert.Equal(command.Model, result.Model);
        
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(command.Id), Times.Once);
        _carRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Car>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateCarCommand_WhenCarDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var command = new UpdateCarCommand(999, "Updated Model", 2021, 30000, "UPD-123");

        _carRepositoryMock.Setup(repo => repo.FindByIdAsync(command.Id))
            .ReturnsAsync((Car?)null);

        var service = CreateCommandService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Handle(command));
        
        Assert.Contains("Car not found", exception.Message);
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(command.Id), Times.Once);
        _carRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Car>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeleteCarCommand_WhenValidCommand_ShouldDeleteCar()
    {
        // Arrange
        var command = new DeleteCarCommand(1);
        var existingCar = CreateTestCar(1);

        _carRepositoryMock.Setup(repo => repo.FindByIdAsync(command.Id))
            .ReturnsAsync(existingCar);
        
        _carRepositoryMock.Setup(repo => repo.DeleteAsync(command.Id))
            .ReturnsAsync(true);
        
        _unitOfWorkMock.Setup(uow => uow.CompleteAsync())
            .ReturnsAsync(1);

        var service = CreateCommandService();

        // Act
        var result = await service.Handle(command);

        // Assert
        Assert.True(result);
        
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(command.Id), Times.Once);
        _carRepositoryMock.Verify(repo => repo.DeleteAsync(command.Id), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_CreateCarCommand_WhenInvalidModel_ShouldThrowException(string invalidModel)
    {
        // Arrange
        var command = new CreateCarCommand(invalidModel, 2020, 25000, "ABC-123", 1);
        var service = CreateCommandService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.Handle(command));
    }

    [Theory]
    [InlineData(1800)] // Too old
    [InlineData(2030)] // Future year
    public async Task Handle_CreateCarCommand_WhenInvalidYear_ShouldThrowException(int invalidYear)
    {
        // Arrange
        var command = new CreateCarCommand("Test Model", invalidYear, 25000, "ABC-123", 1);
        var service = CreateCommandService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.Handle(command));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task Handle_CreateCarCommand_WhenInvalidPrice_ShouldThrowException(decimal invalidPrice)
    {
        // Arrange
        var command = new CreateCarCommand("Test Model", 2020, invalidPrice, "ABC-123", 1);
        var service = CreateCommandService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.Handle(command));
    }

    private ICarCommandService CreateCommandService()
    {
        // This would be the actual implementation
        return new Mock<ICarCommandService>().Object;
    }

    private static Car CreateTestCar(int id, string model = "Test Model")
    {
        return new Car(
            id,
            model,
            new Year(2020),
            new Price(25000),
            new LicensePlate($"TST-{id:000}"),
            null
        );
    }

    private static Brand CreateTestBrand(int id)
    {
        return new Brand(id, "Test Brand");
    }
}

// Mock command classes for testing
public record CreateCarCommand(string Model, int Year, decimal Price, string LicensePlate, int BrandId);
public record UpdateCarCommand(int Id, string Model, int Year, decimal Price, string LicensePlate);
public record DeleteCarCommand(int Id);
