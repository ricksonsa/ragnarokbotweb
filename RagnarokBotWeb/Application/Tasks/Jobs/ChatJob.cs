using Quartz;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class ChatJob(
    ILogger<ChatJob> logger,
    IScumServerRepository scumServerRepository,
    IReaderPointerRepository readerPointerRepository,
    IPlayerRegisterRepository playerRegisterRepository,
    IPlayerRepository playerRespository,
    IReaderRepository readerRepository,
    IDiscordService discordService,
    IFtpService ftpService,
    DiscordChannelPublisher publisher
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var server = await GetServerAsync(context);

        var jobName = context.JobDetail.Key.Name;
        logger.LogInformation($"Executing job: {jobName} from serverId: {server.Id} at {DateTime.Now}");

        var fileType = GetFileTypeFromContext(context);

        var processor = new ScumFileProcessor(ftpService, server, fileType, readerPointerRepository, scumServerRepository, readerRepository);
        await foreach (var line in processor.UnreadFileLinesAsync())
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // await publisher.Publish(server, new ChannelPublishDto { Content = line }, ChannelTemplateValues.GameChat);

            var parsed = new ChatTextParser().Parse(line);
            if (parsed is null)
            {
                logger.LogInformation("line {} > Could not be parsed", line);
                continue;
            }

            var chatCommandHandler = new ExclamationCommandHandlerFactory(
                playerRespository,
                playerRegisterRepository,
                discordService).Create(parsed.Text);

            if (chatCommandHandler is not null)
            {
                await chatCommandHandler.ExecuteAsync(parsed);
            }
        }
    }
}