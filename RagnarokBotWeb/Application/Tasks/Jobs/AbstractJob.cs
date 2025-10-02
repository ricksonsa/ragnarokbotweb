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
        _scumServer = server ?? throw new Exception("Invalid server: server does not exist");
        if (!server.Tenant.Enabled) throw new TenantDisabledException();
        if ((server.Ftp is null || !server.Ftp.Enabled) && ftpRequired) throw new FtpNotSetException();
        if (validateSubscription && !server.Tenant.IsCompliant()) throw new ServerUncompliantException();
        return server;
    }

    protected bool IsCompliant() => _scumServer.Tenant.IsCompliant();
}