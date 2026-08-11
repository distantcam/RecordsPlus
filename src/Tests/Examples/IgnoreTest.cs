[RecordsPlus.Record]
public partial class IgnoreTest
{
    [RecordsPlus.RecordIgnore]
    public int Value;
    [RecordsPlus.RecordIgnore]
    public int Number { get; set; }
    [RecordsPlus.RecordIgnore]
    public string? Text { get; set; }
}
