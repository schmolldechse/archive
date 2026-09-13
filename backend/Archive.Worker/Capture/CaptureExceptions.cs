namespace Archive.Worker.Capture;

public sealed class CaptureDiscardedException(string message) : Exception(message);

public class CaptureFailedException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class CaptureAccessBlockedException() : CaptureFailedException(
    "Cloudflare hat den Zugriff noch nicht freigegeben. Die automatische Wartephase bzw. die erneute Aufnahme " +
    "ist ausgeschöpft. Für den unbeaufsichtigten Betrieb muss der Seitenbetreiber die Worker-Identität " +
    "zulassen (z. B. über eine akzeptierte Cloudflare-Bot-Registrierung). Es wurde kein Challenge-Snapshot veröffentlicht.");

public sealed class CaptureCancelledException : Exception;
