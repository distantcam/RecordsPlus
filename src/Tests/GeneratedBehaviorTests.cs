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
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)] ExampleTests.CompilationBuilderFactory builderFactory)
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
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)] ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var instance = CreateInstance(builderFactory, "IgnoreAllToStringTest", "IgnoreAllToStringTest",
            ("Value", 42), ("Text", "hello"));

        await Assert.That(instance.ToString()).IsEqualTo("IgnoreAllToStringTest { }")
            .ConfigureAwait(false);
    }

    [Test]
    [CombinedDataSources]
    public async Task ToStringWithInheritedMembersDoesNotEmitTrailingSeparator(
        [ClassDataSource<ExampleTests.CompilationBuilderFactory>(Shared = SharedType.PerTestSession)] ExampleTests.CompilationBuilderFactory builderFactory)
    {
        var instance = CreateInstance(builderFactory, "IgnoreAllToStringInheritanceTest", "IgnoreAllToString",
            ("Value", "Bar"), ("Number", 3));

        await Assert.That(instance.ToString()).IsEqualTo("IgnoreAllToString { Value = Bar }")
            .ConfigureAwait(false);
    }

    // ----------------------------------------------------------------------------------------

    private static object CreateInstance(
        ExampleTests.CompilationBuilderFactory builderFactory,
        string exampleName,
        string typeName,
        params (string Name, object Value)[] members)
    {
        var assembly = Emit(builderFactory, exampleName);
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

    private static System.Reflection.Assembly Emit(ExampleTests.CompilationBuilderFactory builderFactory, string exampleName)
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

        static IEnumerable<string> Errors(ImmutableArray<Diagnostic> diagnostics) =>
            diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString());
    }
}
