$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$content = Join-Path $PSScriptRoot '..\Samples\BeaconRun\Content'
$textures = Join-Path $content 'Textures'
$audio = Join-Path $content 'Audio'
New-Item -ItemType Directory -Path $textures, $audio -Force | Out-Null
$starter = Join-Path $PSScriptRoot '..\VerisEditor\ProjectTemplates\TopDownProject\Payload\Content\Textures'
Copy-Item -LiteralPath (Join-Path $starter 'white.png'), (Join-Path $starter 'player.png') -Destination $textures -Force
$heading = New-Object Drawing.Font 'Segoe UI', 42, ([Drawing.FontStyle]::Bold)
$body = New-Object Drawing.Font 'Segoe UI', 19
$small = New-Object Drawing.Font 'Segoe UI', 16
$center = New-Object Drawing.StringFormat
$center.Alignment = [Drawing.StringAlignment]::Center
foreach ($screen in @(
    @{ Name = 'title'; Title = 'BEACON RUN'; Color = [Drawing.Color]::FromArgb(96, 165, 250); Lines = "Collect three gold signals before time runs out.`nAvoid the red drones. Solid crates block your path.`n`nWASD / ARROWS to move`nENTER to start    ESC to quit" },
    @{ Name = 'win'; Title = 'SIGNAL RESTORED'; Color = [Drawing.Color]::FromArgb(52, 211, 153); Lines = "All three signals recovered.`n`nR to play again    ESC to quit" },
    @{ Name = 'lose'; Title = 'SIGNAL LOST'; Color = [Drawing.Color]::FromArgb(248, 113, 113); Lines = "A drone intercepted you, or the timer expired.`n`nR to retry    ESC to quit" }
)) {
    $bitmap = New-Object Drawing.Bitmap 960, 640
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([Drawing.Color]::FromArgb(18, 27, 44))
    $brush = New-Object Drawing.SolidBrush $screen.Color
    $graphics.DrawString($screen.Title, $heading, $brush, ([Drawing.RectangleF]::new(40, 130, 880, 90)), $center)
    $graphics.DrawString($screen.Lines, $body, [Drawing.Brushes]::WhiteSmoke, ([Drawing.RectangleF]::new(60, 260, 840, 320)), $center)
    $graphics.Dispose(); $brush.Dispose()
    $bitmap.Save((Join-Path $textures ($screen.Name + '.png')), [Drawing.Imaging.ImageFormat]::Png); $bitmap.Dispose()
}
$hud = New-Object Drawing.Bitmap 960, 80
$graphics = [Drawing.Graphics]::FromImage($hud)
$graphics.Clear([Drawing.Color]::FromArgb(18, 27, 44))
$graphics.DrawString('BEACON RUN', $body, [Drawing.Brushes]::WhiteSmoke, 22, 8)
$graphics.DrawString('3 SIGNALS  /  AVOID DRONES  /  35 SECONDS', $small, [Drawing.Brushes]::LightSteelBlue, 22, 41)
$graphics.Dispose(); $hud.Save((Join-Path $textures 'hud.png'), [Drawing.Imaging.ImageFormat]::Png); $hud.Dispose()
$digits = New-Object Drawing.Bitmap 400, 40
$graphics = [Drawing.Graphics]::FromImage($digits)
foreach ($score in 0..3) { $graphics.DrawString("$score / 3", $body, [Drawing.Brushes]::Gold, $score * 100 + 12, 0) }
$graphics.Dispose(); $digits.Save((Join-Path $textures 'score.png'), [Drawing.Imaging.ImageFormat]::Png); $digits.Dispose()
$writer = New-Object IO.BinaryWriter ([IO.File]::Create((Join-Path $audio 'signal.wav')))
$samples = 8820
$writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF')); $writer.Write([int](36 + $samples * 2)); $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
$writer.Write([int]16); $writer.Write([int16]1); $writer.Write([int16]1); $writer.Write([int]44100); $writer.Write([int]88200); $writer.Write([int16]2); $writer.Write([int16]16)
$writer.Write([Text.Encoding]::ASCII.GetBytes('data')); $writer.Write([int]($samples * 2))
for ($index = 0; $index -lt $samples; $index++) { $writer.Write([int16](5000 * (1 - $index / $samples) * [Math]::Sin($index * 880 * 2 * [Math]::PI / 44100))) }
$writer.Dispose(); $heading.Dispose(); $body.Dispose(); $small.Dispose(); $center.Dispose()
