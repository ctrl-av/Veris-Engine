$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$textures = Join-Path $PSScriptRoot '..\VerisEditor\ProjectTemplates\TopDownProject\Payload\Content\Textures'
New-Item -ItemType Directory -Path $textures -Force | Out-Null
$white = New-Object Drawing.Bitmap 1, 1
$white.SetPixel(0, 0, [Drawing.Color]::White)
$white.Save((Join-Path $textures 'white.png'), [Drawing.Imaging.ImageFormat]::Png)
$white.Dispose()
$player = New-Object Drawing.Bitmap 64, 32
$graphics = [Drawing.Graphics]::FromImage($player)
$blue = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(80, 160, 255))
foreach ($frame in 0, 1) {
    $x = $frame * 32
    $graphics.FillRectangle($blue, $x + 4, 4, 24, 23)
    $graphics.FillRectangle([Drawing.Brushes]::White, $x + 9, 10, 5, 5)
    $graphics.FillRectangle([Drawing.Brushes]::White, $x + 19, 10, 5, 5)
    $graphics.FillRectangle([Drawing.Brushes]::Navy, $x + 11, 12, 2, 2)
    $graphics.FillRectangle([Drawing.Brushes]::Navy, $x + 21, 12, 2, 2)
    $graphics.FillRectangle($blue, $x + 6 + $frame * 2, 27, 6, 4)
    $graphics.FillRectangle($blue, $x + 20 - $frame * 2, 27, 6, 4)
}
$graphics.Dispose(); $blue.Dispose()
$player.Save((Join-Path $textures 'player.png'), [Drawing.Imaging.ImageFormat]::Png)
$player.Dispose()
