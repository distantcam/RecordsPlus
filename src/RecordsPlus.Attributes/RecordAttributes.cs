using System.Diagnostics;
using static System.AttributeTargets;

namespace RecordsPlus;

[AttributeUsage(Class | Struct, Inherited = false)]
[Conditional("RECORDSPLUS_USAGES")]
public sealed class RecordAttribute : Attribute;
