using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ClaudeSessionManager.Models;
using ClaudeSessionManager.Services;

namespace ClaudeSessionManager.Views;

public partial class SessionsPage : Page
{
    private ObservableCollection<ClaudeSession> _allSessions = new();
    private ICollectionView? _view;
    private CancellationTokenSource? _cts;

    public SessionsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadSessionsAsync();
    }

    private async Task LoadSessionsAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        StatusText.Text = $"Scanning {SessionRepository.ProjectsRoot} ...";
        DeleteSelectedButton.IsEnabled = false;

        try
        {
            var sessions = await SessionRepository.ScanAsync(token);
            if (token.IsCancellationRequested) return;

            _allSessions = new ObservableCollection<ClaudeSession>(sessions);

            var view = CollectionViewSource.GetDefaultView(_allSessions);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(ClaudeSession.Archived), ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription(nameof(ClaudeSession.LastActivityUtc), ListSortDirection.Descending));
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ClaudeSession.GroupName)));
            view.Filter = FilterPredicate;

            _view = view;
            SessionsListBox.ItemsSource = view;

            UpdateStatus();
            EmptyStateText.Visibility = _allSessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to scan sessions: {ex.Message}";
        }
        finally
        {
            DeleteSelectedButton.IsEnabled = true;
        }
    }

    private bool FilterPredicate(object obj)
    {
        if (obj is not ClaudeSession s) return false;
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return true;

        return s.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
               || s.ProjectDisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
               || s.SessionId.Contains(query, StringComparison.OrdinalIgnoreCase)
               || s.Preview.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateStatus()
    {
        var archived = _allSessions.Count(s => s.Archived);
        var ungrouped = _allSessions.Count - archived;
        StatusText.Text = $"{_allSessions.Count} sessions  •  {archived} archived  •  {ungrouped} ungrouped  •  last refreshed {DateTime.Now:HH:mm:ss}";
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        SelectAllCheckBox.IsChecked = false;
        await LoadSessionsAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _view?.Refresh();
    }

    private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_view is null) return;
        var check = SelectAllCheckBox.IsChecked == true;
        foreach (var item in _view.Cast<ClaudeSession>())
            item.IsSelected = check;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not ClaudeSession session) return;
        DeleteSessions(new List<ClaudeSession> { session });
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _allSessions.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(Window.GetWindow(this), "No sessions are selected.", "Delete Selected",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DeleteSessions(selected);
    }

    private void DeleteSessions(IReadOnlyList<ClaudeSession> targets)
    {
        string message;
        if (targets.Count == 1)
        {
            var s = targets[0];
            message = $"Delete this session permanently?\n\n\"{s.Title}\"\n{s.ProjectDisplayName}  •  {s.RelativeTime} ago  •  {s.SizeDisplay}\n\nThis cannot be undone.";
        }
        else
        {
            var preview = string.Join("\n", targets.Take(8).Select(s => $"  • {s.Title}"));
            var more = targets.Count > 8 ? $"\n  … and {targets.Count - 8} more" : "";
            message = $"Delete {targets.Count} sessions permanently?\n\n{preview}{more}\n\nThis cannot be undone.";
        }

        var owner = Window.GetWindow(this);
        var result = MessageBox.Show(owner, message, "Confirm delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (result != MessageBoxResult.Yes) return;

        var failures = new List<string>();
        foreach (var session in targets)
        {
            try
            {
                SessionRepository.Delete(session);
                _allSessions.Remove(session);
            }
            catch (Exception ex)
            {
                failures.Add($"{session.Title}: {ex.Message}");
            }
        }

        UpdateStatus();
        EmptyStateText.Visibility = _allSessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (failures.Count > 0)
        {
            MessageBox.Show(owner, "Some sessions could not be deleted:\n\n" + string.Join("\n", failures),
                "Delete errors", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
