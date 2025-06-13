using Ballware.Document.Metadata;
using DevExpress.Data.Filtering;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx.Internal;

public class FetchTextByValueFromPickvalue : ICustomFunctionOperatorBrowsable
{
    private IServiceProvider ServiceProvider { get; }

    public FetchTextByValueFromPickvalue(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public int MinOperandCount => 4;

    public int MaxOperandCount => 4;

    public string Description => "Fetch Text from Pickvalue by Value";

    public FunctionCategory Category => FunctionCategory.All;

    public string Name => "FetchTextByValueFromPickvalue";

    public object Evaluate(params object[] operands)
    {
        var pickvalueProvider = ServiceProvider.GetRequiredService<IDocumentPickvalueProvider>();

        var tenantId = (Guid)operands[0];
        var entity = operands[1] as string;
        var field = operands[2] as string;
        var value = (int)operands[3];

        return pickvalueProvider.PickvalueNameForTenantAndEntityAndFieldByValue(tenantId, entity, field, value);
    }

    public bool IsValidOperandCount(int count)
    {
        return count == 4;
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
                return type == typeof(string);
            case 3:
                return type == typeof(int);
        }

        return false;
    }

    public Type ResultType(params Type[] operands)
    {
        return typeof(string);
    }
}