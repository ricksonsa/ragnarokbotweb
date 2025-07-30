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
    ICacheService cacheService,
    IBotRepository botRepository,
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
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", context.JobDetail.Key.Name, DateTimeOffset.Now);

            var fileType = GetFileTypeFromContext(context);

            var processor = new ScumFileProcessor(ftpService, server, fileType, readerPointerRepository);
            await foreach (var line in processor.UnreadFileLinesAsync())
            {
                var parsed = new ChatTextParser().Parse(line);
                if (parsed is null)
                {
                    logger.LogError("line {Line} > Could not be parsed", line);
                    continue;
                }

                var chatCommandHandler = new ExclamationCommandHandlerFactory(
                    server,
                    cacheService,
                    scumServerRepository,
                    playerRespository,
                    playerRegisterRepository,
                    discordService,
                    orderService
                    ).Create(parsed.Text);

                if (chatCommandHandler is not null)
                {
                    await chatCommandHandler.ExecuteAsync(parsed);
                }

                if (parsed.Text.Contains("#check-state"))
                {
                    var guid = parsed.Text.Substring(parsed.Text.LastIndexOf("-"));
                    var bot = await botRepository.FindOneAsync(bot => bot.Guid.ToString() == guid);
                    if (bot is not null)
                    {
                        bot.SteamId = parsed.SteamId;
                        bot.LastPinged = DateTime.UtcNow;
                        parsed.Post = false;
                        await botRepository.CreateOrUpdateAsync(bot);
                        await botRepository.SaveAsync();
                    }
                }

                if (!(parsed.Text.StartsWith("#") || parsed.Text.StartsWith("!"))
                    && (parsed.ChatType == "Global" && server.SendGlobalChatToDiscord
                    || parsed.ChatType == "Local" && server.SendLocalChatToDiscord) && parsed.Post)
                {

                    await publisher.Publish(server,
                     new ChannelPublishDto { Content = $"[{parsed.ChatType}] {parsed.PlayerName.Substring(0, parsed.PlayerName.LastIndexOf("("))}: {parsed.Text}" },
                     ChannelTemplateValues.GameChat);
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