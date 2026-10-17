public partial record DerivedRecordBase
{
    public int A { get; set; }
}

[RecordsPlus.Record]
public partial record DerivedRecordLeaf : DerivedRecordBase
{
    public int B { get; set; }
}
