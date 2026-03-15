# LifeOS インストーラー
# 管理者権限不要でインストールします

$AppName    = "LifeOS"
$InstallDir = "$env:LOCALAPPDATA\$AppName"
$ExeName    = "LifeOS.exe"
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceExe  = Join-Path $ScriptDir $ExeName

if (-not (Test-Path $SourceExe)) {
    Write-Error "$ExeName が見つかりません。Install.ps1 と同じフォルダに $ExeName を置いてください。"
    pause
    exit 1
}

# インストール先フォルダ作成
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

# 実行中なら終了
$proc = Get-Process -Name "LifeOS" -ErrorAction SilentlyContinue
if ($proc) {
    $proc | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# ファイルコピー
Copy-Item -Path $SourceExe -Destination $InstallDir -Force
Write-Host "✓ $InstallDir\$ExeName にコピーしました"

# デスクトップショートカット作成
$Desktop  = [Environment]::GetFolderPath("Desktop")
$ShortcutPath = "$Desktop\$AppName.lnk"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath  = "$InstallDir\$ExeName"
$Shortcut.WorkingDirectory = $InstallDir
$Shortcut.Description = "$AppName - 生活管理アプリ"
$Shortcut.Save()
Write-Host "✓ デスクトップにショートカットを作成しました"

# スタートメニューショートカット作成
$StartMenu = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"
$ShortcutPath2 = "$StartMenu\$AppName.lnk"
$Shortcut2 = $WshShell.CreateShortcut($ShortcutPath2)
$Shortcut2.TargetPath  = "$InstallDir\$ExeName"
$Shortcut2.WorkingDirectory = $InstallDir
$Shortcut2.Description = "$AppName - 生活管理アプリ"
$Shortcut2.Save()
Write-Host "✓ スタートメニューにショートカットを作成しました"

Write-Host ""
Write-Host "インストール完了！ デスクトップの $AppName アイコンから起動できます。"
Write-Host "（アプリ起動後は Windows 起動時に自動起動するよう登録されます）"
pause
