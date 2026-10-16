namespace Archive.Worker.Capture.Browser.Access;

public sealed class CaptureAccessBlockedException() : CaptureFailedException(
    "Cloudflare hat den Zugriff noch nicht freigegeben. Die automatische Wartephase bzw. die erneute Aufnahme " +
    "ist ausgeschöpft. Für den unbeaufsichtigten Betrieb muss der Seitenbetreiber die Worker-Identität " +
    "zulassen (z. B. über eine akzeptierte Cloudflare-Bot-Registrierung). Es wurde kein Challenge-Snapshot veröffentlicht.");

internal sealed class CaptureChallengeEncounteredException : Exception;
