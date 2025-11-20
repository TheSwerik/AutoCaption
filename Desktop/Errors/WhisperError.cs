using Desktop.Results;

namespace Desktop.Errors;

public record WhisperError(int ExitCode) : Error;