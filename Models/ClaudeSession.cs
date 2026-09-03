using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClaudeSessionManager.Models;

public sealed class ClaudeSession : INotifyPropertyChanged
{
    public required string SessionId { get; init; }
    public required string ProjectSlug { get; init; }
    public required string ProjectDisplayName { get; init; }
    public required string Title { get; init; }
    public required string Preview { get; init; }
    public required string TitleSource { get; init; }
    public required bool Archived { get; init; }
    public required bool ArchivedKnown { get; init; }
    public required DateTime LastActivityUtc { get; init; }
    public required long FileSizeBytes { get; init; }
    public required string FilePath { get; init; }
    public required string? SidecarDirectoryPath { get; init; }

    public string GroupName => Archived ? "Archived sessions" : "Ungrouped";

    public string RelativeTime => RelativeTimeFormatter.Format(LastActivityUtc);

    public string SizeDisplay => FileSizeBytes < 1024 * 1024
        ? $"{FileSizeBytes / 1024.0:0.#} KB"
        : $"{FileSizeBytes / 1024.0 / 1024.0:0.#} MB";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
