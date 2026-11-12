using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RecordsPlus;
using static ExampleTestsHelper;

internal sealed class GeneratedBehaviorTests
{
    [Test]
    [CombinedDataSources]
    public async Task EqualityOperatorIsNotSelfRecursive(
        [MethodDataSource(typeof(ExampleTests), nameof(ExampleTests.GetExamples))] CodeFileTheoryData theoryData,
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)]
        ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var builder = builderFactory.Create(theoryData);
        var compilation = builder.Build(nameof(GeneratedBehaviorTests));
        new GeneratorDriverBuilder()
            .AddGenerator(new RecordsPlusSourceGenerator())
            .WithAnalyzerOptions(theoryData.Options)
            .Build(builder.ParseOptions)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out _,
                TestHelper.CancellationToken);

        var recursiveCalls = new List<string>();

        foreach (var tree in outputCompilation.SyntaxTrees.Except(compilation.SyntaxTrees))
        {
            var model = outputCompilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync(TestHelper.CancellationToken).ConfigureAwait(false);

            foreach (var op in root.DescendantNodes().OfType<OperatorDeclarationSyntax>())
            {
                if (!op.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken))
                    continue;

                var declaring = model.GetDeclaredSymbol(op, TestHelper.CancellationToken)?.ContainingType;

                foreach (var expression in op.DescendantNodes().OfType<BinaryExpressionSyntax>())
                {
                    if (model.GetSymbolInfo(expression, TestHelper.CancellationToken).Symbol
                        is not IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } called)
                        continue;

                    if (SymbolEqualityComparer.Default.Equals(called.ContainingType, declaring))
                        recursiveCalls.Add($"{declaring?.Name}.operator ==: '{expression}' calls {called.Name}");
                }
            }
        }

        await Assert.That(recursiveCalls).IsEmpty()
            .ConfigureAwait(false);
    }

    [Test]
    [CombinedDataSources]
    public async Task ToStringWithEveryMemberIgnoredMatchesEmptyRecordFormat(
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)]
        ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var instance = CreateInstance(builderFactory, "IgnoreAllToStringTest", "IgnoreAllToStringTest",
            ("Value", 42), ("Text", "hello"));

        await Assert.That(instance.ToString()).IsEqualTo("IgnoreAllToStringTest { }")
            .ConfigureAwait(false);
    }

    [Test]
    [CombinedDataSources]
    public async Task ToStringWithInheritedMembersDoesNotEmitTrailingSeparator(
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)]
        ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var instance = CreateInstance(builderFactory, "IgnoreAllToStringInheritanceTest", "IgnoreAllToString",
            ("Value", "Bar"), ("Number", 3));

        await Assert.That(instance.ToString()).IsEqualTo("IgnoreAllToString { Value = Bar }")
            .ConfigureAwait(false);
    }

    [Test]
    [CombinedDataSources]
    public async Task ToStringPrintsMembersThatShadowGeneratedLocals(
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)]
        ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var instance = CreateInstance(builderFactory, "ShadowedMemberNamesTest", "ShadowedMemberNamesTest",
            ("builder", "b"), ("other", "o"), ("hashCode", 7));

        await Assert.That(instance.ToString())
            .IsEqualTo("ShadowedMemberNamesTest { builder = b, other = o, hashCode = 7 }")
            .ConfigureAwait(false);
    }

    [Test]
    [CombinedDataSources]
    public async Task EqualityUsesMembersThatShadowGeneratedLocals(
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)]
        ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var assembly = Emit(builderFactory, "ShadowedMemberNamesTest");
        var baseline = CreateInstance(assembly, "ShadowedMemberNamesTest",
            ("builder", "b"), ("other", "o"), ("hashCode", 7));
        var same = CreateInstance(assembly, "ShadowedMemberNamesTest",
            ("builder", "b"), ("other", "o"), ("hashCode", 7));
        var differentOther = CreateInstance(assembly, "ShadowedMemberNamesTest",
            ("builder", "b"), ("other", "different"), ("hashCode", 7));
        var differentHashCode = CreateInstance(assembly, "ShadowedMemberNamesTest",
            ("builder", "b"), ("other", "o"), ("hashCode", 8));

        using var _ = Assert.Multiple();

        await Assert.That(baseline.Equals(same)).IsTrue().ConfigureAwait(false);
        await Assert.That(baseline.GetHashCode()).IsEqualTo(same.GetHashCode()).ConfigureAwait(false);
        await Assert.That(baseline.Equals(differentOther)).IsFalse().ConfigureAwait(false);
        await Assert.That(baseline.Equals(differentHashCode)).IsFalse().ConfigureAwait(false);
        await Assert.That(baseline.GetHashCode()).IsNotEqualTo(differentHashCode.GetHashCode())
            .ConfigureAwait(false);
    }

    [Test]
    [CombinedDataSources]
    public async Task InheritanceThroughAnUnmarkedBaseStillChains(
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)]
        ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var assembly = Emit(builderFactory, "DeepInheritanceTest");
        var baseline = CreateInstance(assembly, "DeepLeaf", ("A", 1), ("B", 2), ("C", 3));
        var same = CreateInstance(assembly, "DeepLeaf", ("A", 1), ("B", 2), ("C", 3));
        var differentInherited = CreateInstance(assembly, "DeepLeaf", ("A", 9), ("B", 2), ("C", 3));
        // DeepMiddle isn't marked, so B takes no part in equality or ToString.
        var differentUnmarked = CreateInstance(assembly, "DeepLeaf", ("A", 1), ("B", 9), ("C", 3));

        using var _ = Assert.Multiple();

        await Assert.That(baseline.ToString()).IsEqualTo("DeepLeaf { A = 1, C = 3 }")
            .ConfigureAwait(false);
        await Assert.That(baseline.Equals(same)).IsTrue().ConfigureAwait(false);
        await Assert.That(baseline.Equals(differentInherited)).IsFalse().ConfigureAwait(false);
        await Assert.That(baseline.Equals(differentUnmarked)).IsTrue().ConfigureAwait(false);
    }

    [Test]
    [CombinedDataSources]
    public async Task RecordIgnoreAppliesWithoutTheRecordAttribute(
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)]
        ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var assembly = Emit(builderFactory, "RecordWithoutAttributeTest");
        var baseline = CreateInstance(assembly, "RecordWithoutAttributeTest",
            ("IgnoreEquality", 42), ("IgnoreToString", "hello"));
        var differentIgnoredMember = CreateInstance(assembly, "RecordWithoutAttributeTest",
            ("IgnoreEquality", 99), ("IgnoreToString", "hello"));

        using var _ = Assert.Multiple();

        // The compiler's own record ToString calls the generated PrintMembers, so the member
        // ignored for ToString drops out even though nothing marked the type.
        await Assert.That(baseline.ToString())
            .IsEqualTo("RecordWithoutAttributeTest { IgnoreEquality = 42 }")
            .ConfigureAwait(false);
        await Assert.That(baseline.Equals(differentIgnoredMember)).IsTrue().ConfigureAwait(false);
    }

    // ----------------------------------------------------------------------------------------

    private static object CreateInstance(
        ExampleTests.CompilationBuilderFactory builderFactory,
        string exampleName,
        string typeName,
        params (string Name, object Value)[] members)
        => CreateInstance(Emit(builderFactory, exampleName), typeName, members);

    private static object CreateInstance(
        System.Reflection.Assembly assembly,
        string typeName,
        params (string Name, object Value)[] members)
    {
        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"'{typeName}' missing from the generated assembly.");
        var instance = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Could not construct '{typeName}'.");

        foreach (var (name, value) in members)
        {
            var property = type.GetProperty(name)
                ?? throw new InvalidOperationException($"'{typeName}.{name}' missing from the generated assembly.");
            property.SetValue(instance, value);
        }

        return instance;
    }

    private static System.Reflection.Assembly Emit(
        ExampleTests.CompilationBuilderFactory builderFactory, string exampleName)
    {
        var file = GetExamplesFiles("Examples")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == exampleName)
            ?? throw new InvalidOperationException($"Examples/{exampleName}.cs not found.");

        var theoryData = new CodeFileTheoryData(file);
        var builder = builderFactory.Create(theoryData);
        new GeneratorDriverBuilder()
            .AddGenerator(new RecordsPlusSourceGenerator())
            .WithAnalyzerOptions(theoryData.Options)
            .Build(builder.ParseOptions)
            .RunGeneratorsAndUpdateCompilation(
                builder.Build($"{nameof(GeneratedBehaviorTests)}_{exampleName}"),
                out var outputCompilation,
                out _,
                TestHelper.CancellationToken);

        using var peStream = new MemoryStream();
        var result = outputCompilation.Emit(peStream, cancellationToken: TestHelper.CancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Generated code for {exampleName} did not compile:{Environment.NewLine}"
                + string.Join(Environment.NewLine, Errors(result.Diagnostics)));
        }

        return System.Reflection.Assembly.Load(peStream.ToArray());

        static IEnumerable<string> Errors(ImmutableArray<Diagnostic> diagnostics)
            => diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString());
    }
}
