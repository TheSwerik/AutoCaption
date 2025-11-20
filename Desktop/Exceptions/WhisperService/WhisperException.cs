namespace Desktop.Exceptions.WhisperService;

public class WhisperException(int exitCode) : WhisperServiceException($"Whisper exited with exitCode {exitCode}.\nView the logs for more information.");