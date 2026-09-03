using System.IO;
using System.Text.Json;
using ClaudeSessionManager.Models;

namespace ClaudeSessionManager.Services;

public static class SessionRepository
{
    public static string ProjectsRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects");

    public static async Task<List<ClaudeSession>> ScanAsync(CancellationToken ct = default)
    {
        return await Task.Run(() => Scan(ct), ct);
    }

    private static List<ClaudeSession> Scan(CancellationToken ct)
    {
        var sessions = new List<ClaudeSession>();
        if (!Directory.Exists(ProjectsRoot)) return sessions;

        var vsCodeCache = VsCodeCacheReader.ReadAll();

        foreach (var projectDir in Directory.EnumerateDirectories(ProjectsRoot))
        {
            ct.ThrowIfCancellationRequested();
            var slug = Path.GetFileName(projectDir);

            foreach (var jsonlPath in Directory.EnumerateFiles(projectDir, "*.jsonl", SearchOption.TopDirectoryOnly))
            {
                ct.ThrowIfCancellationRequested();

                var sessionId = Path.GetFileNameWithoutExtension(jsonlPath);
                if (!Guid.TryParse(sessionId, out _)) continue;

                ClaudeSession session;
                try
                {
                    session = BuildSession(slug, sessionId, jsonlPath, vsCodeCache);
                }
                catch
                {
                    continue; // Skip unreadable/corrupt session files rather than fail the whole scan.
                }

                sessions.Add(session);
            }
        }

        return sessions;
    }

    private static ClaudeSession BuildSession(
        string slug, string sessionId, string jsonlPath, Dictionary<string, VsCodeCacheReader.CacheEntry> vsCodeCache)
    {
        var fileInfo = new FileInfo(jsonlPath);
        var (preview, aiTitle) = ReadPreviewAndLatestAiTitle(jsonlPath);

        vsCodeCache.TryGetValue(sessionId, out var cacheEntry);

        string title;
        string titleSource;
        if (!string.IsNullOrWhiteSpace(cacheEntry?.Label))
        {
            title = cacheEntry!.Label;
            titleSource = "vscode";
        }
        else if (!string.IsNullOrWhiteSpace(aiTitle))
        {
            title = aiTitle!;
            titleSource = "ai-title";
        }
        else if (!string.IsNullOrWhiteSpace(preview))
        {
            title = preview!;
            titleSource = "preview";
        }
        else
        {
            title = "(untitled session)";
            titleSource = "none";
        }

        var lastActivityUtc = cacheEntry?.LastActivityUtc ?? fileInfo.LastWriteTimeUtc;

        var sidecarDir = Path.Combine(Path.GetDirectoryName(jsonlPath)!, sessionId);
        var sidecarPath = Directory.Exists(sidecarDir) ? sidecarDir : null;

        var projectDisplayName = cacheEntry?.WorkingDirectoryPath ?? PrettifySlug(slug);

        return new ClaudeSession
        {
            SessionId = sessionId,
            ProjectSlug = slug,
            ProjectDisplayName = projectDisplayName,
            Title = title,
            Preview = preview ?? "",
            TitleSource = titleSource,
            Archived = cacheEntry?.Archived ?? false,
            ArchivedKnown = cacheEntry is not null,
            LastActivityUtc = lastActivityUtc,
            FileSizeBytes = fileInfo.Length,
            FilePath = jsonlPath,
            SidecarDirectoryPath = sidecarPath,
        };
    }

    private static (string? preview, string? aiTitle) ReadPreviewAndLatestAiTitle(string jsonlPath)
    {
        string? preview = null;
        string? latestAiTitle = null;

        using var stream = new FileStream(jsonlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) continue;

            if (preview is null && line.Contains("\"type\":\"user\""))
            {
                preview = TryExtractUserPreview(line);
            }
            else if (line.Contains("\"type\":\"ai-title\""))
            {
                var t = TryExtractAiTitle(line);
                if (t is not null) latestAiTitle = t;
            }
        }

        return (preview, latestAiTitle);
    }

    private static string? TryExtractAiTitle(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("aiTitle", out var el) && el.ValueKind == JsonValueKind.String)
                return el.GetString();
        }
        catch
        {
            // malformed line, ignore
        }
        return null;
    }

    private static string? TryExtractUserPreview(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (!doc.RootElement.TryGetProperty("message", out var messageEl)) return null;
            if (!messageEl.TryGetProperty("content", out var contentEl)) return null;

            string? text = null;
            if (contentEl.ValueKind == JsonValueKind.String)
            {
                text = contentEl.GetString();
            }
            else if (contentEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in contentEl.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "text"
                        && block.TryGetProperty("text", out var textEl))
                    {
                        text = textEl.GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return text.Length > 140 ? text[..140] + "…" : text;
        }
        catch
        {
            return null;
        }
    }

    private static string PrettifySlug(string slug)
    {
        // Best-effort cosmetic reconstruction of the working directory from its slug
        // (e.g. "c--daimler-Modules-Sapi" -> "C:\daimler\Modules\Sapi"). Lossy when a
        // real folder name contains a hyphen - display only, never used for file paths.
        if (slug.Length >= 3 && slug[1] == '-' && slug[2] == '-')
        {
            var drive = char.ToUpperInvariant(slug[0]);
            var rest = slug[3..].Replace('-', '\\');
            return $"{drive}:\\{rest}";
        }
        return slug;
    }

    public static void Delete(ClaudeSession session)
    {
        if (File.Exists(session.FilePath))
            File.Delete(session.FilePath);

        if (session.SidecarDirectoryPath is not null && Directory.Exists(session.SidecarDirectoryPath))
            Directory.Delete(session.SidecarDirectoryPath, recursive: true);
    }
}
