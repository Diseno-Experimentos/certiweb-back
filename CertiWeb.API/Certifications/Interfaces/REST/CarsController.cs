using System.Net.Mime;
using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Commands;
using CertiWeb.API.Certifications.Domain.Model.Queries;
using CertiWeb.API.Certifications.Domain.Services;
using CertiWeb.API.Certifications.Interfaces.REST.Resources;
using CertiWeb.API.Certifications.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CertiWeb.API.Certifications.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Car Certification Endpoints.")]
/// <summary>
/// REST API controller for managing car certification operations.
/// </summary>
public class CarsController(ICarCommandService carCommandService, ICarQueryService carQueryService) : ControllerBase
{
    /// <summary>
    /// Creates a new car certification in the system.
    /// </summary>
    /// <param name="resource">The car creation data.</param>
    /// <returns>The created car resource if successful, BadRequest if creation fails.</returns>
    [HttpPost]
    public async Task<ActionResult<CarResource>> CreateCar([FromBody] CreateCarResource resource)
    {
        Console.WriteLine("--- CREATE CAR ACTION START ---");
        Console.WriteLine($"Received resource: Title='{resource.Title}', Year={resource.Year}, BrandId={resource.BrandId}, LicensePlate='{resource.LicensePlate}'");

        // Basic input validation to provide friendly HTTP 400 responses for invalid inputs
        if (resource.Year < 1900 || resource.Year > DateTime.Now.Year + 1)
        {
            Console.WriteLine($"!!! YEAR VALIDATION FAILED: Year '{resource.Year}' is out of range.");
            return BadRequest(new { message = "Validation error", details = "Year must be between 1900 and current year + 1" });
        }
        if (resource.BrandId <= 0)
        {
            Console.WriteLine($"!!! BRANDID VALIDATION FAILED: BrandId '{resource.BrandId}' is not positive.");
            return BadRequest(new { message = "Validation error", details = "BrandId must be a positive integer" });
        }
        if (resource.Price < 0)
        {
            Console.WriteLine($"!!! PRICE VALIDATION FAILED: Price '{resource.Price}' is negative.");
            return BadRequest(new { message = "Validation error", details = "Price must be non-negative" });
        }
        // Disallow hyphens in license plates for REST API validation layer (system tests expect this behavior)
        if (resource.LicensePlate != null && System.Text.RegularExpressions.Regex.IsMatch(resource.LicensePlate, ".*[-].*"))
        {
            Console.WriteLine($"!!! LICENSE PLATE VALIDATION FAILED: LicensePlate '{resource.LicensePlate}' contains a hyphen.");
            return BadRequest(new { message = "Validation error", details = "License plate cannot contain hyphens or special characters" });
        }
        // Validate pdf certification base64 quick check before creating domain object
        if (!string.IsNullOrWhiteSpace(resource.PdfCertification))
        {
            var tmp = new CertiWeb.API.Certifications.Domain.Model.ValueObjects.PdfCertification(resource.PdfCertification);
            if (!tmp.IsValidBase64())
            {
                Console.WriteLine($"!!! PDF VALIDATION FAILED: PdfCertification is not valid Base64.");
                return BadRequest(new { message = "Validation error", details = "Invalid PDF certification" });
            }
        }

        try
        {
            Console.WriteLine("Validation passed, proceeding to command creation.");
            Console.WriteLine($"Received CreateCarResource: {System.Text.Json.JsonSerializer.Serialize(resource)}");
            
            var createCarCommand = CreateCarCommandFromResourceAssembler.ToCommandFromResource(resource);
            var car = await carCommandService.Handle(createCarCommand);
            
            if (car == null) 
            {
                Console.WriteLine("!!! carCommandService.Handle returned null.");
                return BadRequest(new { 
                    message = "Failed to create car certification.", 
                    details = "Check if reservation ID is already used, license plate already exists, brand ID is valid, or validation requirements are met.",
                    validationRules = new {
                        year = "Must be between 1900 and current year + 1",
                        licensePlate = "Must be between 6 and 10 characters",
                        price = "Must be non-negative",
                        pdfCertification = "Must be a valid Base64 string with at least 10 characters"
                    }
                });
            }
            
            Console.WriteLine($"Car created with ID: {car.Id}. Returning CreatedAtAction.");
            var carResource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);
            return CreatedAtAction(nameof(GetCarById), new { carId = car.Id }, carResource);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error in CreateCar: {ex.Message}");
            return BadRequest(new { 
                message = "Validation error", 
                details = ex.Message,
                parameter = ex.ParamName
            });
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Business error in CreateCar: {ex.Message}");
            return BadRequest(new { message = "Validation error", details = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in CreateCar: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all cars from the system.
    /// </summary>
    /// <returns>A collection of all car resources.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CarResource>>> GetAllCars()
    {
        var getAllCarsQuery = new GetAllCarsQuery();
        var cars = await carQueryService.Handle(getAllCarsQuery);
        var carResources = cars.Select(CarResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(carResources);
    }

    /// <summary>
    /// Retrieves a specific car by its ID.
    /// </summary>
    /// <param name="carId">The ID of the car to retrieve.</param>
    /// <returns>The car resource if found, NotFound if the car doesn't exist.</returns>
    [HttpGet("{carId:int}")]
    public async Task<ActionResult<CarResource>> GetCarById(int carId)
    {
        var getCarByIdQuery = new GetCarByIdQuery(carId);
        var car = await carQueryService.Handle(getCarByIdQuery);
        if (car == null) return NotFound();
        var carResource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);
        return Ok(carResource);
    }

    /// <summary>
    /// Retrieves cars by brand ID.
    /// </summary>
    /// <param name="brandId">The ID of the brand to filter by.</param>
    /// <returns>A collection of car resources for the specified brand.</returns>
    [HttpGet("brand/{brandId:int}")]
    public async Task<ActionResult<IEnumerable<CarResource>>> GetCarsByBrand(int brandId)
    {
        var getCarsByBrandQuery = new GetCarsByBrandQuery(brandId);
        var cars = await carQueryService.Handle(getCarsByBrandQuery);
        var carResources = cars.Select(CarResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(carResources);
    }

    /// <summary>
    /// Retrieves cars by owner email.
    /// </summary>
    /// <param name="ownerEmail">The email of the owner to filter by.</param>
    /// <returns>A collection of car resources for the specified owner.</returns>
    [HttpGet("owner/{ownerEmail}")]
    public async Task<ActionResult<IEnumerable<CarResource>>> GetCarsByOwner(string ownerEmail)
    {
        var getCarsByOwnerQuery = new GetCarsByOwnerEmailQuery(ownerEmail);
        var cars = await carQueryService.Handle(getCarsByOwnerQuery);
        var carResources = cars.Select(CarResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(carResources);
    }

    /// <summary>
    /// Updates an existing car certification in the system.
    /// </summary>
    /// <param name="carId">The ID of the car to update.</param>
    /// <param name="resource">The car update data.</param>
    /// <returns>The updated car resource if successful, BadRequest if update fails.</returns>
    [HttpPatch("{carId:int}")]
    public async Task<ActionResult<CarResource>> UpdateCar(int carId, [FromBody] UpdateCarResource resource)
    {
        // Validate basic properties for update
        if (resource.Year.HasValue && (resource.Year.Value < 1900 || resource.Year.Value > DateTime.Now.Year + 1))
            return BadRequest(new { message = "Validation error", details = "Year must be between 1900 and current year + 1" });
        if (resource.Price.HasValue && resource.Price.Value < 0)
            return BadRequest(new { message = "Validation error", details = "Price must be non-negative" });
        if (!string.IsNullOrEmpty(resource.LicensePlate) && System.Text.RegularExpressions.Regex.IsMatch(resource.LicensePlate, ".*[-].*"))
            return BadRequest(new { message = "Validation error", details = "License plate cannot contain hyphens or special characters" });
        if (!string.IsNullOrWhiteSpace(resource.PdfCertification))
        {
            var tmp = new CertiWeb.API.Certifications.Domain.Model.ValueObjects.PdfCertification(resource.PdfCertification);
            if (!tmp.IsValidBase64())
                return BadRequest(new { message = "Validation error", details = "Invalid PDF certification" });
        }
        try
        {
            Console.WriteLine($"Received UpdateCarResource for car {carId}: {System.Text.Json.JsonSerializer.Serialize(resource)}");
            
            var updateCarCommand = UpdateCarCommandFromResourceAssembler.ToCommandFromResource(resource, carId);
            var car = await carCommandService.Handle(updateCarCommand);
            
            if (car == null) 
            {
                return BadRequest(new { 
                    message = "Failed to update car certification.", 
                    details = "Check if car exists, brand ID is valid, license plate is unique, or validation requirements are met.",
                    validationRules = new {
                        year = "Must be between 1900 and current year + 1",
                        licensePlate = "Must be between 6 and 10 characters and unique",
                        price = "Must be non-negative",
                        pdfCertification = "Must be a valid Base64 string with at least 10 characters"
                    }
                });
            }
            
            var carResource = CarResourceFromEntityAssembler.ToResourceFromEntity(car);
            return Ok(carResource);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error in UpdateCar: {ex.Message}");
            return BadRequest(new { 
                message = "Validation error", 
                details = ex.Message,
                parameter = ex.ParamName
            });
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Business error in UpdateCar: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in UpdateCar: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetCarPdf(int id)
    {
        try
        {
            var getCarByIdQuery = new GetCarByIdQuery(id);
            var car = await carQueryService.Handle(getCarByIdQuery);
            
            if (car == null)
            {
                return NotFound(new { message = "Car not found" });
            }
            
            string pdfData = car.PdfCertification.Base64Data;
            if (!pdfData.StartsWith("data:"))
            {
                pdfData = $"data:application/pdf;base64,{pdfData}";
            }
            
            return Ok(new { pdfCertification = pdfData });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a car certification from the system.
    /// </summary>
    /// <param name="carId">The ID of the car to delete.</param>
    /// <returns>NoContent if successful, NotFound if car doesn't exist.</returns>
    [HttpDelete("{carId:int}")]
    [SwaggerOperation(
        Summary = "Deletes a car certification",
        Description = "Deletes a car certification from the system",
        OperationId = "DeleteCar")]
    [SwaggerResponse(204, "The car was deleted successfully")]
    [SwaggerResponse(404, "The car was not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> DeleteCar(int carId)
    {
        try
        {
            var deleteCarCommand = new DeleteCarCommand(carId);
            var result = await carCommandService.Handle(deleteCarCommand);
            
            if (!result)
            {
                return NotFound(new { message = "Car not found" });
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting car with ID {carId}: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }
}