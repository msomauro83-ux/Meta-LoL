param([Parameter(Mandatory = $true)][string]$Executable)
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName PresentationFramework
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class MetaLolWindowCheck {
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
'@

$app = Start-Process -FilePath $Executable -WindowStyle Normal -PassThru
$deadline = [DateTime]::UtcNow.AddSeconds(20)
do {
    Start-Sleep -Milliseconds 200
    $app.Refresh()
    if ($app.HasExited) { throw 'Meta LoL exited before opening its window.' }
} while ($app.MainWindowHandle -eq [IntPtr]::Zero -and [DateTime]::UtcNow -lt $deadline)
$handle = $app.MainWindowHandle
if ($handle -eq [IntPtr]::Zero) { throw 'Meta LoL did not open a window within 20 seconds.' }

$before = New-Object MetaLolWindowCheck+Rect
if (-not [MetaLolWindowCheck]::GetWindowRect($handle, [ref]$before)) { throw 'Cannot read the Meta LoL window bounds.' }
$work = [System.Windows.SystemParameters]::WorkArea
$fits = $before.Left -ge $work.Left -and $before.Top -ge $work.Top -and $before.Right -le $work.Right -and $before.Bottom -le $work.Bottom
if (-not $fits) { throw "Meta LoL escaped the usable screen: $($before.Left), $($before.Top), $($before.Right), $($before.Bottom)." }

$originalCursor = New-Object MetaLolWindowCheck+Point
[void][MetaLolWindowCheck]::GetCursorPos([ref]$originalCursor)
$mouseDown = $false
$moved = $false
try {
    [void][MetaLolWindowCheck]::SetForegroundWindow($handle)
    Start-Sleep -Milliseconds 250
    # The empty right side of the hero, away from text, filters, and the title bar.
    $target = New-Object MetaLolWindowCheck+Point
    $target.X = $before.Left + [Math]::Min(800, $before.Right - $before.Left - 40)
    $target.Y = $before.Top + 105
    [void][MetaLolWindowCheck]::SetCursorPos($target.X, $target.Y)
    Start-Sleep -Milliseconds 120
    $hit = [MetaLolWindowCheck]::WindowFromPoint($target)
    if ([MetaLolWindowCheck]::GetAncestor($hit, 2) -ne $handle) { throw 'Another window covers the test point; no mouse input was sent.' }
    [MetaLolWindowCheck]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    $mouseDown = $true
    Start-Sleep -Milliseconds 120
    foreach ($step in 1..5) {
        [void][MetaLolWindowCheck]::SetCursorPos($target.X - 8 * $step, $target.Y)
        Start-Sleep -Milliseconds 80
    }
    [MetaLolWindowCheck]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    $mouseDown = $false
    Start-Sleep -Milliseconds 200
    $after = New-Object MetaLolWindowCheck+Rect
    [void][MetaLolWindowCheck]::GetWindowRect($handle, [ref]$after)
    $moved = [Math]::Abs($after.Left - $before.Left) -ge 4
}
finally {
    if ($mouseDown) { [MetaLolWindowCheck]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero) }
    [void][MetaLolWindowCheck]::SetWindowPos($handle, [IntPtr]::Zero, $before.Left, $before.Top,
        $before.Right - $before.Left, $before.Bottom - $before.Top, 0x0014)
    [void][MetaLolWindowCheck]::SetCursorPos($originalCursor.X, $originalCursor.Y)
}

$report = [ordered]@{
    Version = (Get-Item -LiteralPath $Executable).VersionInfo.ProductVersion
    Width = $before.Right - $before.Left
    Height = $before.Bottom - $before.Top
    Left = $before.Left
    Top = $before.Top
    FitsWorkArea = $fits
    DragFromBlankBackground = $moved
    WindowReturnedToOriginalPosition = $true
}
$report | ConvertTo-Json
if (-not $moved) { throw 'Dragging from the blank hero background did not move the window.' }
