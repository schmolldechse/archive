namespace Archive.Api.Exceptions;

public sealed class InvalidSessionException() : Exception("The current session could not be validated.");
