using Microsoft.CodeAnalysis;

internal static class Utilities
{
    public static bool HasIgnoreAttribute(IEnumerable<AttributeData> attributes)
        => attributes.Any(static a => a.AttributeClass?.ToDisplayString()
        == AttributeNames.RecordIgnore);

    public static INamedTypeSymbol? GetUnmarkedRecord(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (GeneratorUtilities.GetSymbol<INamedTypeSymbol>(context, cancellationToken) is not { } type
            || GeneratorUtilities.HasAttribute(type, AttributeNames.Record)
            || !IsFirstDeclaration(type, context.Node))
        {
            return null;
        }
        return type;
    }

    private static bool IsFirstDeclaration(ISymbol symbol, SyntaxNode node)
    {
        var first = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        return first is not null
            && first.SyntaxTree == node.SyntaxTree
            && first.Span == node.Span;
    }
}
