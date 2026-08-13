[RecordsPlus.Record]
public partial class IgnoreAllToStringTest
{
    [RecordsPlus.RecordIgnore(RecordsPlus.IgnoreSetting.ToString)]
    public int Value { get; set; }
    [RecordsPlus.RecordIgnore(RecordsPlus.IgnoreSetting.ToString)]
    public string? Text { get; set; }
}
