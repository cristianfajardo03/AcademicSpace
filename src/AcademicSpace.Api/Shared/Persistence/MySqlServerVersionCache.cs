using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace AcademicSpace.Api.Shared.Persistence;

public static class MySqlServerVersionCache
{
    private static readonly ConcurrentDictionary<string, ServerVersion> Cache = new();

    public static ServerVersion Get(string connectionString) =>
        Cache.GetOrAdd(connectionString, ServerVersion.AutoDetect);
}
