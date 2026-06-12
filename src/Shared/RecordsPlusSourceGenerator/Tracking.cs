namespace RecordsPlus;

public partial class RecordsPlusSourceGenerator
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design", "CA1034:Nested types should not be visible",
        Justification = "Constants used in tests.")]
    public static class TrackingNames
    {
        public static string TypeModels => nameof(TypeModels);

        public static IReadOnlyCollection<string> AllTrackers { get; } = [
            TypeModels,
        ];
    }
}
