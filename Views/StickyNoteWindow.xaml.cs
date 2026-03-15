using LifeOS.Models;
using LifeOS.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace LifeOS.Views;

public partial class StickyNoteWindow : Window
{
    private readonly StickyNoteViewModel _vm;
    private readonly Action? _openManagerCallback;

    public static readonly string[] NoteColors =
    [
        // ネオン
        "#FFF176", "#80DEEA", "#CCFF90", "#FF80AB", "#B388FF", "#FFD180",
        // ビビッド
        "#F48FB1", "#CE93D8", "#90CAF9", "#A5D6A7",
        // パステル
        "#FFB3C1", "#FFDDB3", "#BAFFC9", "#BAE1FF", "#E8BAFF",
        // クール
        "#B2EBF2", "#CFD8DC", "#F0F4C3", "#FAFAFA", "#FFF9C4",
    ];

    public StickyNoteWindow(StickyNoteViewModel vm, Action? openManagerCallback = null)
    {
        InitializeComponent();
        _vm = vm;
        _openManagerCallback = openManagerCallback;
        DataContext = vm;

        Left = vm.PositionX;
        Top  = vm.PositionY;
        Width  = vm.Width;
        Height = vm.Height;

        LocationChanged += (_, _) => { vm.PositionX = Left; vm.PositionY = Top; };
        SizeChanged     += (_, _) => { vm.Width = Width;   vm.Height = Height; };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInteractiveElement(e.OriginalSource as DependencyObject)) return;
        if (e.ClickCount == 2) _openManagerCallback?.Invoke();
        else DragMove();
    }

    private static bool IsInteractiveElement(DependencyObject? el)
    {
        while (el != null)
        {
            if (el is System.Windows.Controls.TextBox ||
                el is System.Windows.Controls.Button  ||
                el is System.Windows.Controls.CheckBox ||
                el is System.Windows.Controls.Primitives.ButtonBase)
                return true;
            el = System.Windows.Media.VisualTreeHelper.GetParent(el);
        }
        return false;
    }

    private void OpenManager_Click(object sender, RoutedEventArgs e) =>
        _openManagerCallback?.Invoke();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void TaskCheck_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as System.Windows.Controls.CheckBox)?.DataContext is TaskItem task)
            _vm.ToggleTask(task);
    }

    private void TaskDelete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as System.Windows.Controls.Button)?.Tag is TaskItem task)
            _vm.DeleteTask(task);
    }

    private void TaskTitle_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox box) return;
        if (box.DataContext is not TaskItem task) return;
        var text = box.Text.Trim();
        if (!string.IsNullOrEmpty(text) && text != task.Title)
            _vm.UpdateTaskTitle(task, text);
    }

    private void TaskTitle_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            System.Windows.Input.Keyboard.ClearFocus();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            if (sender is System.Windows.Controls.TextBox box && box.DataContext is TaskItem task)
                box.Text = task.Title;
            System.Windows.Input.Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private void NewTaskBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var box = sender as System.Windows.Controls.TextBox;
        var text = box?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;
        _vm.NewTaskTitle = text;
        _vm.AddTaskCommand.Execute(null);
        if (box != null) box.Text = string.Empty;
        e.Handled = true;
    }

    private void NewTaskBox_GotFocus(object sender, RoutedEventArgs e) { }
    private void NewTaskBox_LostFocus(object sender, RoutedEventArgs e) { }

    private bool _memoCollapsed = false;
    private bool _tasksCollapsed = false;

    private void MemoToggle_Click(object sender, RoutedEventArgs e)
    {
        _memoCollapsed = !_memoCollapsed;
        MemoRow.Height = _memoCollapsed ? new GridLength(0) : new GridLength(2, GridUnitType.Star);
        MemoTextBox.Visibility = _memoCollapsed ? Visibility.Collapsed : Visibility.Visible;
        MemoToggleBtn.Content = _memoCollapsed ? "▶" : "▼";
    }

    private void TaskToggle_Click(object sender, RoutedEventArgs e)
    {
        _tasksCollapsed = !_tasksCollapsed;
        TaskListRow.Height = _tasksCollapsed ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        AddTaskRowDef.Height = _tasksCollapsed ? new GridLength(0) : GridLength.Auto;
        TaskListScroll.Visibility = _tasksCollapsed ? Visibility.Collapsed : Visibility.Visible;
        AddTaskArea.Visibility = _tasksCollapsed ? Visibility.Collapsed : Visibility.Visible;
        TaskToggleBtn.Content = _tasksCollapsed ? "▶" : "▼";
    }

    // 右クリックで色変更
    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        var menu = new System.Windows.Controls.ContextMenu();
        var colorNames = new[]
        {
            "ネオンイエロー", "シアン", "ライム", "ネオンピンク", "バイオレット", "オレンジ",
            "ローズ", "ラベンダー", "スカイブルー", "セージ",
            "パステルレッド", "パステルオレンジ", "ミント", "パステルブルー", "パープル",
            "アークティック", "シルバー", "シトロン", "ホワイト", "クリーム",
        };
        for (int i = 0; i < NoteColors.Length; i++)
        {
            var color = NoteColors[i];
            var item = new System.Windows.Controls.MenuItem
            {
                Header = colorNames[i],
                Icon = new System.Windows.Shapes.Rectangle
                {
                    Width = 14, Height = 14,
                    Fill = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)),
                    RadiusX = 3, RadiusY = 3
                }
            };
            item.Click += (_, _) => _vm.Color = color;
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
        e.Handled = true;
    }
}
