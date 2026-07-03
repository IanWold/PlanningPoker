using System.Runtime.CompilerServices;

namespace PlanningPoker;

public static class Extensions {
    extension(Task task) {
        public void Forget([CallerMemberName] string? callerMemberName = null) {
            async static Task ForgetAwaited(Task task, string? callerMemberName) {
                try {
                    await task.ConfigureAwait(false);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine($"Unobserved exception in fire-and-forget task from {callerMemberName}: {ex}");
                }
            }

            if (!task.IsCompleted || task.IsFaulted) {
                _ = ForgetAwaited(task, callerMemberName);
            }
        }
    }
}
