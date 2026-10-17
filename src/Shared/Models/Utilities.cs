using Microsoft.CodeAnalysis;

internal static class Utilities
{
    public static bool HasIgnoreAttribute(IEnumerable<AttributeData> attributes)
        => attributes.Any(static a => a.AttributeClass?.ToDisplayString()
        == AttributeNames.RecordIgnore);
}
