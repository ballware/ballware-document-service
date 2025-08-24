using DevExpress.XtraReports;
using DevExpress.XtraReports.Parameters;
using DevExpress.XtraReports.UI;
using DevExpress.XtraReports.Web.Extensions;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ballware.Shared.Authorization;
using Ballware.Document.Metadata;
using DevExpress.DataAccess.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx.Internal;

public class DocumentStorage : ReportStorageWebExtension
{
    private static readonly string GuidCheckRegex = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
    private static readonly string PrintUrlCheckRegex = @"docId=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:&id=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}))+";
    private static readonly string PrintUrlFindIdsRegex = @"id=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})";

    private readonly IServiceProvider _serviceProvider;

    public DocumentStorage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private Guid? GetCurrentUser()
    {
        var contextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var principalUtils = _serviceProvider.GetRequiredService<IPrincipalUtils>();

        var user = contextAccessor.HttpContext?.User;

        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return null;
        }
        
        var userId = principalUtils.GetUserId(user);
        
        return userId;
    }

    private Guid? GetCurrentTenant()
    {
        var contextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var principalUtils = _serviceProvider.GetRequiredService<IPrincipalUtils>();

        var user = contextAccessor.HttpContext?.User;
        
        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return null;
        }
        
        var tenantId = principalUtils.GetUserTenandId(user);

        return tenantId;
    }

    public override bool IsValidUrl(string url)
    {
        return !string.IsNullOrEmpty(url);
    }

    public override bool CanSetData(string url)
    {

        return !string.IsNullOrEmpty(url);
    }

    public override byte[] GetData(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return [];
        }
        
        using var scope = _serviceProvider.CreateScope();
        
        var metaProvider = scope.ServiceProvider.GetRequiredService<IDocumentMetadataProvider>();

        var tenantId = GetCurrentTenant();

        var matchPrintUrl = Regex.Match(url, PrintUrlCheckRegex);
        var matchGuid = Regex.Match(url, GuidCheckRegex);

        if (matchPrintUrl.Success && tenantId != null)
        {
            var docId = matchPrintUrl.Groups[1].Value;
            var ids = new List<Guid>();

            var matchFindIds = Regex.Match(url, PrintUrlFindIdsRegex);

            while (matchFindIds.Success)
            {
                ids.Add(Guid.Parse(matchFindIds.Groups[1].Value));

                matchFindIds = matchFindIds.NextMatch();
            }

            return LoadPrintDocumentBinary(metaProvider, tenantId.Value, Guid.Parse(docId), ids);
        }
        
        if (matchGuid.Success && tenantId != null)
        {
            return LoadSingleDocumentBinary(metaProvider, tenantId.Value, Guid.Parse(matchGuid.Groups[0].Value));
        }
         
        if ("new".Equals(url) && tenantId != null)
        {
            var datasourceProvider = scope.ServiceProvider.GetRequiredService<IDocumentDatasourceProvider>();

            return LoadNewDocumentBinary(datasourceProvider, tenantId.Value);
        }

        return [];
    }

    public override void SetData(XtraReport report, string url)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var metaProvider = scope.ServiceProvider.GetRequiredService<IDocumentMetadataProvider>();
        var metaDatasourceProvider = scope.ServiceProvider.GetRequiredService<IMetaDatasourceProvider>();

        var tenantId = GetCurrentTenant();
        var currentUser = GetCurrentUser();

        var matchGuid = Regex.Match(url, GuidCheckRegex);

        if (tenantId != null && currentUser != null && matchGuid.Success)
        {
            var entity = report.DataMember?.ToLowerInvariant();
            var displayName = report.DisplayName;
            var reportParameter = ExtractParameterDefinition(report, metaDatasourceProvider, tenantId.Value);
            
            byte[] reportBinary;
            
            using (MemoryStream ms = new MemoryStream())
            {
                report.SaveLayoutToXml(ms);
                reportBinary = ms.ToArray();
            }
            
            metaProvider.UpdateDocumentMetadataAndBinaryForTenantAndIdAsync(tenantId.Value, currentUser.Value, Guid.Parse(matchGuid.Groups[0].Value), entity, displayName,reportBinary, reportParameter).GetAwaiter().GetResult();
        }
        else if ("new".Equals(url))
        {
            SetNewData(report, string.Empty);
        }
    }

    public override string SetNewData(XtraReport report, string defaultUrl)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var metaProvider = scope.ServiceProvider.GetRequiredService<IDocumentMetadataProvider>();
        var metaDatasourceProvider = scope.ServiceProvider.GetRequiredService<IMetaDatasourceProvider>();

        var tenantId = GetCurrentTenant();
        var currentUser = GetCurrentUser();

        if (tenantId != null && currentUser != null)
        {
            var entity = report.DataMember?.ToLowerInvariant();
            var displayName = report.DisplayName;
            var reportParameter = ExtractParameterDefinition(report, metaDatasourceProvider, tenantId.Value);
            
            byte[] reportBinary;
            
            using (MemoryStream ms = new MemoryStream())
            {
                report.SaveLayoutToXml(ms);
                reportBinary = ms.ToArray();
            }

            var id = metaProvider.AddDocumentMetadataForTenantAsync(tenantId.Value, currentUser.Value, entity, displayName, reportBinary, reportParameter).GetAwaiter().GetResult();

            return id.ToString();
        }

        throw new ArgumentException("User or tenant missing");
    }

    public override Dictionary<string, string> GetUrls()
    {
        using var scope = _serviceProvider.CreateScope();
        
        var metaProvider = scope.ServiceProvider.GetRequiredService<IDocumentMetadataProvider>();

        var tenantId = GetCurrentTenant();

        if (tenantId != null)
        {
            var documents = metaProvider.DocumentsForTenantAsync(tenantId.Value).GetAwaiter().GetResult();

            return documents.ToDictionary(d => $"{d.Id}", d => d.Name ?? string.Empty);
        }

        throw new ArgumentException("Tenant missing");
    }

    private static string ExtractParameterDefinition(XtraReport report, IMetaDatasourceProvider metaDatasourceProvider, Guid tenant)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartArray();

        foreach (var parameter in report.Parameters)
        {
            if (parameter.Visible)
            {
                writer.WriteStartObject();

                writer.WriteString("name", parameter.Name);
                writer.WriteString("description", parameter.Description);
                writer.WriteString("type", parameter.Type.Name);
                writer.WriteBoolean("multi", parameter.MultiValue);
                    
                if (parameter.ValueSourceSettings is DynamicListLookUpSettings settings 
                    && settings.DataSource is SqlDataSource sqlDataSource
                    && !string.IsNullOrEmpty(sqlDataSource.Name)
                    && !string.IsNullOrEmpty(settings.DataMember))
                {
                    try
                    {
                        var lookupMetadata =
                            metaDatasourceProvider.LookupMetadataForTenantDatasourceAndIdentifier(tenant,
                                sqlDataSource.Name, settings.DataMember);

                        foreach (var entry in lookupMetadata)
                        {
                            writer.WriteString(entry.Key, entry.Value.ToString());
                        }
                        
                        writer.WriteString("lookupDisplayMember", settings.DisplayMember);
                        writer.WriteString("lookupDataMember", settings.ValueMember);
                    } 
                    catch (Exception ex)
                    {
                        // Handle exception if lookup metadata cannot be found
                        writer.WriteString("error", ex.Message);
                    }
                }

                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static byte[] LoadPrintDocumentBinary(IDocumentMetadataProvider metaProvider, Guid tenantId, Guid documentId, IEnumerable<Guid> ids)
    {
       var reportBinary = metaProvider.DocumentBinaryForTenantAndIdAsync(tenantId, documentId).GetAwaiter().GetResult();
        
        XtraReport report = new XtraReport();

        using (var inputStream = new MemoryStream(reportBinary))
        {
            report.LoadLayoutFromXml(inputStream);
        }

        if (report.Parameters["Ids"] != null)
        {
            report.Parameters["Ids"].Value = ids.ToArray();
        }

        if (report.Parameters["TenantId"] != null)
        {
            report.Parameters["TenantId"].Value = tenantId;
        }

        report.RequestParameters = false;

        using (var outputStream = new MemoryStream())
        {
            report.SaveLayoutToXml(outputStream);

            return outputStream.ToArray();
        }
    }

    private static byte[] LoadSingleDocumentBinary(IDocumentMetadataProvider metaProvider, Guid tenantId, Guid documentId)
    {
        var reportBinary = metaProvider.DocumentBinaryForTenantAndIdAsync(tenantId, documentId).GetAwaiter().GetResult();

        XtraReport report = new XtraReport();

        using (var inputStream = new MemoryStream(reportBinary))
        {
            report.LoadLayoutFromXml(inputStream);
        }

        using var outputStream = new MemoryStream();

        report.SaveLayoutToXml(outputStream);

        return outputStream.ToArray();
    }

    private static byte[] LoadNewDocumentBinary(IDocumentDatasourceProvider datasourceProvider, Guid tenantId)
    {
        XtraReport report = new XtraReport();

        report.Parameters.Add(new Parameter()
            { Name = "Ids", Type = typeof(Guid), MultiValue = true, AllowNull = true, Visible = false });
        report.Parameters.Add(new Parameter()
            { Name = "TenantId", Type = typeof(Guid), Visible = false, Value = tenantId });
        
        var datasources = datasourceProvider.CreateDatasourcesForTenant(tenantId);

        DataSourceManager.AddDataSources(report, datasources.Select(d => (IComponent)d.Value).ToArray());

        using var outputStream = new MemoryStream();
        report.SaveLayoutToXml(outputStream);

        return outputStream.ToArray();
    }
}
