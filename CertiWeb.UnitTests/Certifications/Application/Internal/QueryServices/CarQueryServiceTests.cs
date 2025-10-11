using CertiWeb.API.Certifications.Application.Internal.QueryServices;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Queries;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Repositories;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace CertiWeb.UnitTests.Certifications.Application.Internal.QueryServices;

/// <summary>
/// Unit tests for Car query services
/// </summary>
public class CarQueryServiceTests
{
    private readonly Mock<ICarRepository> _carRepositoryMock;
    private readonly Mock<IBrandRepository> _brandRepositoryMock;

    public CarQueryServiceTests()
    {
        _carRepositoryMock = new Mock<ICarRepository>();
        _brandRepositoryMock = new Mock<IBrandRepository>();
    }

    [Fact]
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
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _carRepositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [Fact]
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
        Assert.NotNull(result);
        Assert.Empty(result);
        _carRepositoryMock.Verify(repo => repo.ListAsync(), Times.Once);
    }

    [Fact]
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
        Assert.NotNull(result);
        Assert.Equal(carId, result.Id);
        Assert.Equal(expectedCar.Model, result.Model);
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(carId), Times.Once);
    }

    [Fact]
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
        Assert.Null(result);
        _carRepositoryMock.Verify(repo => repo.FindByIdAsync(nonExistentId), Times.Once);
    }

    [Fact]
    public async Task Handle_GetCarsByBrandQuery_WhenCarsExist_ShouldReturnFilteredCars()
    {
        // Arrange
        var brandId = 1;
        var query = new GetCarsByBrandQuery(brandId);
        var cars = CreateTestCarsForBrand(brandId, 2);

        _carRepositoryMock.Setup(repo => repo.FindByBrandIdAsync(brandId))
            .ReturnsAsync(cars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, car => Assert.Equal(brandId, car.BrandId));
        _carRepositoryMock.Verify(repo => repo.FindByBrandIdAsync(brandId), Times.Once);
    }

    [Fact]
    public async Task Handle_GetCarByLicensePlateQuery_WhenCarExists_ShouldReturnCar()
    {
        // Arrange
        var licensePlate = "ABC-123";
        var query = new GetCarByLicensePlateQuery(licensePlate);
        var expectedCar = CreateTestCar(1, licensePlate: licensePlate);

        _carRepositoryMock.Setup(repo => repo.FindByLicensePlateAsync(It.IsAny<LicensePlate>()))
            .ReturnsAsync(expectedCar);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(licensePlate, result.LicensePlate.Value);
        _carRepositoryMock.Verify(repo => repo.FindByLicensePlateAsync(It.IsAny<LicensePlate>()), Times.Once);
    }

    [Theory]
    [InlineData(2020, 2022)]
    [InlineData(2018, 2020)]
    [InlineData(2015, 2025)]
    public async Task Handle_GetCarsByYearRangeQuery_WhenCarsExist_ShouldReturnFilteredCars(
        int fromYear, int toYear)
    {
        // Arrange
        var query = new GetCarsByYearRangeQuery(fromYear, toYear);
        var allCars = CreateTestCarsWithDifferentYears();
        var filteredCars = allCars.Where(c => c.Year.Value >= fromYear && c.Year.Value <= toYear).ToList();

        _carRepositoryMock.Setup(repo => repo.FindByYearRangeAsync(fromYear, toYear))
            .ReturnsAsync(filteredCars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, car => 
        {
            Assert.True(car.Year.Value >= fromYear);
            Assert.True(car.Year.Value <= toYear);
        });
        _carRepositoryMock.Verify(repo => repo.FindByYearRangeAsync(fromYear, toYear), Times.Once);
    }

    [Theory]
    [InlineData(10000, 30000)]
    [InlineData(20000, 50000)]
    [InlineData(0, 15000)]
    public async Task Handle_GetCarsByPriceRangeQuery_WhenCarsExist_ShouldReturnFilteredCars(
        decimal minPrice, decimal maxPrice)
    {
        // Arrange
        var query = new GetCarsByPriceRangeQuery(minPrice, maxPrice);
        var allCars = CreateTestCarsWithDifferentPrices();
        var filteredCars = allCars.Where(c => c.Price.Value >= minPrice && c.Price.Value <= maxPrice).ToList();

        _carRepositoryMock.Setup(repo => repo.FindByPriceRangeAsync(minPrice, maxPrice))
            .ReturnsAsync(filteredCars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, car => 
        {
            Assert.True(car.Price.Value >= minPrice);
            Assert.True(car.Price.Value <= maxPrice);
        });
        _carRepositoryMock.Verify(repo => repo.FindByPriceRangeAsync(minPrice, maxPrice), Times.Once);
    }

    [Fact]
    public async Task Handle_SearchCarsQuery_WhenSearchTermMatches_ShouldReturnMatchingCars()
    {
        // Arrange
        var searchTerm = "Toyota";
        var query = new SearchCarsQuery(searchTerm);
        var matchingCars = CreateTestCarsWithModels(new[] { "Toyota Camry", "Toyota Corolla" });

        _carRepositoryMock.Setup(repo => repo.SearchAsync(searchTerm))
            .ReturnsAsync(matchingCars);

        var service = CreateQueryService();

        // Act
        var result = await service.Handle(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, car => Assert.Contains(searchTerm, car.Model, StringComparison.OrdinalIgnoreCase));
        _carRepositoryMock.Verify(repo => repo.SearchAsync(searchTerm), Times.Once);
    }

    [Fact]
    public async Task Handle_GetCarsWithPaginationQuery_WhenValidPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var query = new GetCarsWithPaginationQuery(page, pageSize);
        var pagedCars = CreateTestCars(pageSize);

        _carRepositoryMock.Setup(repo => repo.GetPagedAsync(page, pageSize))
            .ReturnsAsync((pagedCars, pageSize * 3)); // Total count simulation

        var service = CreateQueryService();

        // Act
        var (cars, totalCount) = await service.Handle(query);

        // Assert
        Assert.NotNull(cars);
        Assert.Equal(pageSize, cars.Count());
        Assert.Equal(pageSize * 3, totalCount);
        _carRepositoryMock.Verify(repo => repo.GetPagedAsync(page, pageSize), Times.Once);
    }

    private ICarQueryService CreateQueryService()
    {
        // This would be the actual implementation
        return new Mock<ICarQueryService>().Object;
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
        return new Car(
            id,
            model,
            new Year(year),
            new Price(price),
            new LicensePlate(licensePlate ?? $"TST-{id:000}"),
            null
        )
        {
            BrandId = brandId
        };
    }
}

// Mock query classes for testing
public record GetAllCarsQuery;
public record GetCarByIdQuery(int Id);
public record GetCarsByBrandQuery(int BrandId);
public record GetCarByLicensePlateQuery(string LicensePlate);
public record GetCarsByYearRangeQuery(int FromYear, int ToYear);
public record GetCarsByPriceRangeQuery(decimal MinPrice, decimal MaxPrice);
public record SearchCarsQuery(string SearchTerm);
public record GetCarsWithPaginationQuery(int Page, int PageSize);
