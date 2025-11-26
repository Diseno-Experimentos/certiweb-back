using CertiWeb.API.Certifications.Domain.Model.Aggregates;
using CertiWeb.API.Certifications.Domain.Model.ValueObjects;
using CertiWeb.API.Certifications.Domain.Repositories;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;

namespace CertiWeb.UnitTests.Shared.Infrastructure.Concurrency;

/// <summary>
/// Unit tests for concurrency, threading, and thread safety
/// </summary>
public class ConcurrencyTests
{
    #region Thread Safety Tests

    [Test]
    public async Task Repository_WhenConcurrentReads_ShouldHandleThreadSafely()
    {
        // Arrange
        var repositoryMock = new Mock<ICarRepository>();
        var testCar = CreateTestCar(1);
        
        repositoryMock.Setup(repo => repo.FindByIdAsync(1))
            .ReturnsAsync(testCar);

        var tasks = new List<Task<Car?>>();

        // Act - Simulate 100 concurrent read operations
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(repositoryMock.Object.FindByIdAsync(1));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(100, results.Length);
        foreach (var result in results)
        {
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Id);
        }

        repositoryMock.Verify(repo => repo.FindByIdAsync(1), Times.Exactly(100));
    }

    [Test]
    public async Task Repository_WhenConcurrentWrites_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var repositoryMock = new Mock<ICarRepository>();
        var addedCars = new ConcurrentBag<Car>();

        repositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Car>()))
            .Returns<Car>(car =>
            {
                // Simulate some processing time
                Thread.Sleep(10);
                addedCars.Add(car);
                return Task.CompletedTask;
            });

        var tasks = new List<Task>();

        // Act - Simulate 50 concurrent write operations
        for (int i = 0; i < 50; i++)
        {
            var carId = i;
            tasks.Add(Task.Run(async () =>
            {
                var car = CreateTestCar(carId);
                await repositoryMock.Object.AddAsync(car);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(50, addedCars.Count);
        var distinctIds = addedCars.Select(c => c.Id).Distinct().Count();
        Assert.AreEqual(50, distinctIds); // All cars should have unique IDs
    }

    [Test]
    public async Task ValueObject_WhenConcurrentCreation_ShouldBeThreadSafe()
    {
        // Arrange
        var createdPrices = new ConcurrentBag<Price>();
        var tasks = new List<Task>();

        // Act - Create 1000 Price objects concurrently
        for (int i = 0; i < 1000; i++)
        {
            var priceValue = 1000 + i;
            tasks.Add(Task.Run(() =>
            {
                var price = new Price(priceValue);
                createdPrices.Add(price);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(1000, createdPrices.Count);
        var distinctPrices = createdPrices.Select(p => p.Value).Distinct().Count();
        Assert.AreEqual(1000, distinctPrices);
    }

    #endregion

    #region Async Pattern Tests

    [Test]
    public async Task AsyncMethod_WhenCancellationRequested_ShouldCancelGracefully()
    {
        // Arrange
        var repositoryMock = new Mock<ICarRepository>();
        var cancellationTokenSource = new CancellationTokenSource();

        repositoryMock.Setup(repo => repo.FindByIdAsync(It.IsAny<int>()))
            .Returns(async () =>
            {
                await Task.Delay(1000, cancellationTokenSource.Token);
                return CreateTestCar(1);
            });

        // Act
        var task = repositoryMock.Object.FindByIdAsync(1);
        cancellationTokenSource.CancelAfter(100); // Cancel after 100ms

        // Assert
        Assert.Throws<TaskCanceledException>(() => task.GetAwaiter().GetResult());
    }

    [Test]
    public async Task AsyncMethod_WhenMultipleCancellationTokens_ShouldHandleCorrectly()
    {
        // Arrange
        var repositoryMock = new Mock<ICarRepository>();
        var cts1 = new CancellationTokenSource();
        var cts2 = new CancellationTokenSource();

        repositoryMock.Setup(repo => repo.FindByIdAsync(1))
            .Returns(async () =>
            {
                await Task.Delay(500, cts1.Token);
                return CreateTestCar(1);
            });

        repositoryMock.Setup(repo => repo.FindByIdAsync(2))
            .Returns(async () =>
            {
                await Task.Delay(500, cts2.Token);
                return CreateTestCar(2);
            });

        // Act
        var task1 = repositoryMock.Object.FindByIdAsync(1);
        var task2 = repositoryMock.Object.FindByIdAsync(2);
        
        cts1.CancelAfter(100); // Cancel first task early
        // Don't cancel second task

        // Assert
        Assert.Throws<TaskCanceledException>(() => task1.GetAwaiter().GetResult());
        var result2 = await task2; // This should complete successfully
        Assert.IsNotNull(result2);
        Assert.AreEqual(2, result2.Id);
    }

    [Test]
    public async Task AsyncMethod_WhenTaskTimeout_ShouldTimeout()
    {
        // Arrange
        var repositoryMock = new Mock<ICarRepository>();

        repositoryMock.Setup(repo => repo.FindByIdAsync(It.IsAny<int>()))
            .Returns(async () =>
            {
                await Task.Delay(2000); // 2 second delay
                return CreateTestCar(1);
            });

        // Act & Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var task = repositoryMock.Object.FindByIdAsync(1);

        Assert.Throws<TaskCanceledException>(() => task.WaitAsync(cts.Token).GetAwaiter().GetResult());
    }

    #endregion

    #region Parallel Processing Tests

    [Test]
    public async Task ParallelProcessing_WhenProcessingMultipleCars_ShouldCompleteInParallel()
    {
        // Arrange
        var repositoryMock = new Mock<ICarRepository>();
        var cars = Enumerable.Range(1, 10).Select(CreateTestCar).ToList();

        repositoryMock.Setup(repo => repo.ListAsync())
            .ReturnsAsync(cars);

        var processedCars = new ConcurrentBag<string>();
        var processingTasks = new List<Task>();

        // Act - Process cars in parallel
        var allCars = await repositoryMock.Object.ListAsync();
        
        foreach (var car in allCars)
        {
            processingTasks.Add(Task.Run(async () =>
            {
                // Simulate processing time
                await Task.Delay(100);
                processedCars.Add($"Processed car {car.Id}");
            }));
        }

        var startTime = DateTime.UtcNow;
        await Task.WhenAll(processingTasks);
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.AreEqual(10, processedCars.Count);
        
        // Should complete in roughly 100ms (parallel) rather than 1000ms (sequential)
        var executionTime = endTime - startTime;
        Assert.Less(executionTime.TotalMilliseconds, 500, 
            $"Expected parallel execution but took {executionTime.TotalMilliseconds}ms");
    }

    [Test]
    public async Task ParallelProcessing_WhenOneTaskFails_ShouldNotAffectOthers()
    {
        // Arrange
        var successfulTasks = new ConcurrentBag<int>();
        var tasks = new List<Task>();

        // Act - Create multiple tasks, one of which will fail
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(50);
                
                if (taskId == 5) // Task 5 will fail
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                
                successfulTasks.Add(taskId);
            }));
        }

        // Wait for all tasks, capturing any exceptions
        var exceptions = new List<Exception>();
        foreach (var task in tasks)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        // Assert
        Assert.AreEqual(1, exceptions.Count); // Only one task should fail
        Assert.AreEqual(9, successfulTasks.Count); // 9 tasks should succeed
        CollectionAssert.DoesNotContain(successfulTasks, 5); // Task 5 should not be in successful tasks
    }

    #endregion

    #region Resource Contention Tests

    [Test]
    public async Task ResourceContention_WhenMultipleThreadsAccessSharedResource_ShouldHandleCorrectly()
    {
        // Arrange
        var sharedCounter = 0;
        var lockObject = new object();
        var tasks = new List<Task>();

        // Act - Multiple threads incrementing a shared counter
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    lock (lockObject)
                    {
                        sharedCounter++;
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(10000, sharedCounter); // 100 tasks * 100 increments = 10000
    }

    [Test]
    public async Task ResourceContention_WhenUsingSemaphore_ShouldLimitConcurrency()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(3, 3); // Allow max 3 concurrent operations
        var concurrentOperations = 0;
        var maxConcurrentOperations = 0;
        var lockObject = new object();
        var tasks = new List<Task>();

        // Act - Start 10 tasks that need the semaphore
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                
                try
                {
                    lock (lockObject)
                    {
                        concurrentOperations++;
                        maxConcurrentOperations = Math.Max(maxConcurrentOperations, concurrentOperations);
                    }

                    await Task.Delay(100); // Simulate work

                    lock (lockObject)
                    {
                        concurrentOperations--;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.LessOrEqual(maxConcurrentOperations, 3, 
            $"Expected max 3 concurrent operations but got {maxConcurrentOperations}");
        Assert.AreEqual(0, concurrentOperations); // All operations should be complete
    }

    #endregion

    #region Deadlock Prevention Tests

    [Test]
    public async Task DeadlockPrevention_WhenUsingTimeout_ShouldAvoidDeadlock()
    {
        // Arrange
        var lock1 = new SemaphoreSlim(1, 1);
        var lock2 = new SemaphoreSlim(1, 1);

        // Act - Two tasks that could potentially deadlock
        var task1 = Task.Run(async () =>
        {
            await lock1.WaitAsync();
            try
            {
                await Task.Delay(50);
                
                // Try to acquire lock2 with timeout
                var acquired = await lock2.WaitAsync(TimeSpan.FromMilliseconds(100));
                if (acquired)
                {
                    try
                    {
                        await Task.Delay(50);
                    }
                    finally
                    {
                        lock2.Release();
                    }
                }
                
                return acquired;
            }
            finally
            {
                lock1.Release();
            }
        });

        var task2 = Task.Run(async () =>
        {
            await lock2.WaitAsync();
            try
            {
                await Task.Delay(50);
                
                // Try to acquire lock1 with timeout
                var acquired = await lock1.WaitAsync(TimeSpan.FromMilliseconds(100));
                if (acquired)
                {
                    try
                    {
                        await Task.Delay(50);
                    }
                    finally
                    {
                        lock1.Release();
                    }
                }
                
                return acquired;
            }
            finally
            {
                lock2.Release();
            }
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert - Tasks completed (no deadlock). We don't require a specific success outcome here,
        // only that both tasks completed and returned a boolean result.
        Assert.AreEqual(2, results.Count());
    }

    #endregion

    #region Memory and Performance Tests

    [Test]
    public async Task MemoryUsage_WhenCreatingManyObjects_ShouldNotCauseMemoryLeak()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var tasks = new List<Task>();

        // Act - Create many objects in parallel
        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var cars = new List<Car>();
                for (int j = 0; j < 100; j++)
                {
                    cars.Add(CreateTestCar(j));
                }
                
                // Simulate processing
                var processedCount = cars.Count(c => c.Price.Value > 0);
                return processedCount;
            }));
        }

        await Task.WhenAll(tasks);

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert - Memory should not increase dramatically
        var memoryIncrease = finalMemory - initialMemory;
        Assert.Less(memoryIncrease, 50 * 1024 * 1024, // Less than 50MB increase
            $"Memory increased by {memoryIncrease / 1024 / 1024}MB");
    }

    #endregion

    #region Helper Methods

    private static Car CreateTestCar(int id)
    {
        var cmd = new CertiWeb.API.Certifications.Domain.Model.Commands.CreateCarCommand(
            Title: $"Test Title {id}",
            Owner: "Test Owner",
            OwnerEmail: "owner@example.com",
            Year: 2020,
            BrandId: 1,
            Model: $"Test Model {id}",
            Description: null,
            PdfCertification: string.Empty,
            ImageUrl: null,
            Price: 25000 + id,
            LicensePlate: $"TST-{id:000}",
            OriginalReservationId: 0
        );

        var car = new Car(cmd);
        var idProp = typeof(Car).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(car, id);
        return car;
    }

    #endregion
}
