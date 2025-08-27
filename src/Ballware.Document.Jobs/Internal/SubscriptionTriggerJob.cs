using System.Net.Mail;
using Ballware.Document.Jobs.Configuration;
using Ballware.Document.Metadata;
using Quartz;

namespace Ballware.Document.Jobs.Internal;

public class SubscriptionTriggerJob : IJob
{
    public static readonly JobKey Key = new JobKey("trigger", "subscription");

    private INotificationMetadataProvider NotificationMetadataProvider { get; }
    private ISubscriptionMetadataProvider SubscriptionMetadataProvider { get; }
    private IDocumentMetadataProvider DocumentMetadataProvider { get; }
    private IDocumentMailGenerator DocumentMailGenerator { get; }
    private MailOptions MailOptions { get; }
    
    public SubscriptionTriggerJob(INotificationMetadataProvider notificationMetadataProvider, 
        ISubscriptionMetadataProvider subscriptionMetadataProvider,
        IDocumentMetadataProvider documentMetadataProvider,
        IDocumentMailGenerator documentMailGenerator,
        MailOptions mailOptions)
    {
        NotificationMetadataProvider = notificationMetadataProvider;
        SubscriptionMetadataProvider = subscriptionMetadataProvider;
        DocumentMetadataProvider = documentMetadataProvider;
        DocumentMailGenerator = documentMailGenerator;
        MailOptions = mailOptions;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        var tenantId = context.MergedJobDataMap.GetGuidValue("tenantId");
        var subscriptionId = context.MergedJobDataMap.GetGuidValue("subscriptionId");
        
        var subscription = await SubscriptionMetadataProvider.SubscriptionForTenantAndIdAsync(tenantId, subscriptionId);
        
        if (subscription == null)
        {
            throw new ArgumentException($"Subscription {subscriptionId} for tenant {tenantId} unknown");
        }
        
        var notification = await NotificationMetadataProvider.NotificationForTenantAndIdAsync(tenantId, subscription.NotificationId);

        if (notification == null)
        {
            throw new ArgumentException($"Notification {subscription.NotificationId} for tenant {tenantId} unknown");
        }

        if (notification.DocumentId == null)
        {
            throw new ArgumentException($"Notification {subscription.NotificationId} for tenant {tenantId} has no document defined");
        }
        
        var documentBinary = await DocumentMetadataProvider.DocumentBinaryForTenantAndIdAsync(tenantId, notification.DocumentId.Value);
        
        if (documentBinary == null)
        {
            throw new ArgumentException($"Document {notification.DocumentId} for tenant {tenantId} unknown");
        }

        var documentParameters = new List<ReportParameter>();

        if (!string.IsNullOrEmpty(notification.DocumentParams))
        {
            documentParameters = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReportParameter>>(notification.DocumentParams);
        }
        
        if (subscription.Mail != null)
        {
            try
            {
                if (MailOptions.AllowedTenants.Any() && !MailOptions.AllowedTenants.Contains(tenantId))
                {
                    throw new ArgumentException($"Tenant {tenantId} is not allowed to send mails");
                }
                
                MailMessage? mailMessage = null;

                if (subscription.Attachment)
                {
                    mailMessage = await DocumentMailGenerator.GenerateDocumentAsAttachmentMailAsync(tenantId,
                        documentBinary, documentParameters, subscription.AttachmentFileName, MailOptions.FromMail,
                        subscription.Mail, notification.Name, subscription.Body);
                }
                else
                {
                    mailMessage = await DocumentMailGenerator.GenerateDocumentAsInlineMailAsync(tenantId,
                        documentBinary, documentParameters, MailOptions.FromMail, subscription.Mail, notification.Name);
                }

                using var smtpClient = new SmtpClient();

                smtpClient.Host = MailOptions.SmtpHost;
                smtpClient.Port = MailOptions.SmtpPort;
                smtpClient.EnableSsl = MailOptions.SmtpUseSsl;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.Timeout = MailOptions.SmtpTimeout;
                smtpClient.Credentials =
                    new System.Net.NetworkCredential(MailOptions.SmtpUser, MailOptions.SmtpPassword);

                mailMessage.From = new MailAddress(MailOptions.FromMail, MailOptions.FromName);

                smtpClient.Send(mailMessage);
                
                await SubscriptionMetadataProvider.SetSendResultForSubscriptionAsync(tenantId, subscriptionId,
                    "OK");
            }
            catch (Exception ex)
            {
                await SubscriptionMetadataProvider.SetSendResultForSubscriptionAsync(tenantId, subscriptionId,
                    ex.Message);
            }
        }
    }
}