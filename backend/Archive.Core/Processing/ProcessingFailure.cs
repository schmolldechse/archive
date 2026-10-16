using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Archive.Core.Processing;

public sealed class ProcessingUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
public sealed class ProcessingInconsistentException(string message) : Exception(message);

public static class ProcessingFailure
{
    public static bool IsUnavailable(Exception exception) => exception is
        ProcessingUnavailableException or ProcessingInconsistentException or TimeoutException
        or NpgsqlException { IsTransient: true }
        or InvalidOperationException { InnerException: NpgsqlException { IsTransient: true } }
        || exception is DbUpdateException { InnerException: { } inner } && IsUnavailable(inner);
}
