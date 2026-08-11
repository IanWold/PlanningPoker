using PlanningPoker.Client;

namespace PlanningPoker.IntegrationTests;

public static class Extensions {
    extension(SessionStore store) {
        public Task WaitForAsync(Func<bool> condition, TimeSpan? timeout = null) =>
            WaitForChangedAsync(h => store.Changed += h, h => store.Changed -= h, condition, timeout);
    }

    extension(ToastStore store) {
        public Task WaitForAsync(Func<bool> condition, TimeSpan? timeout = null) =>
            WaitForChangedAsync(h => store.Changed += h, h => store.Changed -= h, condition, timeout);
    }

    private static async Task WaitForChangedAsync(Action<EventHandler> subscribe, Action<EventHandler> unsubscribe, Func<bool> condition, TimeSpan? timeout) {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnChanged(object? sender, EventArgs e) {
            if (condition()) {
                completionSource.TrySetResult();
            }
        }

        subscribe(OnChanged);

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
            unsubscribe(OnChanged);
        }
    }
}
