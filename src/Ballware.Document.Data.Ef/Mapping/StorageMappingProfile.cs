using Mapster;

namespace Ballware.Document.Data.Ef.Mapping;

class StorageMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Public.Document, Persistables.Document>()
            .Ignore(dest => dest.Id!)
            .Map(dest => dest.Uuid, src => src.Id);

        config.NewConfig<Persistables.Document, Public.Document>()
            .Map(dest => dest.Id, src => src.Uuid);

        config.NewConfig<Public.Notification, Persistables.Notification>()
            .Ignore(dest => dest.Id!)
            .Map(dest => dest.Uuid, src => src.Id);

        config.NewConfig<Persistables.Notification, Public.Notification>()
            .Map(dest => dest.Id, src => src.Uuid);

        config.NewConfig<Public.Subscription, Persistables.Subscription>()
            .Ignore(dest => dest.Id!)
            .Map(dest => dest.Uuid, src => src.Id);

        config.NewConfig<Persistables.Subscription, Public.Subscription>()
            .Map(dest => dest.Id, src => src.Uuid);
    }
}
