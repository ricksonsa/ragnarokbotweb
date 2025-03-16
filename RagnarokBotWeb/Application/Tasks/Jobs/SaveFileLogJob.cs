using Quartz;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class SaveFileLogJob : IJob
{
    private readonly IFtpService _ftpService;
    private readonly ILogger<ScumFileProcessor> _logger;
    private readonly IScumServerRepository _scumServerRepository;
    private readonly IServiceProvider _serviceProvide;

    public SaveFileLogJob(
        IFtpService ftpService,
        ILogger<ScumFileProcessor> logger,
        IScumServerRepository scumServerRepository,
        IServiceProvider serviceProvide
    )
    {
        _logger = logger;
        _scumServerRepository = scumServerRepository;
        _ftpService = ftpService;
        _serviceProvide = serviceProvide;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var server = await GetServerAsync(context);
        var fileType = GetFileTypeFromContext(context);
        _logger.LogInformation("Triggered SaveLogFileJob to ScumServer: {} and FileType: {}->Execute at: {time}", server.Id, fileType.ToString(), DateTimeOffset.Now);
        
        await new ScumFileProcessor(_serviceProvide, _ftpService, server, fileType).ProcessUnreadFileLines();
    }

    private async Task<ScumServer> GetServerAsync(IJobExecutionContext context)
    {
        var serverId = GetServerIdFromContext(context);

        var server = await _scumServerRepository.FindByIdAsNoTrackingAsync(serverId);
        if (server?.Ftp is null)
            throw new Exception("Invalid server: the server is non existent or does not have a ftp configuration");

        return server;
    }

    private static long GetServerIdFromContext(IJobExecutionContext context)
    {
        return GetValueFromContext<long>(context, "server_id");
    }

    private static EFileType GetFileTypeFromContext(IJobExecutionContext context)
    {
        return Enum.Parse<EFileType>(GetValueFromContext<string>(context, "file_type"));
    }

    private static T GetValueFromContext<T>(IJobExecutionContext context, string key)
    {
        var dataMap = context.JobDetail.JobDataMap;

        if (!dataMap.TryGetValue(key, out var value))
            throw new Exception($"No value found for key: {key}");

        switch (value)
        {
            case T typedValue:
                return typedValue;
            case null:
                throw new Exception($"Null value for key: {key}");
            default:
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to convert key '{key}' to type {typeof(T).Name}", ex);
                }
        }
    }
}