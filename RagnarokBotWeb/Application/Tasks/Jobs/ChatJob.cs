using Quartz;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Diagnostics;

namespace RagnarokBotWeb.Application.Tasks.Jobs;

public class ChatJob(
    ILogger<ChatJob> logger,
    IScumServerRepository scumServerRepository,
    IReaderPointerRepository readerPointerRepository,
    IPlayerRegisterRepository playerRegisterRepository,
    IPlayerRepository playerRespository,
    IOrderService orderService,
    IDiscordService discordService,
    IFtpService ftpService,
    DiscordChannelPublisher publisher
) : AbstractJob(scumServerRepository), IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var server = await GetServerAsync(context);

            var jobName = context.JobDetail.Key.Name;
            logger.LogInformation($"Executing job: {jobName} from serverId: {server.Id} at {DateTime.Now}");

            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(ftpService, server, fileType, readerPointerRepository);
            await foreach (var line in processor.UnreadFileLinesAsync())
            {
                var parsed = new ChatTextParser().Parse(line);
                if (parsed is null)
                {
                    logger.LogInformation("line {} > Could not be parsed", line);
                    continue;
                }

                if (parsed.ChatType == "Global" || parsed.ChatType == "Local" && (!parsed.Text.StartsWith("#") || !parsed.Text.StartsWith("!")))
                {
                    await publisher.Publish(server,
                       new ChannelPublishDto { Content = $"[{parsed.ChatType}]{parsed.PlayerName}: {parsed.Text}" },
                       ChannelTemplateValues.GameChat);
                }

                var chatCommandHandler = new ExclamationCommandHandlerFactory(
                    playerRespository,
                    playerRegisterRepository,
                    discordService,
                    orderService).Create(parsed.Text);

                if (chatCommandHandler is not null)
                {
                    await chatCommandHandler.ExecuteAsync(parsed);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }
    }
}