using System.Net.Mail;
using Ballware.Document.Metadata;
using DevExpress.XtraReports.UI;

namespace Ballware.Document.Engine.Dx.Internal;

class DocumentMailGenerator : IDocumentMailGenerator
{
    private const string TenantIdReportParameterName = "TenantId";
    
    public async Task<MailMessage> GenerateDocumentAsAttachmentMailAsync(Guid tenantId, byte[] documentBinary, IEnumerable<ReportParameter> parameters, string attachmentFileName, string from, string to,
        string? subject, string? body)
    {
        var report = new XtraReport();
        
        using (var inputStream = new MemoryStream(documentBinary))
        {
            report.LoadLayoutFromXml(inputStream);
        }

        foreach (var p in parameters)
        {
            if (report.Parameters[p.Parameter] != null)
            {
                if (p.Multi)
                {
                    report.Parameters[p.Parameter].Value = p.Value.Split(",");
                }
                else
                {
                    report.Parameters[p.Parameter].Value = p.Value;
                }
            }
        }
        
        if (report.Parameters[TenantIdReportParameterName] != null)
        {
            report.Parameters[TenantIdReportParameterName].Value = tenantId;
        }

        var mailMessage = new MailMessage(from, to, subject, body);
        
        using (var stream = new MemoryStream())
        {
            await report.ExportToPdfAsync(stream);

            stream.Position = 0;

            mailMessage.Attachments.Add(new Attachment(stream, attachmentFileName));
        }
        
        return mailMessage;
    }

    public async Task<MailMessage> GenerateDocumentAsInlineMailAsync(Guid tenantId, byte[] documentBinary, IEnumerable<ReportParameter> parameters, string from, string to,
        string? subject)
    {
        var report = new XtraReport();
        
        using (var inputStream = new MemoryStream(documentBinary))
        {
            report.LoadLayoutFromXml(inputStream);
        }

        foreach (var p in parameters)
        {
            if (report.Parameters[p.Parameter] != null)
            {
                if (p.Multi)
                {
                    report.Parameters[p.Parameter].Value = p.Value.Split(",");
                }
                else
                {
                    report.Parameters[p.Parameter].Value = p.Value;
                }
            }
        }
        
        if (report.Parameters[TenantIdReportParameterName] != null)
        {
            report.Parameters[TenantIdReportParameterName].Value = tenantId;
        }

        var mailMessage = await report.ExportToMailAsync(from, to, subject);
        
        return mailMessage;
    }
}