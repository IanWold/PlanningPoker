using Timer = System.Timers.Timer;

namespace PlanningPoker.Client;

public class ToastState
{
    public class Toast
    {
        readonly Timer _timer = new(5000) { AutoReset = false };

        public string Message { get; init; }

        public Toast(string message, Action<Toast> onExpired)
        {
            Message = message;

            _timer.Elapsed += (_, _) => onExpired(this);
            _timer.Start();
        }
    }

    public IEnumerable<Toast> Toasts { get; private set; } = [];

    public event EventHandler? OnStateChanged;

    public void Add(string message)
    {
        Toasts = [..Toasts, new(message, Dismiss)];
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dismiss(Toast toast)
    {
        Toasts = Toasts.Except([toast]).ToArray();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
