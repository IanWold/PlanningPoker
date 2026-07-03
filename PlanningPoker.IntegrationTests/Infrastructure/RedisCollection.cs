using Xunit;

namespace PlanningPoker.IntegrationTests;

// Shares one Redis container across every Redis-backed test class instead of spinning one up per class.
[CollectionDefinition("Redis")]
public class RedisCollection : ICollectionFixture<RedisFixture> { }
