using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using RecordsPlus;

namespace CompileBenchmarks;

[ShortRunJob]
public class Records
{
    [Params(1000, 5000)]
    public int RecordCount { get; set; }

    [Params(true, false)]
    public bool WithAttribute { get; set; }

    private CSharpCompilation _compilation = null!;
    private GeneratorDriver _driver = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var frameworkReferences = await ReferenceAssemblies.Net.Net100
            .ResolveAsync(CSharpParseOptions.Default.Language, CancellationToken.None)
            .ConfigureAwait(false);

        _compilation = CSharpCompilation.Create(nameof(Records))
            .WithOptions(new(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(frameworkReferences)
            .AddReferences(MetadataReference
                .CreateFromFile(typeof(RecordAttribute).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public interface IPlugin;"))
            .AddSyntaxTrees(Enumerable.Range(1, RecordCount)
                .Select(i => CSharpSyntaxTree.ParseText(CreateRecordCode(WithAttribute, i))));

        _driver = CSharpGeneratorDriver.Create(new RecordsPlusSourceGenerator());
    }

    private static string CreateRecordCode(bool withAttribute, int i)
    {
        return @$"
{(withAttribute ? "[RecordsPlus.Record]" : "")}
public partial record Example{i:D4}
{{
    public string Name {{ get; set; }}
    [RecordsPlus.RecordIgnore]
    public int Value {{ get; set; }}
}}
";
    }

    [Benchmark]
    public GeneratorDriver Generate() => _driver.RunGenerators(_compilation);
}
