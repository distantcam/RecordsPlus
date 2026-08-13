[RecordsPlus.Record]
public partial class IgnoreSettingTest
{
    [RecordsPlus.RecordIgnore(ignoreSetting: RecordsPlus.IgnoreSetting.Equality)]
    public int IgnoreEquality { get; set; }
    [RecordsPlus.RecordIgnore(ignoreSetting: RecordsPlus.IgnoreSetting.ToString)]
    public string? IgnoreToString { get; set; }
}
