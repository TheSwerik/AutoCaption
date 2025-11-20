namespace Desktop.Exceptions.WhisperService;

public class FfmpegException(int exitCode) : WhisperServiceException($"ffmpeg exited with exitCode {exitCode}.\nView the logs for more information.");