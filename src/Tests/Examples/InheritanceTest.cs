[RecordsPlus.Record]
public partial class Animal
{
    public string? Name { get; set; }
}

[RecordsPlus.Record]
public partial class Dog : Animal
{
    public string? Breed { get; set; }
}
