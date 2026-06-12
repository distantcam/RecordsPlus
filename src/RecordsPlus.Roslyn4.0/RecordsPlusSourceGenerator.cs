using Microsoft.CodeAnalysis;

namespace RecordsPlus;

public sealed partial class RecordsPlusSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            GeneratorUtilities.IsTypeDeclarationWithAttributes,
            GeneratorUtilities.GetSymbol<INamedTypeSymbol>)
            .Where(static x => Utilities.HasAttribute(x, AttributeNames.Record))
            .Select(static (x, _) => TypeModel.Create(x!))
            .Where(static (t) => !t.IsRecord);

        context.RegisterSourceOutput(types, Emitter.GenerateSource);
    }
}
