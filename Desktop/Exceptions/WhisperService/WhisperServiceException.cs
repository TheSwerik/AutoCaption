using System;

namespace Desktop.Exceptions.WhisperService;

public class WhisperServiceException(string? message = null, Exception? innerException = null) : AutoCaptionException(message, innerException);