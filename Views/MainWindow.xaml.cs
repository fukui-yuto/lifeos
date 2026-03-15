using LifeOS.Models;
using LifeOS.Services;
using LifeOS.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace LifeOS.Views;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    private static readonly IntPtr HWND_TOPMOST    = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST  = new(-2);
    private const uint SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001, SWP_SHOWWINDOW = 0x0040;

    private readonly MainViewModel _vm;
    private readonly List<StickyNoteWindow> _noteWindows = new();

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        RefreshNoteCards();
        RefreshTasks();
        RefreshGoals();
        RefreshLogs();

        // Xボタンで閉じる代わりに非表示にする
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.Tasks))
            {
                RefreshTasks();
                RefreshNoteCards();
            }
        };
    }

    // --- ナビゲーション ---
    private void Nav_Notes(object sender, RoutedEventArgs e) => ShowView("notes");
    private void Nav_Tasks(object sender, RoutedEventArgs e) => ShowView("tasks");
    private void Nav_Goals(object sender, RoutedEventArgs e) => ShowView("goals");
    private void Nav_Logs(object sender, RoutedEventArgs e) => ShowView("logs");

    private void ShowView(string view)
    {
        if (NotesView == null) return;

        NotesView.Visibility = view == "notes" ? Visibility.Visible : Visibility.Collapsed;
        TasksView.Visibility = view == "tasks" ? Visibility.Visible : Visibility.Collapsed;
        GoalsView.Visibility = view == "goals" ? Visibility.Visible : Visibility.Collapsed;
        LogsView.Visibility = view == "logs" ? Visibility.Visible : Visibility.Collapsed;

        if (view == "logs") RefreshLogs();
    }

    // --- 付箋 ---
    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        _vm.AddNoteCommand.Execute(null);
        var note = _vm.Notes.Last();
        OpenStickyNoteWindow(note);
        RefreshNoteCards();
    }

    private void RefreshNoteCards()
    {
        NotesPanel.Children.Clear();
        foreach (var note in _vm.Notes)
        {
            var card = CreateNoteCard(note);
            NotesPanel.Children.Add(card);
        }
    }

    private Border CreateNoteCard(StickyNote note)
    {
        var color = (MediaColor)MediaColorConverter.ConvertFromString(note.Color);

        // カード外枠
        var card = new Border
        {
            Width = 220,
            MinHeight = 140,
            Margin = new Thickness(10),
            CornerRadius = new CornerRadius(16),
            Background = new MediaSolidColorBrush(color),
            Cursor = System.Windows.Input.Cursors.Hand,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 24,
                Opacity = 0.15,
                ShadowDepth = 6,
                Direction = 270
            }
        };

        var outer = new StackPanel();

        // ヘッダー（タイトル＋削除ボタン）
        var header = new Grid { Margin = new Thickness(14, 10, 8, 0) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = note.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 13.5,
            Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(30, 30, 30)),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleText, 0);

        var deleteBtn = new System.Windows.Controls.Button
        {
            Content = "✕",
            FontSize = 11,
            Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(140, 140, 140)),
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "削除",
            Width = 26,
            Height = 26,
            Padding = new Thickness(0)
        };
        Grid.SetColumn(deleteBtn, 1);
        deleteBtn.Click += (_, _) =>
        {
            var result = System.Windows.MessageBox.Show("このノートを削除しますか？", "確認",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var openWin = _noteWindows.FirstOrDefault(w =>
                    (w.DataContext as StickyNoteViewModel)?.Note.Id == note.Id);
                openWin?.Close();
                _vm.DeleteNote(note);
                RefreshNoteCards();
                RefreshTasks();
            }
        };
        header.Children.Add(titleText);
        header.Children.Add(deleteBtn);

        // 区切り線
        var sep = new System.Windows.Shapes.Rectangle
        {
            Height = 1,
            Fill = new MediaSolidColorBrush(MediaColor.FromArgb(20, 0, 0, 0)),
            Margin = new Thickness(14, 8, 14, 0)
        };

        // 1. ヘッダー（タイトル＋削除ボタン）
        outer.Children.Add(header);

        // 2. 区切り線
        outer.Children.Add(sep);

        // 3. 本文（最大3行）
        if (!string.IsNullOrWhiteSpace(note.Content))
        {
            outer.Children.Add(new TextBlock
            {
                Text = note.Content,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(55, 55, 55)),
                MaxHeight = 52,
                Margin = new Thickness(14, 6, 14, 4),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        // 4. タスク一覧（最大3件）
        if (note.Tasks.Count > 0)
        {
            var taskStack = new StackPanel { Margin = new Thickness(14, 4, 14, 0) };
            foreach (var t in note.Tasks.Take(3))
            {
                var row = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin = new Thickness(0, 1, 0, 1)
                };
                var fg = new MediaSolidColorBrush(t.IsCompleted
                    ? MediaColor.FromArgb(100, 0, 0, 0)
                    : MediaColor.FromRgb(55, 55, 55));
                row.Children.Add(new TextBlock { Text = t.IsCompleted ? "☑ " : "☐ ", FontSize = 11.5, Foreground = fg });
                row.Children.Add(new TextBlock
                {
                    Text = t.Title, FontSize = 11.5, Foreground = fg,
                    TextDecorations = t.IsCompleted ? TextDecorations.Strikethrough : null
                });
                taskStack.Children.Add(row);
            }
            if (note.Tasks.Count > 3)
                taskStack.Children.Add(new TextBlock
                {
                    Text = $"… 他{note.Tasks.Count - 3}件",
                    FontSize = 11, FontStyle = FontStyles.Italic,
                    Foreground = new MediaSolidColorBrush(MediaColor.FromArgb(100, 0, 0, 0)),
                    Margin = new Thickness(0, 2, 0, 0)
                });
            outer.Children.Add(taskStack);
        }

        // 5. 空の場合のヒント
        if (string.IsNullOrWhiteSpace(note.Content) && note.Tasks.Count == 0)
        {
            outer.Children.Add(new TextBlock
            {
                Text = "ダブルクリックで開く…",
                FontSize = 12, FontStyle = FontStyles.Italic,
                Foreground = new MediaSolidColorBrush(MediaColor.FromArgb(80, 0, 0, 0)),
                Margin = new Thickness(14, 6, 14, 0)
            });
        }

        outer.Children.Add(new Border { Height = 12 });
        card.Child = outer;

        card.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) OpenStickyNoteWindow(note);
        };

        card.MouseRightButtonUp += (_, e) =>
        {
            var menu = new System.Windows.Controls.ContextMenu();
            var colorNames = new[]
            {
                "ネオンイエロー", "シアン", "ライム", "ネオンピンク", "バイオレット", "オレンジ",
                "ローズ", "ラベンダー", "スカイブルー", "セージ",
                "パステルレッド", "パステルオレンジ", "ミント", "パステルブルー", "パープル",
                "アークティック", "シルバー", "シトロン", "ホワイト", "クリーム",
            };
            for (int i = 0; i < StickyNoteWindow.NoteColors.Length; i++)
            {
                var colorHex = StickyNoteWindow.NoteColors[i];
                var item = new System.Windows.Controls.MenuItem
                {
                    Header = colorNames[i],
                    Icon = new System.Windows.Shapes.Rectangle
                    {
                        Width = 14, Height = 14,
                        Fill = new MediaSolidColorBrush(
                            (MediaColor)MediaColorConverter.ConvertFromString(colorHex)),
                        RadiusX = 3, RadiusY = 3
                    }
                };
                item.Click += (_, _) =>
                {
                    note.Color = colorHex;
                    _vm.UpdateNote(note);
                    RefreshNoteCards();
                    // 開いているノートウィンドウの色も更新
                    var openVm = _noteWindows
                        .FirstOrDefault(w => (w.DataContext as StickyNoteViewModel)?.Note.Id == note.Id)
                        ?.DataContext as StickyNoteViewModel;
                    if (openVm != null) openVm.Color = colorHex;
                };
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
            e.Handled = true;
        };

        return card;
    }

    public void BringNotesToFront()
    {
        foreach (Window win in System.Windows.Application.Current.Windows)
        {
            if (win is not StickyNoteWindow) continue;
            win.Show();
            var hwnd = new WindowInteropHelper(win).Handle;
            SetWindowPos(hwnd, HWND_TOPMOST,   0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
    }

    private void OpenStickyNoteWindow(StickyNote note)
    {
        var existing = _noteWindows.FirstOrDefault(w => w.IsLoaded && (w.DataContext as StickyNoteViewModel)?.Note.Id == note.Id);
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        var noteVm = new StickyNoteViewModel(note, _vm, _vm.Db);
        var win = new StickyNoteWindow(noteVm, () =>
        {
            this.Show();
            this.Activate();
        });
        win.Closed += (_, _) => RefreshNoteCards();
        _noteWindows.Add(win);
        win.Show();
    }

    // 開いているノートウィンドウのタスク一覧を再読み込み
    private void RefreshOpenNoteWindows()
    {
        foreach (var win in _noteWindows)
        {
            if (win.DataContext is StickyNoteViewModel noteVm)
                noteVm.ReloadTasks();
        }
    }

    // --- タスク ---
    private void RefreshTasks()
    {
        TaskGroupPanel.Items.Clear();

        // ノートに紐付いたタスクをノートごとにグループ化
        var noteGroups = _vm.Notes
            .Select(note => new
            {
                Note = note,
                Tasks = _vm.Tasks.Where(t => t.StickyNoteId == note.Id).ToList()
            })
            .Where(g => g.Tasks.Count > 0)
            .ToList();

        // 独立タスク（ノート未紐付け）
        var standalone = _vm.Tasks.Where(t => t.StickyNoteId == null).ToList();

        // ノートごとのグループを描画
        foreach (var group in noteGroups)
        {
            TaskGroupPanel.Items.Add(BuildTaskGroup(group.Note.Title, group.Tasks, group.Note));
        }

        // 独立タスク
        if (standalone.Count > 0 || true)
        {
            TaskGroupPanel.Items.Add(BuildTaskGroup("独立したタスク", standalone, null));
        }
    }

    private Border BuildTaskGroup(string groupTitle, List<TaskItem> tasks, StickyNote? note)
    {
        var groupBorder = new Border
        {
            Background = System.Windows.Media.Brushes.White,
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(0, 0, 0, 8),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10, Opacity = 0.07, ShadowDepth = 2, Direction = 270
            }
        };

        var stack = new StackPanel();

        // グループヘッダー
        var header = new Grid { Margin = new Thickness(16, 12, 8, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new TextBlock
        {
            Text = groupTitle,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(40, 40, 40)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 0);

        // タスク追加ボタン
        var addBtn = new System.Windows.Controls.Button
        {
            Content = "＋ 追加",
            FontSize = 12,
            Background = new MediaSolidColorBrush(MediaColor.FromRgb(91, 107, 245)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 4, 10, 4),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        Grid.SetColumn(addBtn, 1);
        addBtn.Click += (_, _) => AddTaskToGroup(note);

        header.Children.Add(title);
        header.Children.Add(addBtn);
        stack.Children.Add(header);

        // 区切り
        stack.Children.Add(new System.Windows.Shapes.Rectangle
        {
            Height = 1,
            Fill = new MediaSolidColorBrush(MediaColor.FromArgb(15, 0, 0, 0)),
            Margin = new Thickness(16, 0, 16, 8)
        });

        // タスク行
        foreach (var task in tasks)
            stack.Children.Add(BuildTaskRow(task));

        if (tasks.Count == 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "タスクはありません",
                FontSize = 12,
                Foreground = new MediaSolidColorBrush(MediaColor.FromArgb(120, 0, 0, 0)),
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(16, 4, 16, 4)
            });
        }

        groupBorder.Child = stack;
        return groupBorder;
    }

    private UIElement BuildTaskRow(TaskItem task)
    {
        var row = new Grid { Margin = new Thickness(16, 3, 8, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var chk = new System.Windows.Controls.CheckBox
        {
            IsChecked = task.IsCompleted,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        chk.Click += (_, _) => { task.IsCompleted = chk.IsChecked == true; _vm.UpdateTask(task); RefreshOpenNoteWindows(); RefreshTasks(); };
        Grid.SetColumn(chk, 0);

        var lbl = new System.Windows.Controls.TextBox
        {
            Text = task.Title,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null,
            Foreground = task.IsCompleted
                ? new MediaSolidColorBrush(MediaColor.FromArgb(120, 0, 0, 0))
                : new MediaSolidColorBrush(MediaColor.FromRgb(40, 40, 40)),
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            IsReadOnly = false,
            Cursor = System.Windows.Input.Cursors.IBeam
        };
        lbl.LostFocus += (_, _) =>
        {
            var newTitle = lbl.Text.Trim();
            if (!string.IsNullOrEmpty(newTitle) && newTitle != task.Title)
            {
                task.Title = newTitle;
                _vm.UpdateTask(task);
                RefreshOpenNoteWindows();
                RefreshNoteCards();
            }
        };
        lbl.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                System.Windows.Input.Keyboard.ClearFocus();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                lbl.Text = task.Title;
                System.Windows.Input.Keyboard.ClearFocus();
                e.Handled = true;
            }
        };
        Grid.SetColumn(lbl, 1);

        var del = new System.Windows.Controls.Button
        {
            Content = "✕",
            FontSize = 11,
            Width = 24, Height = 24,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = new MediaSolidColorBrush(MediaColor.FromArgb(140, 0, 0, 0)),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        del.Click += (_, _) => { _vm.DeleteTask(task); RefreshOpenNoteWindows(); RefreshTasks(); };
        Grid.SetColumn(del, 2);

        row.Children.Add(chk);
        row.Children.Add(lbl);
        row.Children.Add(del);
        return row;
    }

    private void AddTaskToGroup(StickyNote? note)
    {
        var task = new TaskItem { Title = "新しいタスク", StickyNoteId = note?.Id };
        var dialog = new TaskEditDialog(task) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            task = _vm.Db.AddTask(task);
            _vm.ReloadTasks();
            RefreshOpenNoteWindows();
            RefreshTasks();
        }
    }

    // --- 学習目標 ---
    private void AddGoal_Click(object sender, RoutedEventArgs e)
    {
        var goal = new LearningGoal { Title = "新しい目標" };
        var dialog = new GoalEditDialog(goal, _vm.Notes) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _vm.Db.AddGoal(goal);
            _vm.Db.UpdateGoalNotes(goal.Id, goal.LinkedNotes.Select(n => n.Id).ToList());
            _vm.Goals.Add(goal);
            RefreshGoals();
        }
    }

    private void RefreshGoals()
    {
        GoalsPanel.Children.Clear();
        foreach (var goal in _vm.Goals)
            GoalsPanel.Children.Add(BuildGoalCard(goal));
    }

    private UIElement BuildGoalCard(LearningGoal goal)
    {
        var card = new Border
        {
            Background = System.Windows.Media.Brushes.White,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(16),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8, Opacity = 0.06, ShadowDepth = 2, Direction = 270
            }
        };

        var outer = new Grid();
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // 左：情報
        var info = new StackPanel();
        info.Children.Add(new TextBlock
        {
            Text = goal.Title,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(30, 30, 30))
        });
        if (!string.IsNullOrWhiteSpace(goal.Description))
            info.Children.Add(new TextBlock
            {
                Text = goal.Description,
                FontSize = 12,
                Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                Margin = new Thickness(0, 4, 0, 8),
                TextWrapping = TextWrapping.Wrap
            });

        if (goal.TargetDate.HasValue)
            info.Children.Add(new TextBlock
            {
                Text = $"期限: {goal.TargetDate.Value:yyyy/MM/dd}",
                FontSize = 11,
                Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(130, 130, 130)),
                Margin = new Thickness(0, 4, 0, 0)
            });

        if (goal.LinkedNotes.Count > 0)
            info.Children.Add(new TextBlock
            {
                Text = "📋 " + string.Join(", ", goal.LinkedNotes.Select(n => n.Title)),
                FontSize = 11,
                Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(91, 107, 245)),
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        Grid.SetColumn(info, 0);

        // 右：ボタン
        var btns = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12, 0, 0, 0)
        };
        var editBtn = new System.Windows.Controls.Button
        {
            Content = "編集",
            FontSize = 12,
            Background = new MediaSolidColorBrush(MediaColor.FromRgb(91, 107, 245)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        editBtn.Click += (_, _) =>
        {
            var dialog = new GoalEditDialog(goal, _vm.Notes) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _vm.UpdateGoal(goal);
                _vm.Db.UpdateGoalNotes(goal.Id, goal.LinkedNotes.Select(n => n.Id).ToList());
                RefreshGoals();
            }
        };
        var delBtn = new System.Windows.Controls.Button
        {
            Content = "削除",
            FontSize = 12,
            Background = new MediaSolidColorBrush(MediaColor.FromRgb(224, 85, 85)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 4, 10, 4),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        delBtn.Click += (_, _) =>
        {
            var r = System.Windows.MessageBox.Show($"「{goal.Title}」を削除しますか？",
                "確認", System.Windows.MessageBoxButton.YesNo);
            if (r == System.Windows.MessageBoxResult.Yes)
            {
                _vm.DeleteGoal(goal);
                RefreshGoals();
            }
        };
        btns.Children.Add(editBtn);
        btns.Children.Add(delBtn);
        Grid.SetColumn(btns, 1);

        outer.Children.Add(info);
        outer.Children.Add(btns);
        card.Child = outer;
        return card;
    }

    // --- 操作ログ ---
    private void RefreshLogs()
    {
        LogsPanel.Children.Clear();
        var logs = _vm.Db.GetOperationLogs();
        foreach (var log in logs)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var time = new TextBlock
            {
                Text = log.OccurredAt.ToString("yyyy/MM/dd HH:mm"),
                FontSize = 12,
                Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(130, 130, 130)),
                VerticalAlignment = VerticalAlignment.Center
            };
            var action = new TextBlock
            {
                Text = log.Action,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(91, 107, 245)),
                VerticalAlignment = VerticalAlignment.Center
            };
            var detail = new TextBlock
            {
                Text = log.Detail,
                FontSize = 12,
                Foreground = new MediaSolidColorBrush(MediaColor.FromRgb(50, 50, 50)),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(time, 0);
            Grid.SetColumn(action, 1);
            Grid.SetColumn(detail, 2);
            row.Children.Add(time);
            row.Children.Add(action);
            row.Children.Add(detail);

            var card = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 4),
                Child = row
            };
            LogsPanel.Children.Add(card);
        }

        if (logs.Count == 0)
            LogsPanel.Children.Add(new TextBlock
            {
                Text = "操作ログはまだありません",
                FontSize = 13,
                FontStyle = FontStyles.Italic,
                Foreground = new MediaSolidColorBrush(MediaColor.FromArgb(120, 0, 0, 0))
            });
    }
}
