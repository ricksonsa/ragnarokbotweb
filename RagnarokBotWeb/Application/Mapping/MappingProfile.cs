using AutoMapper;
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
                .ReverseMap()
                    .ForMember(server => server.RestartTimes, opt => opt.MapFrom(dto => string.Join(";", dto.RestartTimes)));

            CreateMap<Item, ItemDto>();

            CreateMap<PackItem, ItemToPackDto>()
                .ForPath((dto) => dto.ItemName, opt => opt.MapFrom(packItem => packItem.Item.Name))
                .ForPath((dto) => dto.ItemId, opt => opt.MapFrom(packItem => packItem.Item.Id))
                .ForPath((dto) => dto.Amount, opt => opt.MapFrom(packItem => packItem.Amount))
                .ForPath((dto) => dto.PackId, opt => opt.MapFrom(packItem => packItem.Pack.Id))
                .ForPath((dto) => dto.ItemCode, opt => opt.MapFrom(packItem => packItem.Item.Code))
                .ReverseMap()
                   .ForPath((packItem) => packItem.Item.Name, opt => opt.MapFrom(dto => dto.ItemName))
                   .ForPath((packItem) => packItem.Item.Id, opt => opt.MapFrom(dto => dto.ItemId))
                   .ForPath((packItem) => packItem.Amount, opt => opt.MapFrom(dto => dto.Amount))
                   .ForPath((packItem) => packItem.Pack.Id, opt => opt.MapFrom(dto => dto.PackId))
                   .ForPath((packItem) => packItem.Item.Code, opt => opt.MapFrom(dto => dto.ItemCode));

            CreateMap<Pack, PackDto>()
                .ForMember(dto => dto.Items, opt => opt.MapFrom(source => source.PackItems))
                .ReverseMap()
                    .ForMember(source => source.PackItems, opt => opt.MapFrom(dto => dto.Items));

            CreateMap<Player, PlayerDto>();
        }
    }
}
