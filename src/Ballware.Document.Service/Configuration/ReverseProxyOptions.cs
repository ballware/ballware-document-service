using System.ComponentModel.DataAnnotations;

namespace Ballware.Document.Service.Configuration;

public class ReverseProxyOptions
{
    // Der vorgeschaltete Reverse-Proxy (https://api.ballware.de) leitet für den Blazor-Anteil nur
    // Prefix-URLs weiter, die Client-Ressourcen (/assets/document/...) liegen also über einen
    // gemeinsamen Prefix. Die Anwendung mappt ihre Ressourcen ohne Prefix und beantwortet sie über
    // das PathBase, unabhängig davon, ob das Gateway den Prefix mitliefert oder vorher abbaut.
    // Ohne PathBase erzeugten die Komponenten absolute Ressourcen-URLs ohne Prefix und das Gateway
    // antwortete mit 401. Ein leeres Prefix schaltet das Verhalten ab.
    [RegularExpression(@"^$|^(/[A-Za-z0-9._\-]+)+$", ErrorMessage = "PathPrefix must be empty or an absolute path")]
    public string PathPrefix { get; set; } = "/assets/document";

    public string BaseHref =>
        string.IsNullOrEmpty(PathPrefix) ? "/" : PathPrefix.TrimEnd('/') + "/";
}
