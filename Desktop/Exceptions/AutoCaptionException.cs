using System;

namespace Desktop.Exceptions;

public class AutoCaptionException(string? message = null, Exception? innerException = null) : Exception(message, innerException);