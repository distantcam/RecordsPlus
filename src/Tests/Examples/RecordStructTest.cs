[RecordsPlus.Record]
public partial record struct RecordStructTest
{
    [RecordsPlus.RecordIgnore(ignoreSetting: RecordsPlus.IgnoreSetting.Equality)]
    public int IgnoreEquality { get; set; }
    [RecordsPlus.RecordIgnore(ignoreSetting: RecordsPlus.IgnoreSetting.ToString)]
    public string? IgnoreToString { get; set; }
}
