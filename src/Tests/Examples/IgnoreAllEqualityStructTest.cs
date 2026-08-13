[RecordsPlus.Record]
public partial struct IgnoreAllEqualityStructTest
{
    [RecordsPlus.RecordIgnore(RecordsPlus.IgnoreSetting.Equality)]
    public int Value { get; set; }
    [RecordsPlus.RecordIgnore(RecordsPlus.IgnoreSetting.Equality)]
    public string? Text { get; set; }
}
