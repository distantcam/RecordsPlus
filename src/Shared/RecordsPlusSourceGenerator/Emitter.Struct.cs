
#if ROSLYN_3
using EmitterContext = Microsoft.CodeAnalysis.GeneratorExecutionContext;
#elif ROSLYN_4
using EmitterContext = Microsoft.CodeAnalysis.SourceProductionContext;
#endif

namespace RecordsPlus;

public partial class RecordsPlusSourceGenerator
{
    private static partial class Emitter
    {
        private static void EmitRecordStruct(CodeBuilder source, TypeModel type)
        {
            var readOnly = !type.IsReadOnly;

            EmitPrintMembers(source, type);
            source.AppendLine();
            EmitStructEquals(source, type);
            source.AppendLine();
            EmitGetHashCode(source, type, readOnly: readOnly);
        }

        private static void EmitStruct(CodeBuilder source, TypeModel type)
        {
            // Members of a `readonly struct` are implicitly readonly, so only emit the
            // explicit modifier when the struct itself is mutable (matching the compiler).
            var readOnly = !type.IsReadOnly;

            EmitToString(source, type, readOnly);
            source.AppendLine();
            EmitPrintMembers(source, type);
            source.AppendLine();
            EmitStructEquals(source, type);
            source.AppendLine();
            EmitStructObjectEquals(source, type);
            source.AppendLine();
            EmitGetHashCode(source, type, readOnly);
            source.AppendLine();
            EmitStructEqualityOperators(source, type);
        }

        private static void EmitStructEquals(CodeBuilder source, TypeModel type)
        {
            var readOnly = type.IsReadOnly ? "" : "readonly ";
            source.AppendLine($"public {readOnly}bool Equals({type.QualifiedName} other)");
            using (source.StartBlock())
            {
                if (!type.Members.Any(static m => !m.IgnoreEquality))
                {
                    source.AppendLine("return true;");
                    return;
                }

                source.AppendIndent().Append("return ");
                var first = true;
                foreach (var member in type.Members)
                {
                    if (member.IgnoreEquality)
                        continue;

                    if (!first)
                        source.Append(" && ");
                    first = false;
                    source.Append($"{EqualityComparer(member)}.Equals({member.Name}, other.{member.Name})");
                }
                source.Append(";").AppendLine();
            }
        }

        private static void EmitStructObjectEquals(CodeBuilder source, TypeModel type)
        {
            var readOnly = type.IsReadOnly ? "" : "readonly ";
            source.AppendLine($"public override {readOnly}bool Equals(object obj)");
            using (source.StartBlock())
            {
                source.AppendLine($"return obj is {type.QualifiedName} other && Equals(other);");
            }
        }

        private static void EmitStructEqualityOperators(CodeBuilder source, TypeModel type)
        {
            source.AppendLine($"public static bool operator ==({type.QualifiedName} left, {type.QualifiedName} right)");
            using (source.StartBlock())
            {
                source.AppendLine("return left.Equals(right);");
            }
            source.AppendLine();
            source.AppendLine($"public static bool operator !=({type.QualifiedName} left, {type.QualifiedName} right)");
            using (source.StartBlock())
            {
                source.AppendLine("return !(left == right);");
            }
        }
    }
}
