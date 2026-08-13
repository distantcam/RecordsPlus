
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
        private static void EmitRecord(CodeBuilder source, TypeModel type)
        {
            EmitPrintMembers(source, type);
            source.AppendLine();
            EmitClassEquals(source, type);
            source.AppendLine();
            EmitGetHashCode(source, type, readOnly: false);
        }

        private static void EmitClass(CodeBuilder source, TypeModel type)
        {
            EmitEqualityContract(source, type);
            source.AppendLine();
            EmitToString(source, type, readOnly: false);
            source.AppendLine();
            EmitPrintMembers(source, type);
            source.AppendLine();
            EmitClassEquals(source, type);
            source.AppendLine();
            EmitClassObjectEquals(source, type);
            source.AppendLine();
            EmitGetHashCode(source, type, readOnly: false);
            source.AppendLine();
            EmitClassEqualityOperators(source, type);
        }

        private static void EmitEqualityContract(CodeBuilder source, TypeModel type)
        {
            var modifier = type.BaseTypeHasEqualityContract ? "protected override"
                : type.IsSealed ? "private"
                : "protected virtual";

            source.AppendLine($"{modifier} global::System.Type EqualityContract");
            using (source.StartBlock())
            {
                using (source.StartBlock("get"))
                {
                    source.AppendLine($"return typeof({type.QualifiedName});");
                }
            }
        }

        private static void EmitClassEquals(CodeBuilder source, TypeModel type)
        {
            var equalsModifier = type.IsSealed ? "public" : "public virtual";

            if (type.BaseTypeName is not null)
            {
                // Override the base record's Equals so that comparing through the base
                // type still routes to the most derived equality check.
                source.AppendLine($"public override bool Equals({type.BaseTypeName} other)");
                using (source.StartBlock())
                {
                    source.AppendLine($"return Equals(other as {type.QualifiedName});");
                }
                source.AppendLine();

                source.AppendLine($"{equalsModifier} bool Equals({type.QualifiedName} other)");
                using (source.StartBlock())
                {
                    source.AppendIndent().Append($"return base.Equals(({type.BaseTypeName})other)");
                    foreach (var member in type.Members)
                    {
                        if (member.IgnoreEquality)
                            continue;

                        source.Append($" && {EqualityComparer(member)}.Equals({member.Name}, other.{member.Name})");
                    }
                    source.Append(";").AppendLine();
                }
            }
            else
            {
                source.AppendLine($"{equalsModifier} bool Equals({type.QualifiedName} other)");
                using (source.StartBlock())
                {
                    source.AppendIndent().Append("return (object)other != null");
                    source.Append(" && EqualityContract == other.EqualityContract");
                    foreach (var member in type.Members)
                    {
                        if (member.IgnoreEquality)
                            continue;

                        source.Append($" && {EqualityComparer(member)}.Equals({member.Name}, other.{member.Name})");
                    }
                    source.Append(";").AppendLine();
                }
            }
        }

        private static void EmitClassObjectEquals(CodeBuilder source, TypeModel type)
        {
            source.AppendLine("public override bool Equals(object obj)");
            using (source.StartBlock())
            {
                source.AppendLine($"return Equals(obj as {type.QualifiedName});");
            }
        }

        private static void EmitClassEqualityOperators(CodeBuilder source, TypeModel type)
        {
            source.AppendLine($"public static bool operator ==({type.QualifiedName} left, {type.QualifiedName} right)");
            using (source.StartBlock())
            {
                source.AppendLine("return (object)left == right || left != null && left.Equals(right);");
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
