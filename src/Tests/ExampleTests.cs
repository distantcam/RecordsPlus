using RecordsPlus;
using Microsoft.CodeAnalysis;
using static ExampleTestsHelper;

#if ROSLYN_4_4
using Microsoft.CodeAnalysis.CSharp;
#endif

internal sealed class ExampleTests
{
    [Test]
    [CombinedDataSources]
    public async Task ExamplesGeneratedCode(
        [MethodDataSource(nameof(GetExamples))] CodeFileTheoryData theoryData,
        [ClassDataSource<CompilationBuilderFactory>(Shared = SharedType.PerTestSession)] CompilationBuilderFactory builderFactory)
    {
        var builder = builderFactory.Create(theoryData);
        var compilation = builder.Build(nameof(ExampleTests));
        var driver = new GeneratorDriverBuilder()
            .AddGenerator(new RecordsPlusSourceGenerator())
            .WithAnalyzerOptions(theoryData.Options)
            .Build(builder.ParseOptions)
            .RunGenerators(compilation, TestHelper.CancellationToken);

        await Verify(driver)
            .UseDirectory(theoryData.VerifiedDirectory)
            .UseTypeName(theoryData.Name)
            .UseMethodName("cs")
            .IgnoreParameters()
            .ConfigureAwait(false);
    }

    [Test]
    [CombinedDataSources]
    public async Task CodeCompilesProperly(
        [MethodDataSource(nameof(GetExamples))] CodeFileTheoryData theoryData,
        [ClassDataSource<CompilationBuilderFactory>(Shared = SharedType.PerTestSession)] CompilationBuilderFactory builderFactory)
    {
        var builder = builderFactory.Create(theoryData);
        var compilation = builder.Build(nameof(ExampleTests));
        new GeneratorDriverBuilder()
            .AddGenerator(new RecordsPlusSourceGenerator())
            .WithAnalyzerOptions(theoryData.Options)
            .Build(builder.ParseOptions)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out _,
                TestHelper.CancellationToken);

        await Assert.That(outputCompilation.GetDiagnostics(TestHelper.CancellationToken)
            .Where(d => !theoryData.IgnoredCompileDiagnostics.Contains(d.Id)))
            .IsEmpty()
            .ConfigureAwait(false);
    }

#if ROSLYN_4_4
    [Test]
    [CombinedDataSources]
    public async Task EnsureRunsAreCachedCorrectly(
        [MethodDataSource(nameof(GetExamples))] CodeFileTheoryData theoryData,
        [ClassDataSource<CompilationBuilderFactory>(Shared = SharedType.PerTestSession)] CompilationBuilderFactory builderFactory)
    {
        var builder = builderFactory.Create(theoryData);
        var compilation = builder.Build(nameof(ExampleTests));

        var driver = new GeneratorDriverBuilder()
            .AddGenerator(new RecordsPlusSourceGenerator())
            .WithAnalyzerOptions(theoryData.Options)
            .Build(builder.ParseOptions);

        driver = driver.RunGenerators(compilation, TestHelper.CancellationToken);
        var firstResult = driver.GetRunResult();

        // Change the compilation
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("// dummy",
            CSharpParseOptions.Default.WithLanguageVersion(theoryData.LangPreview
                ? LanguageVersion.Preview
                : LanguageVersion.Latest),
            cancellationToken: TestHelper.CancellationToken));

        driver = driver.RunGenerators(compilation, TestHelper.CancellationToken);
        var secondResult = driver.GetRunResult();

        await AssertRunsEqual(firstResult, secondResult,
            RecordsPlusSourceGenerator.TrackingNames.AllTrackers)
            .ConfigureAwait(false);
    }
#endif

    // ----------------------------------------------------------------------------------------

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated in generated code.")]
    internal sealed class CompilationBuilderFactory : CompilationBuilderFactory<RecordAttribute>
    {
        protected override IEnumerable<string> GetNuGetIds() => ["Microsoft.Extensions.DependencyInjection.Abstractions"];
    }

    public static IEnumerable<Func<CodeFileTheoryData>> GetExamples()
    {
        foreach (var example in GetExamplesFiles("Examples"))
        {
            yield return () => new CodeFileTheoryData(example);
        }
    }
}
