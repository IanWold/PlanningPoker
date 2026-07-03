using PlanningPoker.IntegrationTests.Infrastructure;

namespace PlanningPoker.IntegrationTests;

public class MultiClientBehaviorTests_InMemory() : MultiClientBehaviorTests(new InMemorySingleServerHarness());
