using Ballware.Document.Metadata;
using DevExpress.Data.Filtering;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx.Internal;

public class FetchNameByIdFromLookup : ICustomFunctionOperatorBrowsable
{
    private IServiceProvider ServiceProvider { get; }
    
    public FetchNameByIdFromLookup(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public int MinOperandCount => 3;

    public int MaxOperandCount => 3;

    public string Description => "Fetch Name from Lookup by Id";

    public FunctionCategory Category => FunctionCategory.All;

    public string Name => "FetchNameByIdFromLookup";

    public object Evaluate(params object[] operands)
    {
        using var scope = ServiceProvider.CreateScope();
        
        var lookupProvider = scope.ServiceProvider.GetRequiredService<IDocumentLookupProvider>();

        var tenantId = (Guid)operands[0];
        var lookupId = Guid.Parse(operands[1] as string);
        var id = (Guid)operands[2];

        return lookupProvider.LookupColumnValueByTenantAndId(tenantId, lookupId, id.ToString(), "Name");
    }

    public bool IsValidOperandCount(int count)
    {
        return count == 3;
    }

    public bool IsValidOperandType(int operandIndex, int operandCount, Type type)
    {

        switch (operandIndex)
        {
            case 0:
                return type == typeof(Guid);
            case 1:
                return type == typeof(string);
            case 2:
                return type == typeof(Guid);
        }

        return false;
    }

    public Type ResultType(params Type[] operands)
    {
        return typeof(string);
    }
}