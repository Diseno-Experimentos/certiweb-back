using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.Commands;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Services;
using CertiWeb.API.Shared.Domain.Repositories;
using CertiWeb.API.Certifications.Domain.Repositories;

namespace CertiWeb.API.Certifications.Application.Internal.CommandServices;

/// <summary>
/// Implementation of the car command service that handles car creation and update operations.
/// </summary>
public class CarCommandServiceImpl(ICarRepository carRepository, IBrandRepository brandRepository, IUnitOfWork unitOfWork) : ICarCommandService
{
    /// <summary>
    /// Handles the creation of a new car certification in the system.
    /// </summary>
    /// <param name="command">The command containing the car creation data.</param>
    /// <returns>The created car if successful, null if an error occurs.</returns>
    public async Task<Car?> Handle(CreateCarCommand command)
    {
        Console.WriteLine($"Creating car with data: Title={command.Title}, Owner={command.Owner}, Year={command.Year}, BrandId={command.BrandId}, Model={command.Model}, Price={command.Price}, LicensePlate={command.LicensePlate}, OriginalReservationId={command.OriginalReservationId}");
        Console.WriteLine($"PdfCertification length: {command.PdfCertification?.Length ?? 0}");

        // Let value object constructors throw ArgumentException for invalid inputs
        var car = new Car(command);

        // Validate simple required string fields early so tests receive ArgumentException
        if (string.IsNullOrWhiteSpace(car.Model))
            throw new ArgumentException("Model cannot be empty", nameof(command.Model));

        var brand = await brandRepository.FindBrandByIdAsync(command.BrandId);
        if (brand == null)
        {
            Console.WriteLine($"Brand with ID {command.BrandId} not found");
            throw new InvalidOperationException("Brand not found");
        }

        var existingCar = await carRepository.FindCarByReservationIdAsync(command.OriginalReservationId);
        if (existingCar != null)
        {
            Console.WriteLine($"Reservation {command.OriginalReservationId} already used");
            throw new ArgumentException("Reservation already used", nameof(command.OriginalReservationId));
        }

        var existingLicensePlate = await carRepository.FindCarByLicensePlateAsync(command.LicensePlate);
        if (existingLicensePlate != null)
        {
            Console.WriteLine($"License plate {command.LicensePlate} already exists");
            throw new InvalidOperationException("License plate already exists");
        }

        car.Brand = brand;

        // When running under high-concurrency tests, EF and SQLite may throw transient
        // errors (e.g., database is locked, busy, or internal function registration
        // conflicts). Implement a short retry policy to make CreateCar resilient.
        var maxAttempts = 5;
        var attempt = 0;
        while (true)
        {
            try
            {
                await carRepository.AddAsync(car);
                await unitOfWork.CompleteAsync();
                break; // success
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                Console.WriteLine($"Database update exception while creating car (attempt {attempt + 1}): {dbEx.Message}");

                // Map known unique constraint/database-level violations to business errors
                var inner = dbEx.InnerException;
                if (inner != null)
                {
                    var msg = inner.Message ?? string.Empty;
                    if (msg.Contains("license_plate", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("License plate already exists");
                    }
                    if (msg.Contains("original_reservation_id", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("Reservation already used", nameof(command.OriginalReservationId));
                    }

                    // Detect transient SQLite busy/locked errors and retry by inspecting the message
                    var innerMsg = inner.Message ?? string.Empty;
                    if (innerMsg.IndexOf("SQLite Error 5", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        innerMsg.IndexOf("database is locked", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        innerMsg.IndexOf("unable to delete/modify user-function", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        attempt++;
                        if (attempt >= maxAttempts)
                        {
                            Console.WriteLine($"Exceeded retry attempts while creating car due to SQLite transient errors: {innerMsg}");
                            throw new InvalidOperationException("An error occurred while saving the car to the database", dbEx);
                        }
                        // Backoff a bit before retrying
                        await Task.Delay(20 * attempt);
                        continue; // retry
                    }
                }

                throw new InvalidOperationException("An error occurred while saving the car to the database", dbEx);
            }
        }

        Console.WriteLine($"Car created successfully with ID: {car.Id}");
        return car;
    }
    
    /// <summary>
    /// Handles the update of an existing car certification in the system.
    /// </summary>
    /// <param name="command">The command containing the car update data.</param>
    /// <returns>The updated car if successful, null if an error occurs.</returns>
    public async Task<Car?> Handle(UpdateCarCommand command)
    {
        Console.WriteLine($"Updating car with ID: {command.Id}");

        var existingCar = await carRepository.FindByIdAsync(command.Id);
        if (existingCar == null)
        {
            Console.WriteLine($"Car with ID {command.Id} not found");
            throw new InvalidOperationException("Car not found");
        }

        if (command.BrandId.HasValue)
        {
            // Only fetch brand if the requested brand differs from current BrandId
            if (existingCar.BrandId != command.BrandId.Value)
            {
                var brand = await brandRepository.FindBrandByIdAsync(command.BrandId.Value);
                if (brand == null)
                {
                    Console.WriteLine($"Brand with ID {command.BrandId} not found");
                    throw new ArgumentException("Brand not found", nameof(command.BrandId));
                }
                existingCar.Brand = brand;
            }
        }

        if (!string.IsNullOrEmpty(command.LicensePlate) && command.LicensePlate != existingCar.LicensePlate.Value)
        {
            var existingLicensePlate = await carRepository.FindCarByLicensePlateAsync(command.LicensePlate);
            if (existingLicensePlate != null)
            {
                Console.WriteLine($"License plate {command.LicensePlate} already exists");
                throw new ArgumentException("License plate already exists", nameof(command.LicensePlate));
            }
        }

        if (!string.IsNullOrEmpty(command.Title))
            existingCar.Title = command.Title;

        if (!string.IsNullOrEmpty(command.Owner))
            existingCar.Owner = command.Owner;

        if (!string.IsNullOrEmpty(command.OwnerEmail))
            existingCar.OwnerEmail = command.OwnerEmail;

        if (command.Year.HasValue)
            existingCar.Year = new Year(command.Year.Value);

        if (!string.IsNullOrEmpty(command.Model))
            existingCar.Model = command.Model;

        if (command.Description != null)
            existingCar.Description = command.Description;

        if (!string.IsNullOrEmpty(command.PdfCertification))
            existingCar.PdfCertification = new PdfCertification(command.PdfCertification);

        if (command.ImageUrl != null)
            existingCar.ImageUrl = command.ImageUrl;

        if (command.Price.HasValue)
            existingCar.Price = new Price(command.Price.Value);

        if (!string.IsNullOrEmpty(command.LicensePlate))
            existingCar.LicensePlate = new LicensePlate(command.LicensePlate);

        carRepository.Update(existingCar);
        try
        {
            await unitOfWork.CompleteAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            Console.WriteLine($"Database update exception while updating car: {dbEx.Message}");
            if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("license_plate", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("License plate already exists");
            }
            throw new InvalidOperationException("An error occurred while updating the car in the database");
        }

        Console.WriteLine($"Car updated successfully with ID: {existingCar.Id}");
        return existingCar;
    }

    /// <summary>
    /// Handles the deletion of a car certification.
    /// </summary>
    /// <param name="command">The command containing the car ID to delete.</param>
    /// <returns>True if the car was deleted successfully, false otherwise.</returns>
    public async Task<bool> Handle(DeleteCarCommand command)
    {
        try
        {
            var car = await carRepository.FindByIdAsync(command.Id);
            if (car == null)
            {
                Console.WriteLine($"Car with ID {command.Id} not found for deletion");
                return false;
            }

            carRepository.Remove(car);
            await unitOfWork.CompleteAsync();
            
            Console.WriteLine($"Car with ID {command.Id} deleted successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting car with ID {command.Id}: {ex.Message}");
            return false;
        }
    }
}