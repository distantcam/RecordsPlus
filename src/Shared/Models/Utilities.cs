using Microsoft.CodeAnalysis;

internal static class Utilities
{
    // Comparing `AttributeClass.ToDisplayString()` formats and allocates a string for every
    // attribute examined, and that is paid for every member of every type and every base in
    // the chain, so match the name and namespace in place instead.
    public static bool IsRecordAttribute(INamedTypeSymbol? attributeClass)
        => GeneratorUtilities.IsType(attributeClass, AttributeNames.Namespace, AttributeNames.RecordName);

    public static bool IsRecordIgnoreAttribute(INamedTypeSymbol? attributeClass)
        => GeneratorUtilities.IsType(attributeClass, AttributeNames.Namespace, AttributeNames.RecordIgnoreName);

    public static bool HasRecordAttribute(ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (IsRecordAttribute(attribute.AttributeClass))
                return true;
        }
        return false;
    }

    // Both of these model the type inside the syntax transform on purpose. Handing an
    // INamedTypeSymbol back to the pipeline instead puts it in the generator's cached state
    // tables, and every cached entry then roots an entire Compilation for as long as the IDE
    // session lasts; only value-equatable models should cross a node boundary.
    public static TypeModel? GetMarkedTypeModel(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (GeneratorUtilities.GetSymbol<INamedTypeSymbol>(context, cancellationToken) is not { } type
            || !HasRecordAttribute(type))
        {
            return null;
        }
        return TypeModel.Create(type);
    }

    public static TypeModel? GetUnmarkedRecordModel(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        // IsFirstDeclaration is a pair of field compares while HasRecordAttribute walks the
        // attribute list, so let the cheap check throw out the extra partial declarations.
        if (GeneratorUtilities.GetSymbol<INamedTypeSymbol>(context, cancellationToken) is not { } type
            || !IsFirstDeclaration(type, context.Node)
            || HasRecordAttribute(type))
        {
            return null;
        }
        return TypeModel.Create(type);
    }

    private static bool IsFirstDeclaration(ISymbol symbol, SyntaxNode node)
    {
        var first = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        return first is not null
            && first.SyntaxTree == node.SyntaxTree
            && first.Span == node.Span;
    }
}
