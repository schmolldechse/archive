namespace Archive.Worker.Capture;

public sealed class CaptureDiscardedException(string message) : Exception(message);

public class CaptureFailedException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class CaptureCancelledException : Exception;
