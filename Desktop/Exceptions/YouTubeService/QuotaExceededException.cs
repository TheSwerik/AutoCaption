namespace Desktop.Exceptions.YouTubeService;

public class QuotaExceededException<TValue>(TValue? partialValue = default) : QuotaExceededException
{
    public TValue? PartialValue { get; } = partialValue;
}

public abstract class QuotaExceededException : YouTubeServiceException;