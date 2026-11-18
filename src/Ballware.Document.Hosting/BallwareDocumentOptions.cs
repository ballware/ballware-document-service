using Ballware.Document.Data.Ef.Configuration;
using Ballware.Document.Hosting.Configuration;
using Ballware.Shared.Web.Session.Configuration;

namespace Ballware.Document.Hosting;

public sealed record BallwareDocumentOptions(
    AuthorizationOptions Authorization,
    StorageOptions Storage,
    WebSessionOptions Session,
    ServiceClientOptions MetaClient,
    ServiceClientOptions StorageClient,
    ServiceClientOptions GenericClient,
    ServiceClientOptions MlClient);
