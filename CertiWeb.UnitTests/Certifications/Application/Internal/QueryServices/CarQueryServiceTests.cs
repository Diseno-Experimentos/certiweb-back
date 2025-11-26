using CertiWeb.API.Certifications.Application.Internal.QueryServices;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Queries;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Repositories;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace CertiWeb.UnitTests.Certifications.Application.Internal.QueryServices;

/// <summary>
/// Unit tests for Car query services
/// </summary>
public class CarQueryServiceTests
{
    private Mock<ICarRepository> _carRepositoryMock;
    private Mock<IBrandRepository> _brandRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _carRepositoryMock = new Mock<ICarRepository>();
        _brandRepositoryMock = new Mock<IBrandRepository>();
    }

    [Test]
    public async Task Handle_GetAllCarsQuery_WhenCarsExist_ShouldReturnAllCars()
    {
        // Arrange
        var query = new GetAllCarsQuery();
        var cars = CreateTestCars(3);

        _carRepositoryMock.Setup(repo => repo.ListAsync())
            .ReturnsAsync(cars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count());
        _carRepositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_GetAllCarsQuery_WhenNoCarsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetAllCarsQuery();
        var emptyCars = new List<Car>();

        _carRepositoryMock.Setup(repo => repo.ListAsync())
            .ReturnsAsync(emptyCars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
        _carRepositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_GetCarByIdQuery_WhenCarExists_ShouldReturnCar()
    {
        // Arrange
        var carId = 1;
        var query = new GetCarByIdQuery(carId);
        var expectedCar = CreateTestCar(carId);

        _carRepositoryMock.Setup(repo => repo.FindByIdAsync(carId))
            .ReturnsAsync(expectedCar);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(carId, result.Id);
        Assert.AreEqual(expectedCar.Model, result.Model);
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(carId), Times.Once);
    }

    [Test]
    public async Task Handle_GetCarByIdQuery_WhenCarDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = 999;
        var query = new GetCarByIdQuery(nonExistentId);

        _carRepositoryMock.Setup(repo => repo.FindByIdAsync(nonExistentId))
            .ReturnsAsync((Car?)null);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNull(result);
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(nonExistentId), Times.Once);
    }

    [Test]
    public async Task Handle_GetCarsByBrandQuery_WhenCarsExist_ShouldReturnFilteredCars()
    {
        // Arrange
        var brandId = 1;
        var query = new GetCarsByBrandQuery(brandId);
        var cars = CreateTestCarsForBrand(brandId, 2);

        _carRepositoryMock.Setup(repo => repo.FindCarsByBrandIdAsync(brandId))
            .ReturnsAsync(cars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(car => car.BrandId == brandId));
        _carRepositoryMock.Verify(repo => repo.FindCarsByBrandIdAsync(brandId), Times.Once);
    }

    [Test]
    public async Task Handle_GetCarByLicensePlateQuery_WhenCarExists_ShouldReturnCar()
    {
        // Arrange
        var licensePlate = "ABC-123";
        var query = new GetCarByLicensePlateQuery(licensePlate);
        var expectedCar = CreateTestCar(1, licensePlate: licensePlate);

        _carRepositoryMock.Setup(repo => repo.FindCarByLicensePlateAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedCar);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(licensePlate, result.LicensePlate.Value);
        _carRepositoryMock.Verify(repo => repo.FindCarByLicensePlateAsync(It.IsAny<string>()), Times.Once);
    }

    [TestCase(2020, 2022)]
    [TestCase(2018, 2020)]
    [TestCase(2015, 2025)]
    public async Task Handle_GetCarsByYearRangeQuery_WhenCarsExist_ShouldReturnFilteredCars(int fromYear, int toYear)
    {
        // Arrange
        var query = new GetCarsByYearRangeQuery(fromYear, toYear);
        var allCars = CreateTestCarsWithDifferentYears();
        var filteredCars = allCars.Where(c => c.Year.Value >= fromYear && c.Year.Value <= toYear).ToList();

        _carRepositoryMock.Setup(repo => repo.ListAsync())
            .ReturnsAsync(allCars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(car => car.Year.Value >= fromYear && car.Year.Value <= toYear));
        _carRepositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [TestCase(10000, 30000)]
    [TestCase(20000, 50000)]
    [TestCase(0, 15000)]
    public async Task Handle_GetCarsByPriceRangeQuery_WhenCarsExist_ShouldReturnFilteredCars(decimal minPrice, decimal maxPrice)
    {
        // Arrange
        var query = new GetCarsByPriceRangeQuery(minPrice, maxPrice);
        var allCars = CreateTestCarsWithDifferentPrices();
        var filteredCars = allCars.Where(c => c.Price.Value >= minPrice && c.Price.Value <= maxPrice).ToList();

        _carRepositoryMock.Setup(repo => repo.ListAsync())
            .ReturnsAsync(allCars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.All(car => car.Price.Value >= minPrice && car.Price.Value <= maxPrice));
        _carRepositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_SearchCarsQuery_WhenSearchTermMatches_ShouldReturnMatchingCars()
    {
        // Arrange
        var searchTerm = "Toyota";
        var query = new SearchCarsQuery(searchTerm);
        var matchingCars = CreateTestCarsWithModels(new[] { "Toyota Camry", "Toyota Corolla" });

        _carRepositoryMock.Setup(repo => repo.ListAsync())
            .ReturnsAsync(matchingCars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(car => car.Model.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        _carRepositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_GetCarsWithPaginationQuery_WhenValidPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var query = new GetCarsWithPaginationQuery(page, pageSize);
        var allCars = CreateTestCars(pageSize * 3);

        _carRepositoryMock.Setup(repo => repo.ListAsync())
            .ReturnsAsync(allCars);

        var service = CreateQueryService();

        // Act
        var (cars, totalCount) = await service.Handle(query);

        // Assert
        Assert.IsNotNull(cars);
        Assert.AreEqual(pageSize, cars.Count());
        Assert.AreEqual(pageSize * 3, totalCount);
        _carRepositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    private ICarQueryService CreateQueryService()
    {
        // Instantiate the real query service with the mocked repository
        return new CarQueryServiceImpl(_carRepositoryMock.Object);
    }

    private static List<Car> CreateTestCars(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestCar(i))
            .ToList();
    }

    private static List<Car> CreateTestCarsForBrand(int brandId, int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestCar(i, brandId: brandId))
            .ToList();
    }

    private static List<Car> CreateTestCarsWithDifferentYears()
    {
        return new[]
        {
            CreateTestCar(1, year: 2018),
            CreateTestCar(2, year: 2020),
            CreateTestCar(3, year: 2022),
            CreateTestCar(4, year: 2024)
        }.ToList();
    }

    private static List<Car> CreateTestCarsWithDifferentPrices()
    {
        return new[]
        {
            CreateTestCar(1, price: 15000),
            CreateTestCar(2, price: 25000),
            CreateTestCar(3, price: 35000),
            CreateTestCar(4, price: 45000)
        }.ToList();
    }

    private static List<Car> CreateTestCarsWithModels(string[] models)
    {
        return models.Select((model, index) => CreateTestCar(index + 1, model: model)).ToList();
    }

    private static Car CreateTestCar(int id, string model = "Test Model", int year = 2020, 
        decimal price = 25000, string licensePlate = null, int brandId = 1)
    {
        var cmd = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: $"Test Title {id}",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: year,
            BrandId: brandId,
            Model: model,
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: price,
            LicensePlate: licensePlate ?? $"TST-{id:000}",
            OriginalReservationId: 0
        );

        var car = new Car(cmd);
        car.BrandId = brandId;
        var idProp = typeof(Car).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(car, id);
        return car;
    }
}

// NOTE: queries are defined in the API project; do not shadow them here.
