using Timer = System.Timers.Timer;

namespace PlanningPoker.Client;

public class Toast {
    readonly Timer _timer = new(5000);

    public string Message { get; init; }

    public DateTime Time { get; } = DateTime.Now;

    public bool IsExpired { get; set; }

    public Toast(string message, EventHandler? stateChanged) {
        Message = message;

        _timer.Elapsed += (_, _) => {
            IsExpired = true;
            stateChanged?.Invoke(this, EventArgs.Empty);
            _timer.Dispose();
        };
        _timer.Start();
    }
}
