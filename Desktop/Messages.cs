using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Desktop;

public class AddInputFilesMessage;

public class RemoveInputFileMessage(string path)
{
    public string Path { get; } = path;
}