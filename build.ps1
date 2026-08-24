$ErrorActionPreference = "Stop"

$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $compiler)) {
    throw "C# compiler not found at $compiler"
}

$iconArgs = @()
if (Test-Path ".\fgmcs.ico") {
    $iconArgs = @("/win32icon:fgmcs.ico")
}

& $compiler `
    /nologo `
    /target:winexe `
    /platform:x64 `
    $iconArgs `
    /out:InputRepeater.exe `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Xml.dll `
    Program.cs

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Built InputRepeater.exe"
