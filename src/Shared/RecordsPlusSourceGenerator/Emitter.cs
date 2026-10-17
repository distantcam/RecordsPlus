
#if ROSLYN_3
using EmitterContext = Microsoft.CodeAnalysis.GeneratorExecutionContext;
#elif ROSLYN_4
using EmitterContext = Microsoft.CodeAnalysis.SourceProductionContext;
#endif

namespace RecordsPlus;

// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record
// https://github.com/dotnet/roslyn/tree/main/src/Compilers/CSharp/Portable/Symbols/Synthesized/Records

public partial class RecordsPlusSourceGenerator
{
    private static partial class Emitter
    {
        public static void GenerateSource(EmitterContext context, TypeModel type)
        {
            var source = new CodeBuilder()
                .AppendHeader()
                .AppendLine();

            using (source.StartPartialType(type, [$"global::System.IEquatable<{type.QualifiedName}>"]))
            {
                if (type.IsRecord && type.IsStruct)
                    EmitRecordStruct(source, type);
                else if (type.IsStruct)
                    EmitStruct(source, type);
                else if (type.IsRecord)
                    EmitRecord(source, type);
                else
                    EmitClass(source, type);
            }

            context.AddSource($"{type.HintName}.g.cs", source);
        }

        private static void EmitToString(CodeBuilder source, TypeModel type, bool readOnly)
        {
            var ro = readOnly ? "readonly " : "";
            source.AppendLine($"public override {ro}string ToString()");
            using (source.StartBlock())
            {
                source.AppendLine("var builder = new global::System.Text.StringBuilder();");
                source.AppendLine($"builder.Append(\"{type.Name}\");");
                source.AppendLine("builder.Append(\" { \");");
                source.AppendLine("if (PrintMembers(builder))");
                using (source.StartBlock())
                {
                    source.AppendLine("builder.Append(' ');");
                }
                source.AppendLine("builder.Append(\"}\");");
                source.AppendLine("return builder.ToString();");
            }
        }

        private static void EmitPrintMembers(CodeBuilder source, TypeModel type)
        {
            var hasBase = !type.IsStruct && type.BaseTypeHasPrintMember;

            string modifier;
            if (type.IsStruct)
                modifier = type.IsReadOnly ? "private" : "private readonly";
            else
                modifier = type.BaseTypeHasPrintMember ? "protected override"
                    : type.IsSealed ? "private"
                    : "protected virtual";

            source.AppendLine($"{modifier} bool PrintMembers(global::System.Text.StringBuilder builder)");
            using (source.StartBlock())
            {
                if (!type.IsStruct)
                    source.AppendLine("global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();");

                if (!type.Members.Any(static m => !m.IgnoreToString))
                {
                    if (hasBase)
                        source.AppendLine("return base.PrintMembers(builder);");
                    else
                        source.AppendLine("return false;");
                    return;
                }

                if (hasBase)
                {
                    source.AppendLine("if (base.PrintMembers(builder))");
                    using (source.StartBlock())
                    {
                        source.AppendLine("builder.Append(\", \");");
                    }
                }

                var first = true;
                foreach (var member in type.Members)
                {
                    if (member.IgnoreToString)
                        continue;

                    var prefix = first ? "" : ", ";
                    first = false;
                    source.AppendLine($"builder.Append(\"{prefix}{member.Name} = \");");
                    if (member.IsValueType)
                        source.AppendLine($"builder.Append({Self(member)}.ToString());");
                    else
                        source.AppendLine($"builder.Append((object){Self(member)});");
                }

                source.AppendLine("return true;");
            }
        }

        private static void EmitGetHashCode(CodeBuilder source, TypeModel type, bool readOnly)
        {
            var ro = readOnly ? "readonly " : "";
            source.AppendLine($"public override {ro}int GetHashCode()");
            using (source.StartBlock())
            {
                source.AppendLine("unchecked");
                using (source.StartBlock())
                {
                    if (type.IsStruct)
                    {
                        source.AppendLine("int hashCode = 0;");
                    }
                    else
                    {
                        source.AppendLine("int hashCode = EqualityContract.GetHashCode();");
                        if (type.BaseTypeHasEqualityContract)
                            source.AppendLine("hashCode = hashCode * -1521134295 + base.GetHashCode();");
                    }

                    foreach (var member in type.Members)
                    {
                        if (member.IgnoreEquality)
                            continue;

                        source.AppendLine($"hashCode = hashCode * -1521134295 + {EqualityComparer(member)}.GetHashCode({Self(member)});");
                    }

                    source.AppendLine("return hashCode;");
                }
            }
        }

        private static string EqualityComparer(MemberModel member)
            => $"global::System.Collections.Generic.EqualityComparer<{member.TypeName}>.Default";

        // `builder`, `other` and `hashCode` are all legal member names, and a member can be a
        // keyword, so qualify and escape every reference instead of letting it bind to the
        // generated local or parameter of the same name.
        private static string Self(MemberModel member)
            => $"this.{member.Name.EscapeKeywordIdentifier()}";

        private static string Other(MemberModel member)
            => $"other.{member.Name.EscapeKeywordIdentifier()}";
    }
}
