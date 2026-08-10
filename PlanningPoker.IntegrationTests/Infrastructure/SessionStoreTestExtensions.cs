using PlanningPoker.Client;

namespace PlanningPoker.IntegrationTests;

public static class SessionStoreTestExtensions {
    public static async Task WaitForAsync(this SessionStore store, Func<bool> condition, TimeSpan? timeout = null) {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnChanged(object? sender, EventArgs e) {
            if (condition()) {
                completionSource.TrySetResult();
            }
        }

        store.Changed += OnChanged;

        try {
            if (condition()) {
                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
            using var registration = cancellationTokenSource.Token.Register(() =>
                completionSource.TrySetException(new TimeoutException($"Condition was not met within {timeout ?? TimeSpan.FromSeconds(10)}."))
            );

            await completionSource.Task;
        }
        finally {
            store.Changed -= OnChanged;
        }
    }
}
