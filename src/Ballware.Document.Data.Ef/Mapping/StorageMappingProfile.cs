using AutoMapper;

namespace Ballware.Document.Data.Ef.Mapping;

class StorageMappingProfile : Profile
{
    public StorageMappingProfile()
    {
        CreateMap<Public.Document, Persistables.Document>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id));

        CreateMap<Persistables.Document, Public.Document>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Uuid));
     
        CreateMap<Public.Notification, Persistables.Notification>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id));

        CreateMap<Persistables.Notification, Public.Notification>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Uuid));

        CreateMap<Public.Subscription, Persistables.Subscription>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Uuid, opt => opt.MapFrom(src => src.Id));

        CreateMap<Persistables.Subscription, Public.Subscription>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Uuid));
    }
}