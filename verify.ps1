param(
    [string]$Root = $PSScriptRoot
)

[Console]::OutputEncoding = [Text.Encoding]::UTF8
$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path $Root).Path

function Get-TrackedFiles([string]$Pattern) {
    git -C $Root -c core.quotepath=off ls-files -- $Pattern | ForEach-Object { $_ }
}

function Test-Links {
    $problems = @()
    foreach ($file in Get-TrackedFiles '*.md') {
        $full = Join-Path $Root $file
        $dir = Split-Path $full -Parent
        $inFence = $false
        $lineNo = 0
        foreach ($line in [IO.File]::ReadAllLines($full, [Text.Encoding]::UTF8)) {
            $lineNo++
            if ($line -match '^\s*(```|~~~)') {
                $inFence = -not $inFence
                continue
            }
            if ($inFence) { continue }
            $text = [regex]::Replace($line, '`+[^`]*`+', '')
            foreach ($m in [regex]::Matches($text, '\[[^\]]*\]\(([^)\s]+)(?:\s+"[^"]*")?\)')) {
                $target = $m.Groups[1].Value.Trim('<', '>')
                if ($target -match '^(https?:|mailto:|#)') { continue }
                $path = [Uri]::UnescapeDataString($target.Split('#')[0])
                if ($path -eq '') { continue }
                if (-not (Test-Path -LiteralPath (Join-Path $dir $path))) {
                    $problems += "${file}:$lineNo $path not found"
                }
            }
        }
    }
    $problems
}

$checks = [ordered]@{
    links = { Test-Links }
}

$failed = $false
foreach ($name in $checks.Keys) {
    $problems = @(& $checks[$name])
    if ($problems.Count -eq 0) {
        Write-Output "PASS: $name"
    } else {
        $failed = $true
        foreach ($p in $problems) { Write-Output "FAIL: $name — $p" }
    }
}
exit ($failed ? 1 : 0)
