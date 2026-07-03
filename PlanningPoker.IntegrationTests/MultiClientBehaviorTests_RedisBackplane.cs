using PlanningPoker.IntegrationTests.Infrastructure;
using Xunit;

namespace PlanningPoker.IntegrationTests;

[Collection("Redis")]
public class MultiClientBehaviorTests_RedisBackplane(RedisFixture redis) : MultiClientBehaviorTests(new RedisDualServerHarness(redis));
