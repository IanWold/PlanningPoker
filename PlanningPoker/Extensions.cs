namespace PlanningPoker;

public static class Extensions {
    extension(Task task) {
        public void Forget() {
            async static Task ForgetAwaited(Task task) =>
                await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            if (!task.IsCompleted || task.IsFaulted) {
                _ = ForgetAwaited(task);
            }
        }
    }
}
