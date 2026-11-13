using Ballware.Document.Data.Ef.Mapping;
using Mapster;

namespace Ballware.Document.Data.Ef;

public static class MapperConfigurationExtensions
{
    public static TypeAdapterConfig AddBallwareDocumentStorageMappings(
        this TypeAdapterConfig configuration)
    {
        new StorageMappingProfile().Register(configuration);

        return configuration;
    }
}
