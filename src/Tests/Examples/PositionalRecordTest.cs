public partial record PositionalRecordTest(int Number, string? Text);

public partial record struct PositionalRecordStructTest(
    [property: RecordsPlus.RecordIgnore(RecordsPlus.IgnoreSetting.Equality)] int Skipped,
    int Kept);
