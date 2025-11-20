using Desktop.Results;

namespace Desktop.Errors;

public record FfmpegError(int ExitCode) : Error;