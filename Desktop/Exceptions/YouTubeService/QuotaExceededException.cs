using System;

namespace Desktop.Exceptions.YouTubeService;

public class QuotaExceededException<TValue>(Exception innerException, TValue? partialValue = default) : QuotaExceededException(innerException)
{
    public TValue? PartialValue { get; } = partialValue;
}

public abstract class QuotaExceededException(Exception innerException) : YouTubeServiceException(innerException: innerException);