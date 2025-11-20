namespace Desktop.Exceptions.WhisperService;

public class WhisperException(int exitCode) : WhisperServiceException($"Whisper exited with exitCode {exitCode}");