using RagnarokBotWeb.Application.Tasks.Jobs;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.BackgroundServices;

public class ChatJobRunnerService(IServiceProvider serviceProvider, ILogger<ChatJobRunnerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ChatJobRunnerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                var serverRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
                var servers = await serverRepository.FindActive();

                foreach (var server in servers)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    _ = Task.Run(async () =>
                    {
                        using var jobScope = serviceProvider.CreateScope();
                        var job = jobScope.ServiceProvider.GetRequiredService<ChatJob>();

                        try
                        {
                            await job.Execute(server.Id, Domain.Enums.EFileType.Chat);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error while executing ChatJob for server {ServerId}", server.Id);
                        }
                    }, stoppingToken);
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while running jobs");
            }

            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }
}