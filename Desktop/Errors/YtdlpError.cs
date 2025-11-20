using Desktop.Results;

namespace Desktop.Errors;

public record YtdlpError(int ExitCode) : Error;