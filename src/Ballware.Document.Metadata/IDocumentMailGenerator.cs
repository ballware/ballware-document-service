using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;

namespace Ballware.Document.Metadata;

public interface IDocumentMailGenerator
{
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Parameters are necessary for document generation")]
    Task<MailMessage> GenerateDocumentAsAttachmentMailAsync(
        Guid tenantId,
        byte[] documentBinary, 
        IEnumerable<ReportParameter> parameters,
        string attachmentFileName,
        string from,
        string to,
        string? subject,
        string? body);
    
    Task<MailMessage> GenerateDocumentAsInlineMailAsync(
        Guid tenantId,
        byte[] documentBinary,
        IEnumerable<ReportParameter> parameters,
        string from,
        string to,
        string? subject);
}