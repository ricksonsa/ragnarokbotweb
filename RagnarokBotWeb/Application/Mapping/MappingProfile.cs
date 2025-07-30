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

            CreateMap<Item, ItemDto>();
            CreateMap<Order, OrderDto>();

            CreateMap<PackItem, PackItemDto>()
             .ForPath((dto) => dto.PackId, opt => opt.MapFrom(pack => pack.Pack.Id))
             .ForPath((dto) => dto.ItemName, opt => opt.MapFrom(pack => pack.Item.Name))
             .ForPath((dto) => dto.ItemId, opt => opt.MapFrom(pack => pack.Item.Id))
         .ReverseMap()
            .ForPath((pack) => pack.Pack.Id, opt => opt.MapFrom(dto => dto.PackId))
            .ForPath((pack) => pack.Item.Name, opt => opt.MapFrom(dto => dto.ItemName))
            .ForPath((pack) => pack.Item.Id, opt => opt.MapFrom(dto => dto.ItemId));

            CreateMap<Pack, PackDto>().ReverseMap();

            CreateMap<Player, PlayerDto>()
                .ForMember((dto) => dto.IsVip, opt => opt.MapFrom(player => player.IsVip()))
                .ForMember((dto) => dto.IsBanned, opt => opt.MapFrom(player => player.IsBanned()))
                .ForMember((dto) => dto.IsSilenced, opt => opt.MapFrom(player => player.IsSilenced()))
                .ForMember((dto) => dto.VipExpiresAt,
                    opt => opt.MapFrom(player => player.IsVip() ? player.Vips.OrderByDescending(v => v.ExpirationDate).First(x => x.Indefinitely || !x.Processed).ExpirationDate : null))
                .ForMember((dto) => dto.BanExpiresAt,
                    opt => opt.MapFrom(player => player.IsBanned() ? player.Bans.OrderByDescending(v => v.ExpirationDate).First(x => x.Indefinitely || !x.Processed).ExpirationDate : null))
                .ForMember((dto) => dto.SilenceExpiresAt,
                    opt => opt.MapFrom(player => player.IsSilenced() ? player.Silences.OrderByDescending(v => v.ExpirationDate).First(x => x.Indefinitely || !x.Processed).ExpirationDate : null));

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
            .ReverseMap()
               .ForPath((warzone) => warzone.ScumServer.Id, opt => opt.MapFrom(dto => dto.ScumServerId));
        }
    }
}
