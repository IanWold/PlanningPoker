using PlanningPoker.IntegrationTests.Infrastructure;
using Xunit;

namespace PlanningPoker.IntegrationTests;

[Collection("Redis")]
public class MultiClientBehaviorTests_Redis(RedisFixture redis) : MultiClientBehaviorTests(new RedisSingleServerHarness(redis));
