using RagnarokBotWeb.Domain.Services.Interfaces;
using System.Text.RegularExpressions;
using Timer = System.Timers.Timer;

namespace RagnarokBotWeb.HostedServices
{
    public class LoginHostedService : BaseHostedService, IHostedService, IDisposable
    {
        private readonly ILogger<LoginHostedService> _logger;
        private readonly IServiceProvider _services;
        private static Timer _timer;

        public LoginHostedService(
            ILogger<LoginHostedService> logger,
            IFtpService ftpService,
            IServiceProvider services) : base(ftpService.GetClient(), "login_")
        {
            _logger = logger;
            _services = services;
            _timer = new Timer(10000); // Set interval to 10 seconds
            _timer.Elapsed += async (sender, e) => await Process();
            _timer.AutoReset = true;  // Keep firing repeatedly
            _timer.Enabled = true;    // Start the timer
        }

        public override async Task Process()
        {
            try
            {
                _logger.LogInformation("Triggered LoginLogTask->Process at: {time}", DateTimeOffset.Now);

                using (var scope = _services.CreateScope())
                {
                    var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

                    foreach (var fileName in GetLogFiles())
                    {
                        foreach (var line in GetUnreadFileLines(fileName))
                        {
                            _logger.LogInformation("LoginLogTask->Process Reading file: " + fileName);
                            if (string.IsNullOrEmpty(line)) continue;

                            var fixedLine = line.Replace("\0", "");
                            Match match1 = Regex.Match(fixedLine, @"(\d+):([A-Za-z]+)");
                            string steamId64 = match1.Success ? match1.Groups[1].Value : "Not found";
                            if (steamId64 == "Not found") continue;

                            string name = match1.Success ? match1.Groups[2].Value : "Not found";

                            Match match2 = Regex.Match(fixedLine, @"\((\d+)\)");
                            string scumId = match2.Success ? match2.Groups[1].Value : "Not found";

                            bool isLoggedIn = fixedLine.Contains("logged in");
                            bool isLoggedOut = fixedLine.Contains("logged out");

                            if (isLoggedIn)
                            {
                                await playerService.PlayerConnected(steamId64, scumId, name);
                            }
                            else if (isLoggedOut)
                            {
                                playerService.PlayerDisconnected(steamId64);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is starting.");
            _timer.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");
            _timer.Stop();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
