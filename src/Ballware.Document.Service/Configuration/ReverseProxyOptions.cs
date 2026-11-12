using System.ComponentModel.DataAnnotations;

namespace Ballware.Document.Service.Configuration;

public class ReverseProxyOptions
{
    // Der vorgeschaltete Reverse-Proxy (https://api.ballware.de) leitet ausschließlich die Pfade
    // /signon, /designer, /viewer und /document an den Document-Service weiter. Die Client-
    // Ressourcen des Blazor-Anteils (_framework, _content, wwwroot-Dateien) werden ohne Prefix
    // angefordert und führen dann zu 401-Antworten des Gateways. Über das Prefix werden sie auf
    // denselben Pfad gelegt wie die Api-Endpunkte. Ein leeres Prefix schaltet das Verhalten ab.
    [RegularExpression(@"^$|^(/[A-Za-z0-9._\-]+)+$", ErrorMessage = "PathPrefix must be empty or an absolute path")]
    public string PathPrefix { get; set; } = "/document";

    public string BaseHref =>
        string.IsNullOrEmpty(PathPrefix) ? "/" : PathPrefix.TrimEnd('/') + "/";
}
