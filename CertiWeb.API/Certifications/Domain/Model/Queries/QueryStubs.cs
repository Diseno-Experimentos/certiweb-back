namespace CertiWeb.API.Certifications.Domain.Model.Queries;

// Only declare query types that are not already present in the project.
public record GetCarByLicensePlateQuery(string LicensePlate);
public record GetCarsByYearRangeQuery(int FromYear, int ToYear);
public record GetCarsByPriceRangeQuery(decimal MinPrice, decimal MaxPrice);
public record SearchCarsQuery(string SearchTerm);
public record GetCarsWithPaginationQuery(int Page, int PageSize);
