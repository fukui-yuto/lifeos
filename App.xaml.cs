using LifeOS.Services;
using LifeOS.ViewModels;
using LifeOS.Views;
using Microsoft.Win32;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace LifeOS;

public partial class App : System.Windows.Application
{
    private const string AppName = "LifeOS";

    private MainViewModel? _mainVm;
    private MainWindow? _mainWindow;
    private WinForms.NotifyIcon? _trayIcon;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, ex) =>
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppName, "error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now}] {ex.Exception}\n\n");
            ex.Handled = true;
        };

        var db = new DatabaseService();
        _mainVm = new MainViewModel(db);
        _mainWindow = new MainWindow(_mainVm);

        SetupTrayIcon();
        SetWindowsStartup();

        _mainWindow.Show();
    }

    private void SetupTrayIcon()
    {
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
        var icon = System.IO.File.Exists(iconPath)
            ? new System.Drawing.Icon(iconPath)
            : System.Drawing.SystemIcons.Application;

        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = AppName
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("管理ウィンドウを開く", null, (_, _) => ShowMainWindow());
        menu.Items.Add("-");
        menu.Items.Add("終了", null, (_, _) => Shutdown());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.Click += (_, _) => ShowMainWindow();
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Dispatcher.Invoke(() =>
        {
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.Topmost = true;
            _mainWindow.Topmost = false;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                _mainWindow.BringNotesToFront();
            };
            timer.Start();
        });
    }

    private void SetWindowsStartup()
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            var exePath = Environment.ProcessPath ?? string.Empty;
            if (!string.IsNullOrEmpty(exePath))
                key?.SetValue(AppName, $"\"{exePath}\"");
        }
        catch { /* 起動時登録は任意 */ }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
