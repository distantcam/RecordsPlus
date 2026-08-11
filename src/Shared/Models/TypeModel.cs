using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

internal readonly record struct TypeModel(
    string? Namespace,
    string? ClassBase,
    EquatableList<string> TypeDeclarations,

    string Name,
    string QualifiedName,
    string HintName,

    bool IsRecord,
    bool IsStruct,
    bool IsReadOnly,
    bool IsSealed,
    string? BaseTypeName,
    bool BaseTypeHasPrintMember,
    bool BaseTypeHasEqualityContract,

    EquatableList<MemberModel> Members
) : IPartialTypeModel
{
    public static TypeModel Create(INamedTypeSymbol type)
    {
        var baseMembers = type.BaseType?.GetMembers() ?? ImmutableArray<ISymbol>.Empty;

        var members = type.GetMembers()
            .Where(IsPrintable)
            .Select(MemberModel.Create);

        var qualifiedName = type.ToDisplayString(MinimallyQualifiedFormat);

        var baseType = type.BaseType;
        var baseHasAttribute = baseType is not null
            && GeneratorUtilities.HasAttribute(baseType, AttributeNames.Record);

        var baseHasEqualityContract = baseHasAttribute
            || baseMembers.OfType<IPropertySymbol>()
                .Any(static s => s.Name == "EqualityContract"
                    && s.GetMethod != null
                    && s.Type.ToDisplayString(FullyQualifiedFormat) == "System.Type");

        var baseHasPrintMembers = baseHasAttribute
            || baseMembers.OfType<IMethodSymbol>()
                .Any(static s => s.Name == "PrintMembers"
                    && s.Parameters.Length == 1
                    && s.Parameters[0].Type.ToDisplayString(FullyQualifiedFormat) == "System.Text.StringBuilder");

        return new(
            Namespace: GeneratorUtilities.GetNamespace(type),
            ClassBase: $"global::System.IEquatable<{qualifiedName}>",
            TypeDeclarations: GeneratorUtilities.GetTypeDeclarations(type),

            Name: type.Name,
            QualifiedName: qualifiedName,
            HintName: GeneratorUtilities.GetHintName(type),

            IsRecord: type.IsRecord,
            IsStruct: type.IsValueType,
            IsReadOnly: type.IsReadOnly,
            IsSealed: type.IsSealed,
            BaseTypeName: baseHasEqualityContract
                ? baseType!.ToDisplayString(FullyQualifiedFormat)
                : null,
            BaseTypeHasPrintMember: baseHasPrintMembers,
            BaseTypeHasEqualityContract: baseHasEqualityContract,

            Members: new(members)
        );
    }

    private static bool IsPrintable(ISymbol m)
    {
        if (!IsPublicInstanceMember(m))
        {
            return false;
        }

        if (HasIgnored(m))
        {
            return false;
        }

        if (m.Kind is SymbolKind.Field)
        {
            return true;
        }

        if (m.Kind is SymbolKind.Property)
        {
            var property = (IPropertySymbol)m;
            return IsPrintableProperty(property);
        }

        return false;
    }

    private static bool IsPublicInstanceMember(ISymbol m)
    {
        return m.DeclaredAccessibility == Accessibility.Public && !m.IsStatic;
    }

    private static bool IsPrintableProperty(IPropertySymbol property)
    {
        return !property.IsIndexer && !property.IsOverride && property.GetMethod is not null;
    }

    private static bool HasIgnored(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Any(static a => a.AttributeClass?.ToDisplayString() == AttributeNames.RecordIgnore);
    }
}

internal readonly record struct MemberModel(
    string Name,
    SymbolKind Kind,
    bool IsValueType,
    string TypeName
)
{
    public static MemberModel Create(ISymbol member)
    {
        var type = member.Kind switch
        {
            SymbolKind.Field => ((IFieldSymbol)member).Type,
            SymbolKind.Property => ((IPropertySymbol)member).Type,
            _ => throw new InvalidOperationException($"Unexpected symbol kind: {member.Kind}")
        };

        return new(
            Name: member.Name,
            Kind: member.Kind,
            IsValueType: type.IsValueType,
            TypeName: type.ToDisplayString(FullyQualifiedFormat)
        );
    }
}
