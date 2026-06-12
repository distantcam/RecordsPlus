using Microsoft.CodeAnalysis;

internal static class Utilities
{
    public static bool HasIgnoreAttribute(IEnumerable<AttributeData> attributes) =>
        attributes.Any(static a => a.AttributeClass?.ToDisplayString()
        == AttributeNames.RecordIgnore);

    public static bool HasAttribute(ISymbol? symbol, string attributeName)
    {
        return symbol != null && symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeName);
    }
}
