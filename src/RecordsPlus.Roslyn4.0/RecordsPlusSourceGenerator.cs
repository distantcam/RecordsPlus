using Microsoft.CodeAnalysis;

namespace RecordsPlus;

public sealed partial class RecordsPlusSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider.CreateSyntaxProvider(
            GeneratorUtilities.IsTypeDeclarationWithAttributes,
            Utilities.GetMarkedTypeModel);

        var unmarkedRecords = context.SyntaxProvider.CreateSyntaxProvider(
            GeneratorUtilities.IsRecordDeclaration,
            Utilities.GetUnmarkedRecordModel);

        context.RegisterSourceOutput(types, Emitter.GenerateSourceIfPresent);
        context.RegisterSourceOutput(unmarkedRecords, Emitter.GenerateSourceIfPresent);
    }
}
