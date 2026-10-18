using Microsoft.CodeAnalysis;

namespace RecordsPlus;

public sealed partial class RecordsPlusSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            GeneratorUtilities.IsTypeDeclarationWithAttributes,
            GeneratorUtilities.GetSymbol<INamedTypeSymbol>)
            .Where(static x => GeneratorUtilities.HasAttribute(x, AttributeNames.Record))
            .Select(static (x, _) => TypeModel.Create(x!));

        var unmarkedRecords = context.SyntaxProvider.CreateSyntaxProvider(
            GeneratorUtilities.IsRecordDeclaration,
            Utilities.GetUnmarkedRecord)
            .Where(static x => x is not null)
            .Select(static (x, _) => TypeModel.Create(x!));

        context.RegisterSourceOutput(types, Emitter.GenerateSource);
        context.RegisterSourceOutput(unmarkedRecords, Emitter.GenerateSource);
    }
}
