using Desktop.Results;

namespace Desktop.Errors;

public record YouTubeServiceError(string? Message = null) : Error;

public record QuotaExceededError() : YouTubeServiceError();

public record NoVisibilitySelectedError() : YouTubeServiceError();

public record AuthorizationError(string Message) : YouTubeServiceError(Message);