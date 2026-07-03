using Microsoft.AspNetCore.Mvc.Testing;

namespace PlanningPoker.IntegrationTests;

// Program.cs reads the Redis connection string from configuration and branches before calling Build(),
// which runs too early for WebApplicationFactory's ConfigureWebHost/ConfigureAppConfiguration overrides -
// those are only guaranteed to be visible by the time Build() completes. Environment variables are read
// as part of WebApplicationBuilder.CreateBuilder() itself, earlier than that check, so they're the one
// override mechanism this app's startup code actually observes. This relies on the whole assembly running
// without test parallelization (see AssemblyInfo.cs) since the variable is process-wide.
public class PlanningPokerFactory : WebApplicationFactory<Program> {
    public PlanningPokerFactory(string? redisConnectionString = null) {
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", redisConnectionString);
    }
}
