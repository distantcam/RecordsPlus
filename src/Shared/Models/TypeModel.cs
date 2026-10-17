using Microsoft.CodeAnalysis;

internal readonly record struct TypeModel(
    string? Namespace,
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
        var members = type.GetMembers()
            .Where(IsPrintable)
            .Select(MemberModel.Create);

        var qualifiedName = type.ToDisplayString(MinimallyQualifiedFormat);

        var equalityContractBase = FindBaseDeclaring(type, DeclaresEqualityContract);
        var printMembersBase = FindBaseDeclaring(type, DeclaresPrintMembers);
        var equalsBase = FindBaseDeclaring(type, DeclaresOverridableEquals);

        return new(
            Namespace: GeneratorUtilities.GetNamespace(type),
            TypeDeclarations: GeneratorUtilities.GetTypeDeclarations(type),

            Name: type.Name,
            QualifiedName: qualifiedName,
            HintName: GeneratorUtilities.GetHintName(type),

            IsRecord: type.IsRecord,
            IsStruct: type.IsValueType,
            IsReadOnly: type.IsReadOnly,
            IsSealed: type.IsSealed,
            BaseTypeName: equalsBase?.ToDisplayString(FullyQualifiedFormat),
            BaseTypeHasPrintMember: printMembersBase is not null,
            BaseTypeHasEqualityContract: equalityContractBase is not null,

            Members: new(members)
        );
    }

    // The member can be declared anywhere up the chain - an unmarked base still inherits it -
    // so keep walking until it is found rather than only looking at the immediate base.
    private static INamedTypeSymbol? FindBaseDeclaring(INamedTypeSymbol type, Func<INamedTypeSymbol, bool> declares)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            // A base marked [Record] has these generated for it, but not until after this type
            // is modelled, so trust the attribute instead of looking the members up.
            if (GeneratorUtilities.HasAttribute(baseType, AttributeNames.Record) || declares(baseType))
                return baseType;
        }
        return null;
    }

    private static bool DeclaresEqualityContract(INamedTypeSymbol type)
        => type.GetMembers("EqualityContract").OfType<IPropertySymbol>()
            .Any(static s => s.GetMethod is not null
                && s.Type.ToDisplayString(FullyQualifiedFormat) == "global::System.Type");

    // Without a base `Equals(Base)` to chain to, `base.Equals((Base)other)` would silently
    // bind to `object.Equals(object)` and compare by reference, so only chain when there is one.
    private static bool DeclaresOverridableEquals(INamedTypeSymbol type)
        => type.SpecialType != SpecialType.System_Object // object.Equals(object) is not a chain
        && type.GetMembers("Equals").OfType<IMethodSymbol>()
            .Any(m => !m.IsSealed
                && (m.IsVirtual || m.IsOverride || m.IsAbstract)
                && m.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, type));

    private static bool DeclaresPrintMembers(INamedTypeSymbol type)
        => type.GetMembers("PrintMembers").OfType<IMethodSymbol>()
            .Any(static s => s.Parameters.Length == 1
                && s.Parameters[0].Type.ToDisplayString(FullyQualifiedFormat) == "global::System.Text.StringBuilder");

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
        var ignoreAttribute = symbol.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString()
            == AttributeNames.RecordIgnore);
        var ignoreValue = (int?)ignoreAttribute?.ConstructorArguments[0].Value;
        return ignoreValue == 3;
    }
}

internal readonly record struct MemberModel(
    string Name,
    SymbolKind Kind,
    bool IsValueType,
    bool IgnoreEquality,
    bool IgnoreToString,
    string TypeName
)
{
    public static MemberModel Create(ISymbol member)
    {
        var ignoreAttribute = member.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString()
            == AttributeNames.RecordIgnore);
        var ignoreValue = (int?)ignoreAttribute?.ConstructorArguments[0].Value ?? 0;

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
            IgnoreEquality: (ignoreValue & 1) != 0, // IgnoreSetting.Equality
            IgnoreToString: (ignoreValue & 2) != 0, // IgnoreSetting.ToString
            TypeName: type.ToDisplayString(FullyQualifiedFormat)
        );
    }
}
