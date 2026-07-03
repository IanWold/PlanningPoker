using PlanningPoker.IntegrationTests.Infrastructure;

namespace PlanningPoker.IntegrationTests;

public class SingleClientBehaviorTests_InMemory() : SingleClientBehaviorTests(new InMemorySingleServerHarness());
