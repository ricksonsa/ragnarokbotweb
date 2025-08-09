using Quartz;
using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Text.RegularExpressions;

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

            var processor = new ScumFileProcessor(server);
            await foreach (var line in processor.UnreadFileLinesAsync(fileType, readerPointerRepository, ftpService))
            {
                var parsed = new ChatTextParser().Parse(line);
                if (parsed is null)
                {
                    logger.LogError("line {Line} > Could not be parsed", line);
                    continue;
                }

                if (IsCompliant())
                {
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
                        await chatCommandHandler.ExecuteAsync(parsed);
                }

                if (parsed.Text.Contains("!check-state"))
                {

                    var match = Regex.Match(parsed.Text, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                    if (match.Success)
                    {
                        var guid = new Guid(match.Value);
                        var bot = cacheService.GetConnectedBots(server.Id)
                          .Where(bot => bot.Key == guid)
                          .Select(b => b.Value)
                          .FirstOrDefault();

                        if (bot is not null)
                        {
                            bot.SteamId = parsed.SteamId;
                            bot.LastPinged = DateTime.UtcNow;
                            parsed.Post = false;
                            cacheService.GetConnectedBots(server.Id)[guid] = bot;
                        }
                    }
                    else
                    {
                        logger.LogError("Could not parse the string [{}] to Guid", parsed.Text);
                    }
                }

                if (!IsCommand(parsed)
                    && IsServerAllowed(server, parsed)
                    && !IsBotSteamId(cacheService, server, parsed)
                    && parsed.Post)
                {
                    await publisher.Publish(server,
                     new ChannelPublishDto { Content = $"[{parsed.ChatType}] {parsed.PlayerName.Substring(0, parsed.PlayerName.LastIndexOf("("))}: {parsed.Text}" },
                     ChannelTemplateValues.GameChat);
                }

            }
        }
        catch (Exception ex)
        {
            logger.LogError("{Job} Exception -> {Ex}", context.JobDetail.Key.Name, ex.Message);
            throw;
        }
    }

    private static bool IsCommand(ChatTextParseResult parsed)
    {
        return parsed.Text.StartsWith("#") || parsed.Text.StartsWith("!");
    }

    private static bool IsServerAllowed(ScumServer server, ChatTextParseResult parsed)
    {
        return parsed.ChatType == "Global" && server.SendGlobalChatToDiscord
            || parsed.ChatType == "Local" && server.SendLocalChatToDiscord;
    }

    private static bool IsBotSteamId(ICacheService cacheService, ScumServer server, ChatTextParseResult parsed)
    {
        return cacheService.GetConnectedBots(server.Id).Any(b => b.Value.SteamId == parsed.SteamId);
    }
}