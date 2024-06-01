namespace PlanningPoker;

public static class Extensions {
    public static void Forget(this Task task) {
        async static Task ForgetAwaited(Task task) =>
            await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (!task.IsCompleted || task.IsFaulted) {
            _ = ForgetAwaited(task);
        }
    }
}
