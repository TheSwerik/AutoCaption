using System;

namespace Desktop.Exceptions.YouTubeService;

public class YouTubeServiceException : AutoCaptionException
{
    public YouTubeServiceException()
    {
    }

    public YouTubeServiceException(string? message) : base(message)
    {
    }

    public YouTubeServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}