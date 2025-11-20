namespace Desktop.Exceptions.YouTubeService;

public class QuotaExceededException<TValue>(TValue? partialValue) : YouTubeServiceException
{
    public TValue? PartialValue { get; } = partialValue;
}