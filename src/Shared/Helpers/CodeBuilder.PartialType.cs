internal interface IPartialTypeModel
{
    string? Namespace { get; }
    string? ClassBase { get; }
    EquatableList<string> TypeDeclarations { get; }
}

internal partial class CodeBuilder
{
    public IDisposable StartPartialType(IPartialTypeModel typeModel)
    {
        if (!string.IsNullOrEmpty(typeModel.Namespace))
        {
            AppendLine($"namespace {typeModel.Namespace!}");
            AppendLine("{");
            IncreaseIndent();
        }

        for (var i = 0; i < typeModel.TypeDeclarations.Count; i++)
        {
            if (i == typeModel.TypeDeclarations.Count - 1 && !string.IsNullOrEmpty(typeModel.ClassBase))
                AppendLine($"{typeModel.TypeDeclarations[i]} : {typeModel.ClassBase!}");
            else
                AppendLine(typeModel.TypeDeclarations[i]);
            AppendLine("{");
            IncreaseIndent();
        }

        return new CloseBlockDisposable(this, typeModel.TypeDeclarations.Count + (typeModel.Namespace != null ? 1 : 0));
    }

    private readonly struct CloseBlockDisposable(CodeBuilder codeBuilder, int count) : IDisposable
    {
        public void Dispose()
        {
            for (var i = 0; i < count; i++)
            {
                codeBuilder.DecreaseIndent();
                codeBuilder.AppendLine("}");
            }
        }
    }
}
