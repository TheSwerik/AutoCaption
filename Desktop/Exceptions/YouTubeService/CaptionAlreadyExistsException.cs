using System;

namespace Desktop.Exceptions.YouTubeService;

public class CaptionAlreadyExistsException<TValue>(Exception innerException, TValue? partialValue = default) : QuotaExceededException(innerException)
{
    public TValue? PartialValue { get; } = partialValue;
}

public abstract class CaptionAlreadyExistsException(Exception innerException) : YouTubeServiceException(innerException: innerException);