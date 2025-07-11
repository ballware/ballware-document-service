namespace Ballware.Document.Metadata;

public interface ISubscriptionMetadataProvider
{
    Task<Subscription?> SubscriptionForTenantAndIdAsync(Guid tenantId, Guid subscriptionId);
    Task SetSendResultForSubscriptionAsync(Guid tenantId, Guid subscriptionId, string? sendResult);

    Task<IEnumerable<TenantListEntry>> GetReportAllowedTenantsAsync();
    Task<IEnumerable<Subscription>> GetActiveSubscriptionsForTenantFrequencyAsync(Guid tenantId, int frequency);
}