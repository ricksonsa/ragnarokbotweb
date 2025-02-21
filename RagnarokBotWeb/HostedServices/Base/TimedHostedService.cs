using Timer = System.Timers.Timer;

namespace RagnarokBotWeb.HostedServices.Base
{
    public abstract class TimedHostedService : IHostedService
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Timer.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Timer.Stop();
            return Task.CompletedTask;
        }
    }
}
