using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace ClaudeSessionManager.Services;

/// <summary>
/// Reads per-session title/archived/last-activity data that the "Claude Code" VS Code
/// extension caches in each workspace's state.vscdb (key: agentSessions.model.cache),
/// so the standalone manager can show the same names and archived state VS Code shows.
/// This is opportunistic/best-effort: the cache is only present for sessions that were
/// ever opened from VS Code, and only reflects whatever VS Code last flushed to disk.
/// </summary>
public static class VsCodeCacheReader
{
    public sealed record CacheEntry(string Label, bool Archived, DateTime? LastActivityUtc, string? WorkingDirectoryPath);

    private static readonly Regex GuidRegex = new(
        @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.Compiled);

    public static Dictionary<string, CacheEntry> ReadAll()
    {
        var result = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

        var workspaceStorageDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Code", "User", "workspaceStorage");

        if (!Directory.Exists(workspaceStorageDir))
            return result;

        foreach (var dir in Directory.EnumerateDirectories(workspaceStorageDir))
        {
            var dbPath = Path.Combine(dir, "state.vscdb");
            if (!File.Exists(dbPath)) continue;

            try
            {
                MergeFromDatabase(dbPath, result);
            }
            catch
            {
                // Locked, corrupt, or unreadable - skip this workspace, it's best-effort data.
            }
        }

        return result;
    }

    private static void MergeFromDatabase(string dbPath, Dictionary<string, CacheEntry> result)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly,
        };

        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM ItemTable WHERE key = 'agentSessions.model.cache'";
        var raw = command.ExecuteScalar() as string;
        if (string.IsNullOrWhiteSpace(raw)) return;

        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var sessionId = ExtractSessionId(item);
            if (sessionId is null) continue;

            var label = item.TryGetProperty("label", out var labelEl) && labelEl.ValueKind == JsonValueKind.String
                ? labelEl.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(label)) continue;

            var archived = item.TryGetProperty("archived", out var archivedEl)
                           && archivedEl.ValueKind is JsonValueKind.True or JsonValueKind.False
                           && archivedEl.GetBoolean();

            DateTime? lastActivity = null;
            if (item.TryGetProperty("timing", out var timingEl) && timingEl.ValueKind == JsonValueKind.Object)
            {
                lastActivity = ReadEpochMs(timingEl, "lastRequestEnded")
                               ?? ReadEpochMs(timingEl, "lastRequestStarted")
                               ?? ReadEpochMs(timingEl, "created");
            }

            string? workingDir = null;
            if (item.TryGetProperty("metadata", out var metaEl) && metaEl.ValueKind == JsonValueKind.Object
                && metaEl.TryGetProperty("workingDirectoryPath", out var wdEl) && wdEl.ValueKind == JsonValueKind.String)
            {
                workingDir = wdEl.GetString();
            }

            var entry = new CacheEntry(label!, archived, lastActivity, workingDir);

            if (!result.TryGetValue(sessionId, out var existing) ||
                (entry.LastActivityUtc ?? DateTime.MinValue) > (existing.LastActivityUtc ?? DateTime.MinValue))
            {
                result[sessionId] = entry;
            }
        }
    }

    private static string? ExtractSessionId(JsonElement item)
    {
        foreach (var propName in new[] { "resource", "id" })
        {
            if (item.TryGetProperty(propName, out var propEl) && propEl.ValueKind == JsonValueKind.String)
            {
                var match = GuidRegex.Match(propEl.GetString() ?? "");
                if (match.Success) return match.Value;
            }
        }
        return null;
    }

    private static DateTime? ReadEpochMs(JsonElement timing, string propName)
    {
        if (timing.TryGetProperty(propName, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var ms))
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        return null;
    }
}
