param(
    [string]$Root = $PSScriptRoot
)

[Console]::OutputEncoding = [Text.Encoding]::UTF8
$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path $Root).Path

function Get-TrackedFiles([string]$Pattern) {
    $files = git -C $Root -c core.quotepath=off ls-files -- $Pattern
    if ($LASTEXITCODE) { throw "git ls-files failed in $Root" }
    $files | ForEach-Object { $_ }
}

function Test-RealDate([string]$Text) {
    $parsed = [datetime]::MinValue
    [datetime]::TryParseExact($Text, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture, 'None', [ref]$parsed)
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

function Test-Todo {
    $file = 'TODO.md'
    $full = Join-Path $Root $file
    if (-not (Test-Path -LiteralPath $full)) { return }
    $categories = 'Critical|High Priority|Medium Priority|Low Priority|Architecture'
    $expected = 1
    $lineNo = 0
    foreach ($line in [IO.File]::ReadAllLines($full, [Text.Encoding]::UTF8)) {
        $lineNo++
        if ($line -match '^## ') {
            if (($line -cnotmatch "^## ($categories) — (\d{4}-\d{2}-\d{2})$") -or -not (Test-RealDate $Matches[2])) {
                "${file}:$lineNo heading '$line' must be '## <Category> — YYYY-MM-DD' with Category one of Critical, High Priority, Medium Priority, Low Priority, Architecture"
            }
        } elseif ($line -match '^[-*+] \[[ xX]\]') {
            if ($line -cnotmatch '^- \[[ x]\] \*\*#(\d+) — .+\*\*') {
                "${file}:$lineNo item '$line' must match '- [ ] **#N — <text>**' (items use '- [ ]' or '- [x]')"
            } else {
                $n = [int]$Matches[1]
                if ($n -ne $expected) { "${file}:$lineNo found #$n, expected #$expected" }
                $expected = $n + 1
            }
        }
    }
}

function Test-Decisions {
    $file = 'docs/decisions.md'
    $full = Join-Path $Root $file
    if (-not (Test-Path -LiteralPath $full)) { return }
    $lines = [IO.File]::ReadAllLines($full, [Text.Encoding]::UTF8)
    $entries = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^## ') { $entries += $i }
    }
    $previousDate = $null
    for ($e = 0; $e -lt $entries.Count; $e++) {
        $start = $entries[$e]
        $end = if ($e + 1 -lt $entries.Count) { $entries[$e + 1] } else { $lines.Count }
        $heading = $lines[$start]
        $lineNo = $start + 1
        if (($heading -cnotmatch '^## (\d{4}-\d{2}-\d{2}) — .+') -or -not (Test-RealDate $Matches[1])) {
            "${file}:$lineNo heading '$heading' must be '## YYYY-MM-DD — <title>'"
            continue
        }
        $date = $Matches[1]
        if ($previousDate -and $date -gt $previousDate) {
            "${file}:$lineNo date $date is newer than the entry above ($previousDate); entries must be newest first"
        }
        $previousDate = $date
        $body = $lines[($start + 1)..($end - 1)]
        foreach ($field in 'What', 'Why', 'Links') {
            if (-not ($body | Where-Object { $_ -cmatch "^\*\*${field}:\*\*" })) {
                "${file}:$lineNo entry is missing a **${field}:** line"
            }
        }
    }
}

function Test-Specs {
    foreach ($file in Get-TrackedFiles 'docs/superpowers/specs/*.md') {
        $lines = [IO.File]::ReadAllLines((Join-Path $Root $file), [Text.Encoding]::UTF8)
        $typeLines = @(0..($lines.Count - 1) | Where-Object { $lines[$_] -match '^Type:' })
        if ($typeLines.Count -eq 0) {
            "${file}:1 missing a 'Type: feature', 'Type: fix' or 'Type: chore' line"
        } else {
            foreach ($i in $typeLines) {
                if ($lines[$i] -cnotmatch '^Type: (feature|fix|chore)\s*$') {
                    "${file}:$($i + 1) '$($lines[$i])' must be 'Type: feature', 'Type: fix' or 'Type: chore'"
                }
            }
            if ($typeLines.Count -gt 1) {
                "${file}:$($typeLines[1] + 1) more than one Type: line; exactly one is allowed"
            }
        }
    }
}

function Test-Plans {
    foreach ($file in Get-TrackedFiles 'docs/superpowers/plans/*.md') {
        $lines = [IO.File]::ReadAllLines((Join-Path $Root $file), [Text.Encoding]::UTF8)
        $specLine = @(0..($lines.Count - 1) | Where-Object { $lines[$_] -match '^\*\*Spec:\*\*' })
        if ($specLine.Count -eq 0) {
            "${file}:1 missing a **Spec:** line"
            continue
        }
        $lineNo = $specLine[0] + 1
        if ($lines[$specLine[0]] -notmatch '^\*\*Spec:\*\*\s*(?:`([^`]+)`|(\S+))') {
            "${file}:$lineNo **Spec:** line names no file"
        } else {
            $spec = if ($Matches[1]) { $Matches[1] } else { $Matches[2] }
            if (-not (Test-Path -LiteralPath (Join-Path $Root $spec) -PathType Leaf)) {
                "${file}:$lineNo spec $spec not found"
            }
        }
    }
}

$checks = [ordered]@{
    links = { Test-Links }
    todo = { Test-Todo }
    decisions = { Test-Decisions }
    specs = { Test-Specs }
    plans = { Test-Plans }
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
