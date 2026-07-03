using PlanningPoker.Client;

namespace PlanningPoker.IntegrationTests.Infrastructure;

public class TestEncryptionService : IEncryptionService {
    public Task<string> DecryptAsync(string value) =>
        Task.FromResult(value);

    public Task<string> EncryptAsync(string value) =>
        Task.FromResult(value);

    public Task<string> GetKeyAsync() =>
        Task.FromResult("test-key");
}
