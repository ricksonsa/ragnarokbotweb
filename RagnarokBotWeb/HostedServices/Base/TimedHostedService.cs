using Timer = System.Timers.Timer;

namespace RagnarokBotWeb.HostedServices.Base;

public abstract class TimedHostedService : BackgroundService, IDisposable
{
    private readonly Timer _timer;

    protected TimedHostedService(TimeSpan time)
    {
        _timer = new Timer(time);
        _timer.Elapsed += async (sender, e) => await Process();
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    public override void Dispose()
    {
        _timer.Stop();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    protected abstract Task Process();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer.Start();
        return Task.CompletedTask;
    }
}