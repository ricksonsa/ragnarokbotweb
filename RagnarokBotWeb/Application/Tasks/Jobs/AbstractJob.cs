using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public abstract class AbstractJob
{
    private readonly IScumServerRepository _scumServerRepository;
    private ScumServer _scumServer;

    protected AbstractJob(IScumServerRepository scumServerRepository)
    {
        _scumServerRepository = scumServerRepository;
    }

    protected async Task<ScumServer> GetServerAsync(long serverId, bool ftpRequired = true, bool validateSubscription = false)
    {
        var server = await _scumServerRepository.FindByIdAsNoTrackingAsync(serverId);
        if (server is null) throw new Exception("Invalid server: server does not exist");
        _scumServer = server;
        if (validateSubscription && !server.Tenant.IsCompliant()) throw new ServerUncompliantException();
        if (server!.Ftp is null && ftpRequired) throw new FtpNotSetException();
        return server;
    }

    protected bool IsCompliant() => _scumServer.Tenant.IsCompliant();
}