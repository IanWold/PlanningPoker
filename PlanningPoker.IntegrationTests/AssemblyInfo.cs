using System.Diagnostics.CodeAnalysis;
using Xunit;

// PlanningPokerFactory configures RedisStore-vs-InMemoryStore selection via a process-wide environment
// variable (see PlanningPokerFactory.cs), so tests in this assembly must never run concurrently with
// each other - parallelism would let one test's connection string bleed into another's host startup.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "<Pending>", Scope = "namespace", Target = "~N:PlanningPoker.IntegrationTests")]
