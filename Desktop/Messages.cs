using Desktop.ViewModels;

namespace Desktop;

public class AddInputFilesMessage;

public class RemoveInputFileMessage(string path)
{
    public string Path { get; } = path;
}

public class EditInputFileMessage(FileItemViewModel file)
{
    public FileItemViewModel File { get; } = file;
}

public class CloseFileSettingsMessage(bool result)
{
    public bool Result { get; } = result;
}