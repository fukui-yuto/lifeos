Add-Type -AssemblyName System.Drawing

function New-IconBitmap($size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # 角丸背景（グラデーション）
    $radius = [int]($size * 0.20)
    $c1 = [System.Drawing.Color]::FromArgb(255, 100, 108, 255)  # 明るいインディゴ
    $c2 = [System.Drawing.Color]::FromArgb(255, 48,  56, 180)  # 深いインディゴ
    $gradBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, 0)),
        (New-Object System.Drawing.Point($size, $size)),
        $c1, $c2)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r2 = $radius * 2
    $path.AddArc(0,            0,            $r2, $r2, 180, 90)
    $path.AddArc($size - $r2,  0,            $r2, $r2, 270, 90)
    $path.AddArc($size - $r2,  $size - $r2,  $r2, $r2,   0, 90)
    $path.AddArc(0,            $size - $r2,  $r2, $r2,  90, 90)
    $path.CloseFigure()
    $g.FillPath($gradBrush, $path)

    # 小さいカード形（ノートのシンボル）
    $cx = [int]($size / 2)
    $cy = [int]($size / 2)
    $cw = [int]($size * 0.44)
    $ch = [int]($size * 0.52)
    $cx0 = $cx - [int]($cw / 2)
    $cy0 = $cy - [int]($ch / 2)
    $cr  = [int]($size * 0.06)

    $cardPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $cr2 = $cr * 2
    $cardPath.AddArc($cx0,         $cy0,         $cr2, $cr2, 180, 90)
    $cardPath.AddArc($cx0+$cw-$cr2,$cy0,         $cr2, $cr2, 270, 90)
    $cardPath.AddArc($cx0+$cw-$cr2,$cy0+$ch-$cr2,$cr2, $cr2,   0, 90)
    $cardPath.AddArc($cx0,         $cy0+$ch-$cr2,$cr2, $cr2,  90, 90)
    $cardPath.CloseFigure()

    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 255, 255, 255))
    $g.FillPath($whiteBrush, $cardPath)

    # カード上の横線（3本）
    $lineColor = [System.Drawing.Color]::FromArgb(120, 70, 80, 200)
    $lw = [float]([Math]::Max(1.0, $size * 0.04))
    $pen = New-Object System.Drawing.Pen($lineColor, $lw)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
    $lx0 = $cx0 + [int]($cw * 0.18)
    $lx1 = $cx0 + [int]($cw * 0.82)
    for ($i = 1; $i -le 3; $i++) {
        $ly = $cy0 + [int]($ch * (0.25 + $i * 0.17))
        $g.DrawLine($pen, $lx0, $ly, $lx1, $ly)
    }

    $g.Dispose()
    return $bmp
}

$sizes = @(16, 32, 48, 256)
$pngBytes = @{}
foreach ($s in $sizes) {
    $bmp = New-IconBitmap $s
    $ms  = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes[$s] = $ms.ToArray()
    $bmp.Dispose()
    $ms.Dispose()
}

# ICO ファイルの書き出し
$ico    = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($ico)

# ヘッダー
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)

# ディレクトリエントリ（オフセット計算）
$offset = 6 + $sizes.Count * 16
foreach ($s in $sizes) {
    $w = if ($s -eq 256) { 0 } else { $s }
    $h = if ($s -eq 256) { 0 } else { $s }
    $writer.Write([byte]$w)
    $writer.Write([byte]$h)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$pngBytes[$s].Length)
    $writer.Write([uint32]$offset)
    $offset += $pngBytes[$s].Length
}

# 画像データ
foreach ($s in $sizes) {
    $writer.Write($pngBytes[$s])
}

$writer.Flush()
[System.IO.File]::WriteAllBytes('C:\Users\yuto\Desktop\GitHub\lifeos\app.ico', $ico.ToArray())
Write-Host "Icon generated: app.ico"
