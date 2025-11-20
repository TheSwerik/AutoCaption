using System;

namespace Desktop.Exceptions.YouTubeService;

public class AuthorizationException(string? message, Exception? innerException) : YouTubeServiceException(message, innerException);