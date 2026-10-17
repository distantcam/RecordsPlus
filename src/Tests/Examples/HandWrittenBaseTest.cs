public partial class HandWrittenBase
{
    protected virtual global::System.Type EqualityContract => typeof(HandWrittenBase);

    protected virtual bool PrintMembers(global::System.Text.StringBuilder builder) => false;
}

[RecordsPlus.Record]
public partial class HandWrittenBaseLeaf : HandWrittenBase
{
    public int B { get; set; }
}
