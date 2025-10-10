[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(2)] // Lower parallelism for integration tests due to database operations
