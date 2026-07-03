using PlanningPoker.IntegrationTests.Infrastructure;
using Xunit;

namespace PlanningPoker.IntegrationTests;

[Collection("Redis")]
public class SingleClientBehaviorTests_Redis(RedisFixture redis) : SingleClientBehaviorTests(new RedisSingleServerHarness(redis));
