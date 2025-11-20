using System;

namespace Desktop.Exceptions.WhisperService;

public class WhisperServiceException : AutoCaptionException
{
    public WhisperServiceException()
    {
    }

    public WhisperServiceException(string? message) : base(message)
    {
    }

    public WhisperServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}