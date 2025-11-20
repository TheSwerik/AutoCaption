using Desktop.Results;

namespace Desktop.Errors;

public record WhisperError(int ExitCode) : Error;

public record YtdlpError(int ExitCode) : WhisperError(ExitCode);

public record FfmpegError(int ExitCode) : WhisperError(ExitCode);