[RecordsPlus.Record]
public partial class DeepBase
{
    public int A { get; set; }
}

// Not marked, so nothing is generated for it, but it still inherits DeepBase's members.
public partial class DeepMiddle : DeepBase
{
    public int B { get; set; }
}

[RecordsPlus.Record]
public partial class DeepLeaf : DeepMiddle
{
    public int C { get; set; }
}
