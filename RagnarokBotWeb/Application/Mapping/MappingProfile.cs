using AutoMapper;
using RagnarokBotWeb.Application.Discord.Dto;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Define mappings
            CreateMap<Ftp, FtpDto>();

            CreateMap<ScumServer, ScumServerDto>()
                .ForMember((dto) => dto.RestartTimes, opt => opt.MapFrom(server => server.GetRestartTimesList()))
                .ForMember((dto) => dto.IsCompliant, opt => opt.MapFrom(server => server.IsCompliant()))
                .ForMember((dto) => dto.Discord, opt => opt.MapFrom(server => server.Guild != null ?
                    new DiscordDto
                    {
                        Id = server.Guild.DiscordId,
                        DiscordLink = server.Guild.DiscordLink,
                        Token = server.Guild.Token,
                        Confirmed = server.Guild.Confirmed,
                        Name = server.Guild.DiscordName
                    } : null))
                .ReverseMap()
                    .ForMember(server => server.RestartTimes, opt => opt.MapFrom(dto => string.Join(";", dto.RestartTimes)));

            CreateMap<UpdateKillFeedDto, ScumServer>();
            CreateMap<UpdateServerSettingsDto, ScumServer>()
                .ForMember(server => server.RestartTimes, opt => opt.MapFrom(dto => string.Join(";", dto.RestartTimes)));

            CreateMap<Item, ItemDto>();
            CreateMap<Order, OrderDto>();
            CreateMap<User, AccountDto>().ReverseMap();

            CreateMap<CustomTask, CustomTaskDto>().ReverseMap();

            CreateMap<Uav, UavDto>().ReverseMap();

            CreateMap<Subscription, SubscriptionDto>().ReverseMap();
            CreateMap<Payment, PaymentDto>()
                .ForMember((dto) => dto.IsExpired, opt => opt.MapFrom(data => data.ExpireAt < DateTime.UtcNow))
                .ReverseMap();

            CreateMap<PackItem, PackItemDto>()
             .ForPath((dto) => dto.PackId, opt => opt.MapFrom(pack => pack.Pack.Id))
             .ForPath((dto) => dto.ItemName, opt => opt.MapFrom(pack => pack.Item.Name))
             .ForPath((dto) => dto.ItemId, opt => opt.MapFrom(pack => pack.Item.Id))
         .ReverseMap()
            .ForPath((pack) => pack.Pack.Id, opt => opt.MapFrom(dto => dto.PackId))
            .ForPath((pack) => pack.Item.Name, opt => opt.MapFrom(dto => dto.ItemName))
            .ForPath((pack) => pack.Item.Id, opt => opt.MapFrom(dto => dto.ItemId));

            CreateMap<Pack, PackDto>()
                .ReverseMap()
                .ForMember(dest => dest.PackItems, opt => opt.Ignore());

            CreateMap<Player, PlayerDto>()
        .ForMember(dto => dto.IsVip,
            opt => opt.MapFrom(player => player.IsVip()))
        .ForMember(dto => dto.IsBanned,
            opt => opt.MapFrom(player => player.IsBanned()))
        .ForMember(dto => dto.IsSilenced,
            opt => opt.MapFrom(player => player.IsSilenced()))
        .ForMember(dto => dto.LastLoggedIn,
            opt => opt.MapFrom(player =>
                player.ScumServer != null
                && !string.IsNullOrEmpty(player.ScumServer.TimeZoneId)
                && player.LastLoggedIn.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(
                        player.LastLoggedIn.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(player.ScumServer.TimeZoneId))
                    : (DateTime?)null))
        .ForMember(dto => dto.VipExpiresAt,
            opt => opt.MapFrom(player =>
                player.IsVip()
                    ? player.Vips
                        .Where(v => v.Indefinitely || !v.Processed)
                        .OrderByDescending(v => v.ExpirationDate)
                        .Select(v => (DateTime?)v.ExpirationDate)
                        .FirstOrDefault()
                    : null))
        .ForMember(dto => dto.BanExpiresAt,
            opt => opt.MapFrom(player =>
                player.IsBanned()
                    ? player.Bans
                        .Where(b => b.Indefinitely || !b.Processed)
                        .OrderByDescending(b => b.ExpirationDate)
                        .Select(b => (DateTime?)b.ExpirationDate)
                        .FirstOrDefault()
                    : null))
        .ForMember(dto => dto.SilenceExpiresAt,
            opt => opt.MapFrom(player =>
                player.IsSilenced()
                    ? player.Silences
                        .Where(s => s.Indefinitely || !s.Processed)
                        .OrderByDescending(s => s.ExpirationDate)
                        .Select(s => (DateTime?)s.ExpirationDate)
                        .FirstOrDefault()
                    : null));

            CreateMap<Button, ButtonDto>();
            CreateMap<Guild, GuildDto>()
                .ForMember((dto) => dto.ServerId, opt => opt.MapFrom(server => server.ScumServer.Id));

            CreateMap<Teleport, TeleportDto>().ReverseMap();

            CreateMap<WarzoneTeleport, WarzoneTeleportDto>()
                .ForPath((dto) => dto.WarzoneId, opt => opt.MapFrom(warzone => warzone.Warzone.Id))
                .ReverseMap()
                   .ForPath((warzone) => warzone.Warzone.Id, opt => opt.MapFrom(dto => dto.WarzoneId));

            CreateMap<WarzoneSpawn, WarzoneSpawnDto>()
               .ForPath((dto) => dto.WarzoneId, opt => opt.MapFrom(warzone => warzone.Warzone.Id))
               .ReverseMap()
                  .ForPath((warzone) => warzone.Warzone.Id, opt => opt.MapFrom(dto => dto.WarzoneId));

            CreateMap<WarzoneItem, WarzoneItemDto>()
                .ForPath((dto) => dto.WarzoneId, opt => opt.MapFrom(warzone => warzone.Warzone.Id))
                .ForPath((dto) => dto.ItemName, opt => opt.MapFrom(warzone => warzone.Item.Name))
                .ForPath((dto) => dto.ItemId, opt => opt.MapFrom(warzone => warzone.Item.Id))
            .ReverseMap()
               .ForPath((warzone) => warzone.Warzone.Id, opt => opt.MapFrom(dto => dto.WarzoneId))
               .ForPath((warzone) => warzone.Item.Name, opt => opt.MapFrom(dto => dto.ItemName))
               .ForPath((warzone) => warzone.Item.Id, opt => opt.MapFrom(dto => dto.ItemId));

            CreateMap<Warzone, WarzoneDto>()
                 .ForPath((dto) => dto.ScumServerId, opt => opt.MapFrom(warzone => warzone.ScumServer.Id))
                 .ForPath((dto) => dto.IsRunning, opt => opt.MapFrom(warzone => warzone.IsRunning))
            .ReverseMap()
               .ForPath((warzone) => warzone.ScumServer.Id, opt => opt.MapFrom(dto => dto.ScumServerId))
               .ForMember(dest => dest.WarzoneItems, opt => opt.Ignore())
               .ForMember(dest => dest.SpawnPoints, opt => opt.Ignore())
               .ForMember(dest => dest.Teleports, opt => opt.Ignore())
               .ForMember(dest => dest.ScumServer, opt => opt.Ignore());

            CreateMap<Taxi, TaxiDto>()
                   .ForPath((dto) => dto.ScumServerId, opt => opt.MapFrom(taxi => taxi.ScumServer.Id))
              .ReverseMap()
                 .ForPath((taxi) => taxi.ScumServer.Id, opt => opt.MapFrom(dto => dto.ScumServerId))
                 .ForMember(dest => dest.TaxiTeleports, opt => opt.Ignore())
                 .ForMember(dest => dest.ScumServer, opt => opt.Ignore());

            CreateMap<TaxiTeleport, TaxiTeleportDto>()
               .ForPath((dto) => dto.TaxiId, opt => opt.MapFrom(taxiTeleport => taxiTeleport.Taxi.Id))
               .ReverseMap()
                  .ForPath((taxiTeleport) => taxiTeleport.Taxi.Id, opt => opt.MapFrom(dto => dto.TaxiId));
        }
    }
}
