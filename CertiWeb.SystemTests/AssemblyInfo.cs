[assembly: Parallelizable(ParallelScope.None)] // System tests should run sequentially to avoid port conflicts
[assembly: LevelOfParallelism(1)]
