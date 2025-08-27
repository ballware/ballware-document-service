using AutoMapper;
using Ballware.Document.Data.Repository;
using Ballware.Document.Metadata;
using Ballware.Meta.Service.Client;

namespace Ballware.Document.Service.Adapter;

public class SubscriptionMetadataProvider : ISubscriptionMetadataProvider
{
    private IMapper Mapper { get; }
    private ISubscriptionMetaRepository Repository { get; }
    private MetaServiceClient MetaClient { get; }
    
    public SubscriptionMetadataProvider(IMapper mapper, ISubscriptionMetaRepository repository, MetaServiceClient metaClient)
    {
        Mapper = mapper;
        Repository = repository;   
        MetaClient = metaClient;  
    }

    public async Task<Data.Public.Subscription?> SubscriptionForTenantAndIdAsync(Guid tenantId, Guid subscriptionId)
    {
        return await Repository.MetadataByTenantAndIdAsync(tenantId, subscriptionId);
    }

    public async Task SetSendResultForSubscriptionAsync(Guid tenantId, Guid subscriptionId, string? sendResult)
    {
        await Repository.SetLastErrorAsync(tenantId, subscriptionId, sendResult ?? string.Empty);
    }

    public async Task<IEnumerable<TenantListEntry>> GetReportAllowedTenantsAsync()
    {
        return Mapper.Map<IEnumerable<TenantListEntry>>(await MetaClient.TenantReportAllowedTenantsAsync());
    }

    public async Task<IEnumerable<Data.Public.Subscription>> GetActiveSubscriptionsForTenantFrequencyAsync(Guid tenantId, int frequency)
    {
        return await Repository.GetActiveSubscriptionsByTenantAndFrequencyAsync(tenantId, frequency);
    }
}