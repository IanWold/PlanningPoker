using System.Threading.Tasks;

namespace PlanningPoker.Client;

public interface IEncryptionService {
    Task<string> DecryptAsync(string value);
    Task<string> EncryptAsync(string value);
    Task<string> GetKeyAsync();
}
