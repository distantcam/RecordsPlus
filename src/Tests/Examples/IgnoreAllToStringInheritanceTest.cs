[RecordsPlus.Record]
public partial class IgnoreAllToStringBase
{
    public string? Value { get; set; }
}

[RecordsPlus.Record]
public partial class IgnoreAllToString : IgnoreAllToStringBase
{
    [RecordsPlus.RecordIgnore(RecordsPlus.IgnoreSetting.ToString)]
    public int Number { get; set; }
}
