#!/usr/bin/env pwsh
# Prints the "Effort:" line for a card's final report: Claude's elapsed time and the tokens
# recorded in this repo's local Claude Code transcripts between -Since and -Until.
# Counts every session and subagent for this repo on this machine in that window.
param(
    [Parameter(Mandatory)][datetimeoffset]$Since,
    [datetimeoffset]$Until = [datetimeoffset]::Now,
    [string]$ProjectDir
)

$ErrorActionPreference = 'Stop'
$inv = [System.Globalization.CultureInfo]::InvariantCulture

if (-not $ProjectDir) {
    $configDir = if ($env:CLAUDE_CONFIG_DIR) { $env:CLAUDE_CONFIG_DIR } else { Join-Path $HOME '.claude' }
    # Worktrees share the main checkout's sessions, so key on the main checkout's path.
    $repoRoot = Split-Path -Parent (git rev-parse --path-format=absolute --git-common-dir)
    $ProjectDir = Join-Path $configDir 'projects' ($repoRoot -replace '[^A-Za-z0-9]', '-')
}
if (-not (Test-Path $ProjectDir)) { throw "No Claude Code transcripts at $ProjectDir" }

function Get-Usage([System.IO.FileInfo]$file) {
    $sum = [ordered]@{ In = [long]0; Cached = [long]0; Out = [long]0; Messages = 0 }
    $seen = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
        if (-not $line.Contains('"usage"')) { continue }
        $entry = $line | ConvertFrom-Json -Depth 64 -DateKind String
        $message = $entry.message
        if (-not $message.usage -or -not $entry.timestamp) { continue }
        # Streaming writes one message over several lines; count each message id once.
        if (-not $seen.Add([string]$message.id)) { continue }
        $at = [datetimeoffset]::Parse($entry.timestamp, $inv)
        if ($at -lt $Since -or $at -gt $Until) { continue }
        $u = $message.usage
        $sum.In += [long]$u.input_tokens + [long]$u.cache_creation_input_tokens
        $sum.Cached += [long]$u.cache_read_input_tokens
        $sum.Out += [long]$u.output_tokens
        $sum.Messages++
    }
    $sum
}

function Format-Tokens([long]$n) {
    if ($n -ge 1000000) { return ($n / 1000000).ToString('0.0', $inv) + 'M' }
    if ($n -ge 1000) { return ($n / 1000).ToString('0', $inv) + 'k' }
    "$n"
}

function Format-Part([string]$label, $u) {
    "$label $(Format-Tokens $u.In) in / $(Format-Tokens $u.Out) out (+$(Format-Tokens $u.Cached) cache reads)"
}

$recent = { $_.LastWriteTime -ge $Since.LocalDateTime }
$mainFiles = Get-ChildItem -Path $ProjectDir -Filter '*.jsonl' -File | Where-Object $recent
$subFiles = Get-ChildItem -Path $ProjectDir -Directory |
    ForEach-Object { Join-Path $_.FullName 'subagents' } |
    Where-Object { Test-Path $_ } |
    ForEach-Object { Get-ChildItem -Path $_ -Filter '*.jsonl' -File } |
    Where-Object $recent

$main = [ordered]@{ In = [long]0; Cached = [long]0; Out = [long]0 }
foreach ($f in $mainFiles) {
    $u = Get-Usage $f
    $main.In += $u.In; $main.Cached += $u.Cached; $main.Out += $u.Out
}
$subs = [ordered]@{ In = [long]0; Cached = [long]0; Out = [long]0 }
$subCount = 0
foreach ($f in $subFiles) {
    $u = Get-Usage $f
    if ($u.Messages -eq 0) { continue }
    $subCount++
    $subs.In += $u.In; $subs.Cached += $u.Cached; $subs.Out += $u.Out
}

$elapsed = $Until - $Since
$time = '{0}h {1}m' -f [math]::Floor($elapsed.TotalHours), $elapsed.Minutes
"Effort: Claude $time (In Progress to In Review); tokens: $(Format-Part 'main' $main), $(Format-Part "subagents ($subCount)" $subs)"
