using Desktop.Exceptions.YouTubeService;

namespace Desktop.Exceptions;

public class YtdlpException(int exitCode) : YouTubeServiceException($"yt-dlp exited with ExitCode: {exitCode}");