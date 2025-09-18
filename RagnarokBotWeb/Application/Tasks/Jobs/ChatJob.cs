using RagnarokBotWeb.Application.Handlers;
using RagnarokBotWeb.Application.LogParser;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Resolvers;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
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
    IUnitOfWork uow,
    IPlayerRegisterRepository playerRegisterRepository,
    IPlayerRepository playerRespository,
    IOrderService orderService,
    IDiscordService discordService,
    IFtpService ftpService,
    IBotService botService,
    DiscordChannelPublisher publisher,
    SteamAccountResolver steamAccountResolver
) : AbstractJob(scumServerRepository), IFtpJob
{
    public async Task Execute(long serverId, EFileType fileType)
    {
        try
        {
            var server = await GetServerAsync(serverId);

            logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

            var processor = new ScumFileProcessor(server, uow);

            await foreach (var line in processor.UnreadFileLinesAsync(fileType, ftpService))
            {
                try
                {
                    var parsed = new ChatTextParser().Parse(line);
                    if (parsed is null)
                    {
                        logger.LogError("line {Line} > Could not be parsed", line);
                        continue;
                    }

                    if (parsed.Text.Contains("!check-state") || parsed.Text.Contains("!status"))
                    {
                        var match = Regex.Match(parsed.Text, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                        if (match.Success)
                        {
                            var guid = new Guid(match.Value);
                            botService.BotPingUpdate(server.Id, guid, parsed.SteamId);
                        }
                        continue;
                    }

                    if (IsCompliant())
                    {
                        var chatCommandHandler = new ExclamationCommandHandlerFactory(
                               server,
                               botService,
                               scumServerRepository,
                               playerRespository,
                               playerRegisterRepository,
                               discordService,
                               orderService,
                               steamAccountResolver
                           ).Create(parsed.Text);

                        if (chatCommandHandler is not null)
                            await chatCommandHandler.ExecuteAsync(parsed);
                    }

                    if (!IsCommand(parsed)
                        && IsServerAllowed(server, parsed)
                        && !IsBotSteamId(botService, server, parsed)
                        && parsed.Post)
                    {
                        await publisher.Publish(server,
                         new ChannelPublishDto { Content = $"[{parsed.ChatType}] {parsed.PlayerName.Substring(0, parsed.PlayerName.LastIndexOf('('))}: {parsed.Text}" },
                         ChannelTemplateValues.GameChat);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Job} Exception", $"{GetType().Name}({serverId})");
                }
            }
        }
        catch (ServerUncompliantException) { }
        catch (FtpNotSetException) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Job} Exception", $"{GetType().Name}({serverId})");
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

    private static bool IsBotSteamId(IBotService botService, ScumServer server, ChatTextParseResult parsed)
    {
        return botService.GetBots(server.Id).Any(bot => bot.SteamId == parsed.SteamId);
    }
}