using Timer = System.Timers.Timer;

namespace RagnarokBotWeb.HostedServices.Base
{
    public abstract class TimedHostedService : BackgroundService, IDisposable
    {
        public static Timer Timer;

        public TimedHostedService(TimeSpan time)
        {
            Timer = new Timer(time);
            Timer.Elapsed += async (sender, e) => await Process();
            Timer.AutoReset = true;
            Timer.Enabled = true;
        }

        public abstract Task Process();

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Timer.Start();
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            Timer.Stop();
            base.Dispose();
        }
    }
}
