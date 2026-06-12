using Microsoft.CodeAnalysis;

namespace RecordsPlus;

public sealed partial class RecordsPlusSourceGenerator : ISourceGenerator
{
    private sealed class SyntaxContextReceiver(CancellationToken cancellationToken) : ISyntaxContextReceiver
    {
        public List<TypeModel>? TypeModels { get; private set; }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            INamedTypeSymbol? type;
            if (GeneratorUtilities.IsTypeDeclarationWithAttributes(context.Node, cancellationToken)

                && (type = GeneratorUtilities.GetSymbol<INamedTypeSymbol>(context, cancellationToken)) != null

                && Utilities.HasAttribute(type, AttributeNames.Record))
            {
                (TypeModels ??= []).Add(TypeModel.Create(type));
            }
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(static () =>
            new SyntaxContextReceiver(CancellationToken.None));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxContextReceiver receiver
            || receiver.TypeModels == null)
            return;

        foreach (var typeModel in receiver.TypeModels)
        {
            Emitter.GenerateSource(context, typeModel);
        }
    }
}
