namespace ClaudeSessionManager.Models;

public static class RelativeTimeFormatter
{
    public static string Format(DateTime utcTimestamp)
    {
        var delta = DateTime.UtcNow - utcTimestamp;
        if (delta < TimeSpan.Zero) delta = TimeSpan.Zero;

        if (delta.TotalMinutes < 1) return "now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h";
        if (delta.TotalDays < 30) return $"{(int)delta.TotalDays}d";
        if (delta.TotalDays < 365) return $"{(int)(delta.TotalDays / 30)}mo";
        return $"{(int)(delta.TotalDays / 365)}y";
    }
}
