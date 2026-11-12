using Ballware.Document.Data.Ef.Configuration;
using Ballware.Document.Hosting.Configuration;
using Ballware.Document.Session.Configuration;

namespace Ballware.Document.Hosting;

public sealed record BallwareDocumentOptions(
    AuthorizationOptions Authorization,
    StorageOptions Storage,
    SessionOptions Session,
    ServiceClientOptions MetaClient,
    ServiceClientOptions StorageClient,
    ServiceClientOptions GenericClient,
    ServiceClientOptions MlClient);
