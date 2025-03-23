using Quartz;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class KillLogJob : AbstractJob, IJob
{
    private readonly IFtpService _ftpService;
    private readonly ILogger<KillLogJob> _logger;
    private readonly IPlayerRepository _playerRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWork _unitOfWork;

    public KillLogJob(
        ILogger<KillLogJob> logger,
        IFtpService ftpService,
        IScumServerRepository scumServerRepository,
        IPlayerRepository playerRepository,
        IUnitOfWork unitOfWork,
        IServiceProvider serviceProvider) : base(scumServerRepository)
    {
        _logger = logger;
        _playerRepository = playerRepository;
        _unitOfWork = unitOfWork;
        _ftpService = ftpService;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Triggered KillLogJob->Execute at: {time}", DateTimeOffset.Now);

            var server = await GetServerAsync(context);
            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(_serviceProvider, _ftpService, server, fileType);
            var lines = await processor.ProcessUnreadFileLines();

            var resolvedLines = lines
                .Where(line => !line.Contains("Game version") || !string.IsNullOrWhiteSpace(line));

            using var enumerator = resolvedLines.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var first = enumerator.Current;
                if (!enumerator.MoveNext()) break;
                var second = enumerator.Current;

                if (string.IsNullOrWhiteSpace(first) && second.Contains("Game version")) continue;

                var players = await _playerRepository.GetAllByServerId(server.Id);
                var kill = new KillLogParser(server).Parse(first, second);
                _logger.Log(LogLevel.Information, "Adding new kill entry: {} -> {}", kill.KillerName, kill.TargetName);
                _unitOfWork.ScumServers.Attach(server);
                await _unitOfWork.Kills.AddAsync(kill);
                await _unitOfWork.SaveAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}