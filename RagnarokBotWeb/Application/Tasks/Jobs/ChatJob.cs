using Quartz;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class ChatJob(
    ILogger<ChatJob> logger,
    IScumServerRepository scumServerRepository,
    IFtpService ftpService,
    IServiceProvider serviceProvider,
    DiscordChannelPublisher publisher
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var server = await GetServerAsync(context);

        var jobName = context.JobDetail.Key.Name;
        logger.LogInformation($"Executing job: {jobName} from serverId: {server.Id} at {DateTime.Now}");

        var fileType = GetFileTypeFromContext(context);

        var processor = new ScumFileProcessor(serviceProvider, ftpService, server, fileType);
        var lines = await processor.ProcessUnreadFileLines();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            await publisher.Publish(server, new ChannelPublishDto { Content = line }, ChannelTemplateValues.GameChat);
        }
    }
}