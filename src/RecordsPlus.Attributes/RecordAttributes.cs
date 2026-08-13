using System.Diagnostics;
using static System.AttributeTargets;

namespace RecordsPlus;

[Flags]
public enum IgnoreSetting
{
    None,
    Equality = 1,
    ToString = 2,
    All,
}

[AttributeUsage(Class | Struct, Inherited = false)]
[Conditional("RECORDSPLUS_USAGES")]
public sealed class RecordAttribute : Attribute;

[AttributeUsage(Field | Property, Inherited = false)]
[Conditional("RECORDSPLUS_USAGES")]
public sealed class RecordIgnoreAttribute(IgnoreSetting ignoreSetting = IgnoreSetting.All) : Attribute
{
    public IgnoreSetting IgnoreSetting { get; } = ignoreSetting;
}
