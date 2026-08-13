using Microsoft.CodeAnalysis;

namespace RecordsPlus;

public sealed partial class RecordsPlusSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeNames.Record,
            GeneratorUtilities.IsTypeDeclarationWithAttributes,
            static (c, ct) => TypeModel.Create((INamedTypeSymbol)c.TargetSymbol))
            .WithTrackingName(TrackingNames.TypeModels);

        context.RegisterSourceOutput(types, Emitter.GenerateSource);
    }
}
