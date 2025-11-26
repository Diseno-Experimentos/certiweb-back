using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Queries;
using CertiWeb.API.Certifications.Domain.Repositories;
using CertiWeb.API.Certifications.Domain.Services;

namespace CertiWeb.API.Certifications.Application.Internal.QueryServices;

/// <summary>
/// Implementation of the car query service that handles car retrieval operations.
/// </summary>
public class CarQueryServiceImpl(ICarRepository carRepository) : ICarQueryService
{
    // Cache list of cars for the lifetime of this service instance to avoid
    // multiple calls to the repository during a single test or operation.
    private List<Car>? _cachedCars;

    private async Task<List<Car>> GetAllCarsCachedAsync()
    {
        if (_cachedCars == null)
        {
            var all = await carRepository.ListAsync();
            _cachedCars = all?.ToList() ?? new List<Car>();
        }
        return _cachedCars;
    }
    /// <summary>
    /// Retrieves all cars from the system.
    /// </summary>
    /// <param name="query">The query parameters for retrieving all cars.</param>
    /// <returns>A collection of all cars in the system.</returns>
    public async Task<IEnumerable<Car>> Handle(GetAllCarsQuery query)
    {
        return await GetAllCarsCachedAsync();
    }

    /// <summary>
    /// Retrieves a car by its unique identifier.
    /// </summary>
    /// <param name="query">The query containing the car ID to search for.</param>
    /// <returns>The car if found, null otherwise.</returns>
    public async Task<Car?> Handle(GetCarByIdQuery query)
    {
        return await carRepository.FindByIdAsync(query.Id);
    }

    /// <summary>
    /// Retrieves cars by brand ID.
    /// </summary>
    /// <param name="query">The query containing the brand ID to search for.</param>
    /// <returns>A collection of cars for the specified brand.</returns>
    public async Task<IEnumerable<Car>> Handle(GetCarsByBrandQuery query)
    {
        return await carRepository.FindCarsByBrandIdAsync(query.BrandId);
    }
    
    /// <summary>
    /// Retrieves cars by owner email.
    /// </summary>
    /// <param name="query">The query containing the owner email to search for.</param>
    /// <returns>A collection of cars for the specified owner.</returns>
    public async Task<IEnumerable<Car>> Handle(GetCarsByOwnerEmailQuery query)
    {
        return await carRepository.FindCarsByOwnerEmailAsync(query.OwnerEmail);
    }

    public async Task<Car?> Handle(GetCarByLicensePlateQuery query)
    {
        return await carRepository.FindCarByLicensePlateAsync(query.LicensePlate);
    }

    public async Task<IEnumerable<Car>> Handle(GetCarsByYearRangeQuery query)
    {
        var all = await GetAllCarsCachedAsync();
        return all.Where(c => c.Year.Value >= query.FromYear && c.Year.Value <= query.ToYear);
    }

    public async Task<IEnumerable<Car>> Handle(GetCarsByPriceRangeQuery query)
    {
        var all = await GetAllCarsCachedAsync();
        return all.Where(c => c.Price.Value >= query.MinPrice && c.Price.Value <= query.MaxPrice);
    }

    public async Task<IEnumerable<Car>> Handle(SearchCarsQuery query)
    {
        var all = await GetAllCarsCachedAsync();
        return all.Where(c => c.Model != null && c.Model.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(IEnumerable<Car> cars, int totalCount)> Handle(GetCarsWithPaginationQuery query)
    {
        var all = await GetAllCarsCachedAsync();
        var total = all.Count;
        var paged = all.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
        return (paged, total);
    }
}