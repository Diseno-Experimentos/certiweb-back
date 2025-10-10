using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.IntegrationTests.Shared.Infrastructure;

namespace CertiWeb.IntegrationTests.Certifications.Domain.Model.Aggregates;

[TestFixture]
public class BrandIntegrationTests : DatabaseTestBase
{
    [Test]
    public async Task CreateBrand_WithValidData_ShouldPersistToDatabase()
    {
        // Arrange
        var brand = new Brand("Toyota");

        // Act
        Context.Brands.Add(brand);
        await Context.SaveChangesAsync();

        // Assert
        var savedBrand = await Context.Brands.FirstOrDefaultAsync(b => b.Name == "Toyota");
        savedBrand.Should().NotBeNull();
        savedBrand!.Name.Should().Be("Toyota");
        savedBrand.IsActive.Should().BeTrue();
        savedBrand.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task CreateMultipleBrands_ShouldPersistAllToDatabase()
    {
        // Arrange
        var brands = new List<Brand>
        {
            new("Honda"),
            new("Nissan"),
            new("Ford")
        };

        // Act
        Context.Brands.AddRange(brands);
        await Context.SaveChangesAsync();

        // Assert
        var savedBrands = await Context.Brands.Where(b => 
            b.Name == "Honda" || b.Name == "Nissan" || b.Name == "Ford").ToListAsync();
        
        savedBrands.Should().HaveCount(3);
        savedBrands.Select(b => b.Name).Should().Contain(new[] { "Honda", "Nissan", "Ford" });
        savedBrands.Should().OnlyContain(b => b.IsActive == true);
    }

    [Test]
    public async Task UpdateBrand_ShouldPersistChangesToDatabase()
    {
        // Arrange
        var brand = new Brand("BMW");
        Context.Brands.Add(brand);
        await Context.SaveChangesAsync();

        // Act
        brand.Name = "BMW Updated";
        brand.IsActive = false;
        await Context.SaveChangesAsync();

        // Assert
        await SaveChangesAndClearContext();
        var updatedBrand = await Context.Brands.FindAsync(brand.Id);
        updatedBrand.Should().NotBeNull();
        updatedBrand!.Name.Should().Be("BMW Updated");
        updatedBrand.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task DeleteBrand_ShouldRemoveFromDatabase()
    {
        // Arrange
        var brand = new Brand("Mercedes");
        Context.Brands.Add(brand);
        await Context.SaveChangesAsync();
        var brandId = brand.Id;

        // Act
        Context.Brands.Remove(brand);
        await Context.SaveChangesAsync();

        // Assert
        var deletedBrand = await Context.Brands.FindAsync(brandId);
        deletedBrand.Should().BeNull();
    }

    [Test]
    public async Task FindBrandById_WithExistingId_ShouldReturnCorrectBrand()
    {
        // Arrange
        var brand = new Brand("Audi");
        Context.Brands.Add(brand);
        await Context.SaveChangesAsync();
        var brandId = brand.Id;

        // Act
        await SaveChangesAndClearContext();
        var foundBrand = await Context.Brands.FindAsync(brandId);

        // Assert
        foundBrand.Should().NotBeNull();
        foundBrand!.Name.Should().Be("Audi");
        foundBrand.IsActive.Should().BeTrue();
        foundBrand.Id.Should().Be(brandId);
    }

    [Test]
    public async Task FindBrandById_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = 99999;

        // Act
        var foundBrand = await Context.Brands.FindAsync(nonExistingId);

        // Assert
        foundBrand.Should().BeNull();
    }

    [Test]
    public async Task QueryBrandsByName_ShouldReturnMatchingBrands()
    {
        // Arrange
        var brands = new List<Brand>
        {
            new("Toyota"),
            new("Toyota Motors"),
            new("Honda"),
            new("Hyundai")
        };
        Context.Brands.AddRange(brands);
        await Context.SaveChangesAsync();

        // Act
        var toyotaBrands = await Context.Brands
            .Where(b => b.Name.Contains("Toyota"))
            .ToListAsync();

        // Assert
        toyotaBrands.Should().HaveCount(2);
        toyotaBrands.Select(b => b.Name).Should().Contain(new[] { "Toyota", "Toyota Motors" });
    }

    [Test]
    public async Task QueryActiveBrands_ShouldReturnOnlyActiveBrands()
    {
        // Arrange
        var activeBrand = new Brand("Volkswagen");
        var inactiveBrand = new Brand("Suzuki") { IsActive = false };
        
        Context.Brands.AddRange(activeBrand, inactiveBrand);
        await Context.SaveChangesAsync();

        // Act
        var activeBrands = await Context.Brands
            .Where(b => b.IsActive)
            .ToListAsync();

        // Assert
        activeBrands.Should().NotContain(b => b.Name == "Suzuki");
        activeBrands.Should().Contain(b => b.Name == "Volkswagen");
    }

    [Test]
    public async Task BrandEntityTracking_ShouldDetectChanges()
    {
        // Arrange
        var brand = new Brand("Mazda");
        Context.Brands.Add(brand);
        await Context.SaveChangesAsync();

        // Act
        brand.Name = "Mazda Updated";
        
        // Assert - Entity should be marked as modified
        var entry = Context.Entry(brand);
        entry.State.Should().Be(EntityState.Modified);
        entry.Property(b => b.Name).IsModified.Should().BeTrue();
    }

    [Test]
    public async Task ConcurrentBrandCreation_ShouldHandleMultipleOperations()
    {
        // Arrange
        var brand1 = new Brand("Kia");
        var brand2 = new Brand("Subaru");

        // Act
        Context.Brands.Add(brand1);
        Context.Brands.Add(brand2);
        await Context.SaveChangesAsync();

        // Assert
        var allBrands = await Context.Brands.ToListAsync();
        allBrands.Should().Contain(b => b.Name == "Kia");
        allBrands.Should().Contain(b => b.Name == "Subaru");
    }
}
