using Quartz;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public abstract class AbstractJob
{
    private readonly IScumServerRepository _scumServerRepository;

    protected AbstractJob(IScumServerRepository scumServerRepository)
    {
        _scumServerRepository = scumServerRepository;
    }

    protected async Task<ScumServer> GetServerAsync(IJobExecutionContext context, bool ftpRequired = true)
    {
        var serverId = GetServerIdFromContext(context);
        var server = await _scumServerRepository.FindByIdAsNoTrackingAsync(serverId);
        if (server is null) throw new Exception("Invalid server: server does not exist");
        if (server!.Ftp is null && ftpRequired)
            throw new Exception("Invalid server: server does not have a ftp configuration");
        return server;
    }

    protected static long GetServerIdFromContext(IJobExecutionContext context)
    {
        return GetValueFromContext<long>(context, "server_id");
    }

    protected static EFileType GetFileTypeFromContext(IJobExecutionContext context)
    {
        return Enum.Parse<EFileType>(GetValueFromContext<string>(context, "file_type"));
    }

    protected static T GetValueFromContext<T>(IJobExecutionContext context, string key)
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