namespace PlanningPoker.Client;

public class ToastStore {
    public event EventHandler? Changed;

    public IEnumerable<Toast> Toasts { get; private set; } = [];

    internal void Add(string message) {
        Toasts = [.. Toasts, new Toast(message, Changed)];
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
