using Ballware.Document.Metadata;
using DevExpress.Data.Filtering;
using Microsoft.Extensions.DependencyInjection;

namespace Ballware.Document.Engine.Dx.Internal;

public class FetchNameByStateFromProcessingState : ICustomFunctionOperatorBrowsable
{
    private IServiceProvider ServiceProvider { get; }

    public FetchNameByStateFromProcessingState(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public int MinOperandCount => 3;

    public int MaxOperandCount => 3;

    public string Description => "Fetch Name from Processing State by State";

    public FunctionCategory Category => FunctionCategory.All;

    public string Name => "FetchNameByStateFromProcessingState";

    public object Evaluate(params object[] operands)
    {
        var stateProvider = ServiceProvider.GetRequiredService<IDocumentProcessingStateProvider>();

        var tenantId = (Guid)operands[0];
        var entity = operands[1] as string;
        var state = (int)operands[2];

        return stateProvider.ProcessingStateNameForTenantAndEntityAndState(tenantId, entity, state);
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
                return type == typeof(int);
        }

        return false;
    }

    public Type ResultType(params Type[] operands)
    {
        return typeof(string);
    }
}