using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class KillLogJob : FtpJob, IJob
    {
        private readonly ILogger<KillLogJob> _logger;
        private readonly IPlayerRepository _playerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public KillLogJob(
            ILogger<KillLogJob> logger,
            IFtpService ftpService,
            IReaderRepository reader,
            IScumServerRepository scumServerRepository,
            IPlayerRepository playerRepository,
            IUnitOfWork unitOfWork) : base(reader, ftpService, scumServerRepository, "kill_")
        {
            _logger = logger;
            _playerRepository = playerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Triggered KillLogJob->Execute at: {time}", DateTimeOffset.Now);

                var server = await GetServerAsync(context);
                foreach (var fileName in GetLogFiles(server.Ftp!))
                {
                    _logger.LogInformation("KillLogJob->Execute Reading file: {}", fileName);
                    var resolvedLines = (await GetUnreadFileLinesAsync(server.Ftp!, fileName))
                        .Where(line => !line.Value.Contains("Game version") || !string.IsNullOrWhiteSpace(line.Value));

                    using (var enumerator = resolvedLines.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            var first = enumerator.Current;
                            if (!enumerator.MoveNext()) break;
                            var second = enumerator.Current;

                            var players = await _playerRepository.GetAllByServerId(server.Id);
                            var kill = new KillLogParser(players).Parse(first.Value, second.Value);
                            _unitOfWork.ScumServers.Attach(server);
                            await _unitOfWork.Kills.AddAsync(kill);
                            await _unitOfWork.SaveAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
