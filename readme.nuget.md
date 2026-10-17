# RecordsPlus

RecordsPlus is a Roslyn Source Generator that provides the same features records have to classes and structs.

# How to Use

- Make the class or struct `partial`
- Add `[Record]`
- Remove the hand written equality and `ToString` members

```csharp
[Record]
public partial class Person
{
    public string? Name { get; set; }
    public int Age { get; set; }
}
```

Members can be left out of the generated code with `[RecordIgnore]`.

```csharp
[Record]
public partial class Person
{
    public string? Name { get; set; }

    [RecordIgnore(IgnoreSetting.ToString)]
    public string? Secret { get; set; }

    [RecordIgnore(IgnoreSetting.Equality)]
    public DateTime LastAccessed { get; set; }
}
```

Full documentation is at https://github.com/distantcam/RecordsPlus
