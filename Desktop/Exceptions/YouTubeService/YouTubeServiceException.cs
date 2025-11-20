using System;

namespace Desktop.Exceptions.YouTubeService;

public class YouTubeServiceException(string? message = null, Exception? innerException = null) : AutoCaptionException(message, innerException);