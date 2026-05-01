using Xunit;

// Run all tests sequentially — Playwright browser/context is shared and
// parallel execution causes "Target page, context or browser has been closed".
[assembly: CollectionBehavior(DisableTestParallelization = true)]
