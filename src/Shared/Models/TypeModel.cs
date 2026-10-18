using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

internal sealed record TypeModel(
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
        var typeMembers = type.GetMembers();
        var members = ImmutableArray.CreateBuilder<MemberModel>(typeMembers.Length);
        foreach (var member in typeMembers)
        {
            if (MemberModel.TryCreate(member, out var model))
                members.Add(model);
        }

        var qualifiedName = type.ToDisplayString(MinimallyQualifiedFormat);

        var (equalityContractBase, printMembersBase, equalsBase) = FindBases(type);

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

            Members: new(members.ToImmutable())
        );
    }

    // The member can be declared anywhere up the chain - an unmarked base still inherits it -
    // so keep walking until it is found rather than only looking at the immediate base.
    // All three answers come out of one walk: the chain can be long and every base costs an
    // attribute scan, so walking it once per answer tripled that cost.
    private static (INamedTypeSymbol? EqualityContract, INamedTypeSymbol? PrintMembers, INamedTypeSymbol? EqualsBase) FindBases(INamedTypeSymbol type)
    {
        INamedTypeSymbol? equalityContract = null;
        INamedTypeSymbol? printMembers = null;
        INamedTypeSymbol? equals = null;

        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            // A base marked [Record] has these generated for it, but not until after this type
            // is modelled, so trust the attribute instead of looking the members up.
            var marked = Utilities.HasRecordAttribute(baseType);

            equalityContract ??= marked || DeclaresEqualityContract(baseType) ? baseType : null;
            printMembers ??= marked || DeclaresPrintMembers(baseType) ? baseType : null;
            equals ??= marked || DeclaresOverridableEquals(baseType) ? baseType : null;

            if (equalityContract is not null && printMembers is not null && equals is not null)
                break;
        }

        return (equalityContract, printMembers, equals);
    }

    private static bool DeclaresEqualityContract(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers("EqualityContract"))
        {
            if (member is IPropertySymbol { GetMethod: not null } property
                && IsSystemType(property.Type))
                return true;
        }
        return false;
    }

    // Without a base `Equals(Base)` to chain to, `base.Equals((Base)other)` would silently
    // bind to `object.Equals(object)` and compare by reference, so only chain when there is one.
    private static bool DeclaresOverridableEquals(INamedTypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_Object) // object.Equals(object) is not a chain
            return false;

        foreach (var member in type.GetMembers("Equals"))
        {
            if (member is IMethodSymbol { IsSealed: false, Parameters.Length: 1 } method
                && (method.IsVirtual || method.IsOverride || method.IsAbstract)
                && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, type))
                return true;
        }
        return false;
    }

    private static bool DeclaresPrintMembers(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers("PrintMembers"))
        {
            if (member is IMethodSymbol { Parameters.Length: 1 } method
                && IsStringBuilder(method.Parameters[0].Type))
                return true;
        }
        return false;
    }

    // Formatting the type to a `global::` string just to compare it allocates one string per
    // candidate member, for every base in the chain. The symbol shape says the same thing.
    private static bool IsSystemType(ITypeSymbol type)
        => type is INamedTypeSymbol
        {
            Name: "Type",
            ContainingType: null,
            ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true }
        };

    private static bool IsStringBuilder(ITypeSymbol type)
        => type is INamedTypeSymbol
        {
            Name: "StringBuilder",
            ContainingType: null,
            ContainingNamespace:
            {
                Name: "Text",
                ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true }
            }
        };
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
    public static bool TryCreate(ISymbol member, out MemberModel model)
    {
        model = default;

        if (member.DeclaredAccessibility != Accessibility.Public || member.IsStatic)
            return false;

        ITypeSymbol type;
        switch (member.Kind)
        {
            case SymbolKind.Field:
                type = ((IFieldSymbol)member).Type;
                break;

            case SymbolKind.Property:
                var property = (IPropertySymbol)member;
                if (property.IsIndexer || property.IsOverride || property.GetMethod is null)
                    return false;
                type = property.Type;
                break;

            default:
                return false;
        }

        // The ignore setting both drops the member entirely (IgnoreSetting.All) and supplies the
        // per-feature flags, so read it once instead of scanning the attributes a second time.
        var ignore = GetIgnoreSetting(member);
        if (ignore == 3) // IgnoreSetting.All
            return false;

        model = new(
            Name: member.Name,
            Kind: member.Kind,
            IsValueType: type.IsValueType,
            IgnoreEquality: (ignore & 1) != 0, // IgnoreSetting.Equality
            IgnoreToString: (ignore & 2) != 0, // IgnoreSetting.ToString
            TypeName: type.ToDisplayString(FullyQualifiedFormat)
        );
        return true;
    }

    private static int GetIgnoreSetting(ISymbol member)
    {
        foreach (var attribute in member.GetAttributes())
        {
            if (!Utilities.IsRecordIgnoreAttribute(attribute.AttributeClass))
                continue;

            // Half-typed code in the IDE can leave the argument off entirely.
            return attribute.ConstructorArguments.Length > 0
                ? (int?)attribute.ConstructorArguments[0].Value ?? 0
                : 0;
        }
        return 0;
    }
}
