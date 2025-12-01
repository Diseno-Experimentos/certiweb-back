using CertiWeb.API.Certifications.Application.Internal.QueryServices;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Repositories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CertiWeb.UnitTests.Certifications.Application.Internal.QueryServices;

[TestFixture]
public class BrandQueryServiceTests
{
    private Mock<IBrandRepository> _brandRepositoryMock;
    private BrandQueryServiceImpl _brandQueryService;

    [SetUp]
    public void SetUp()
    {
        _brandRepositoryMock = new Mock<IBrandRepository>();
        _brandQueryService = new BrandQueryServiceImpl(_brandRepositoryMock.Object);
    }

    [Test]
    public async Task GetAllActiveBrandsAsync_ShouldReturnAllBrands()
    {
        // Arrange
        var expectedBrands = new List<Brand>
        {
            new Brand("Toyota"),
            new Brand("Honda"),
            new Brand("Ford")
        };
        _brandRepositoryMock.Setup(repo => repo.GetActiveBrandsAsync())
            .ReturnsAsync(expectedBrands);

        // Act
        var result = await _brandQueryService.GetAllActiveBrandsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedBrands);
        _brandRepositoryMock.Verify(repo => repo.GetActiveBrandsAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllActiveBrandsAsync_WhenNoBrands_ShouldReturnEmptyCollection()
    {
        // Arrange
        _brandRepositoryMock.Setup(repo => repo.GetActiveBrandsAsync())
            .ReturnsAsync(new List<Brand>());

        // Act
        var result = await _brandQueryService.GetAllActiveBrandsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetBrandByIdAsync_WhenBrandExists_ShouldReturnBrand()
    {
        // Arrange
        var expectedBrand = new Brand("Tesla");
        _brandRepositoryMock.Setup(repo => repo.FindByIdAsync(1))
            .ReturnsAsync(expectedBrand);

        // Act
        var result = await _brandQueryService.GetBrandByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedBrand);
        result!.Name.Should().Be("Tesla");
        _brandRepositoryMock.Verify(repo => repo.FindByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetBrandByIdAsync_WhenBrandDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _brandRepositoryMock.Setup(repo => repo.FindByIdAsync(999))
            .ReturnsAsync((Brand?)null);

        // Act
        var result = await _brandQueryService.GetBrandByIdAsync(999);

        // Assert
        result.Should().BeNull();
        _brandRepositoryMock.Verify(repo => repo.FindByIdAsync(999), Times.Once);
    }

    [Test]
    public async Task GetBrandByIdAsync_WithNegativeId_ShouldCallRepository()
    {
        // Arrange
        _brandRepositoryMock.Setup(repo => repo.FindByIdAsync(-1))
            .ReturnsAsync((Brand?)null);

        // Act
        var result = await _brandQueryService.GetBrandByIdAsync(-1);

        // Assert
        result.Should().BeNull();
        _brandRepositoryMock.Verify(repo => repo.FindByIdAsync(-1), Times.Once);
    }

    [Test]
    public async Task GetBrandByIdAsync_WithZeroId_ShouldCallRepository()
    {
        // Arrange
        _brandRepositoryMock.Setup(repo => repo.FindByIdAsync(0))
            .ReturnsAsync((Brand?)null);

        // Act
        var result = await _brandQueryService.GetBrandByIdAsync(0);

        // Assert
        result.Should().BeNull();
        _brandRepositoryMock.Verify(repo => repo.FindByIdAsync(0), Times.Once);
    }
}
