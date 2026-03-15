Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@
$proc = Get-Process LifeOS -ErrorAction SilentlyContinue | Select-Object -Last 1
if ($proc) {
    [Win32]::ShowWindow($proc.MainWindowHandle, 9)
    [Win32]::SetForegroundWindow($proc.MainWindowHandle)
    Write-Host "ウィンドウを前面に出しました"
} else {
    Write-Host "プロセスが見つかりません"
}
