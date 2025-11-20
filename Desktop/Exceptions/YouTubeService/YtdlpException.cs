namespace Desktop.Exceptions.YouTubeService;

public class YtdlpException(int exitCode) : YouTubeServiceException($"yt-dlp exited with ExitCode: {exitCode}.\nView the logs for more information.");