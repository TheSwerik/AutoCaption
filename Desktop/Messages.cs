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

public class CancelMessage;

public class StartMessage(bool waitUntilReady)
{
    public bool WaitUntilReady { get; } = waitUntilReady;
}