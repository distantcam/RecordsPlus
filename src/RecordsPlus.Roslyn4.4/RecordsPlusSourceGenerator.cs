using Microsoft.CodeAnalysis;

namespace RecordsPlus;

public sealed partial class RecordsPlusSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var markedTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeNames.Record,
            GeneratorUtilities.IsTypeDeclarationWithAttributes,
            static (c, _) => TypeModel.Create((INamedTypeSymbol)c.TargetSymbol))
            .WithTrackingName(TrackingNames.TypeModels);

        var unmarkedRecords = context.SyntaxProvider.CreateSyntaxProvider(
            GeneratorUtilities.IsRecordDeclaration,
            Utilities.GetUnmarkedRecordModel)
            .WithTrackingName(TrackingNames.UnmarkedRecordModels);

        context.RegisterSourceOutput(markedTypes, Emitter.GenerateSource);
        context.RegisterSourceOutput(unmarkedRecords, Emitter.GenerateSourceIfPresent);
    }
}
