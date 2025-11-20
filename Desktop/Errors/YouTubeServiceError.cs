using Desktop.Results;

namespace Desktop.Errors;

public record YouTubeServiceError(string message) : Error;