using System;

namespace Desktop.Exceptions;

public class AutoCaptionException : Exception
{
    public AutoCaptionException()
    {
    }

    public AutoCaptionException(string? message) : base(message)
    {
    }

    public AutoCaptionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}