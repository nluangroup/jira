#!/usr/bin/env pwsh
# Prints the "Effort:" line for a card's final report: wall-clock time since -Since and the tokens
# recorded in this repo's local Claude Code transcripts between -Since and -Until.
# Counts every session and subagent for this repo on this machine in that window.
param(
    [Parameter(Mandatory)][datetimeoffset]$Since,
    [datetimeoffset]$Until = [datetimeoffset]::Now,
    [string[]]$ProjectDir
)

$ErrorActionPreference = 'Stop'
$inv = [System.Globalization.CultureInfo]::InvariantCulture
if ($Until -lt $Since) { throw "-Until ($Until) is before -Since ($Since)" }

if (-not $ProjectDir) {
    $configDir = if ($env:CLAUDE_CONFIG_DIR) { $env:CLAUDE_CONFIG_DIR } else { Join-Path $HOME '.claude' }
    # Transcripts are stored under the directory a session started in: the main checkout or a worktree.
    $roots = @(
        (Split-Path -Parent (git rev-parse --path-format=absolute --git-common-dir)),
        (git rev-parse --show-toplevel)
    ) | ForEach-Object { [System.IO.Path]::GetFullPath($_) } | Select-Object -Unique
    $ProjectDir = $roots | ForEach-Object { Join-Path $configDir 'projects' ($_ -replace '[^A-Za-z0-9]', '-') }
}
$ProjectDir = @($ProjectDir | Where-Object { Test-Path $_ })
if (-not $ProjectDir) { throw 'No Claude Code transcripts found for this repo' }

function Get-Usage([System.IO.FileInfo]$file) {
    # Streaming can write one message over several lines; keep the largest usage seen per message id.
    $byId = @{}
    foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
        if (-not $line.Contains('"usage"')) { continue }
        # A transcript being written can end in a partial line.
        try { $entry = $line | ConvertFrom-Json -Depth 64 -DateKind String } catch { continue }
        $message = $entry.message
        if ($message -isnot [psobject] -or -not $message.usage -or -not $entry.timestamp) { continue }
        $at = [datetimeoffset]::Parse($entry.timestamp, $inv)
        if ($at -lt $Since -or $at -gt $Until) { continue }
        $u = $message.usage
        $tokens = @{
            In     = [long]$u.input_tokens + [long]$u.cache_creation_input_tokens
            Cached = [long]$u.cache_read_input_tokens
            Out    = [long]$u.output_tokens
        }
        $id = [string]$message.id
        if (-not $byId.ContainsKey($id) -or $byId[$id].Out -lt $tokens.Out) { $byId[$id] = $tokens }
    }
    $sum = [ordered]@{ In = [long]0; Cached = [long]0; Out = [long]0; Messages = $byId.Count }
    foreach ($t in $byId.Values) { $sum.In += $t.In; $sum.Cached += $t.Cached; $sum.Out += $t.Out }
    $sum
}

function Format-Tokens([long]$n) {
    if ($n -ge 999500) { return ($n / 1000000).ToString('0.0', $inv) + 'M' }
    if ($n -ge 10000) { return ($n / 1000).ToString('0', $inv) + 'k' }
    if ($n -ge 1000) { return ($n / 1000).ToString('0.0', $inv) + 'k' }
    "$n"
}

function Format-Part([string]$label, $u) {
    "$label $(Format-Tokens $u.In) in / $(Format-Tokens $u.Out) out (+$(Format-Tokens $u.Cached) cache reads)"
}

# A file last written before -Since holds no messages in the window.
$recent = { $_.LastWriteTime -ge $Since.LocalDateTime }
$mainFiles = $ProjectDir | ForEach-Object { Get-ChildItem -Path $_ -Filter '*.jsonl' -File } | Where-Object $recent
$subFiles = $ProjectDir | ForEach-Object { Get-ChildItem -Path $_ -Directory } |
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
"Effort: $time elapsed since In Progress; tokens: $(Format-Part 'main' $main), $(Format-Part "subagents ($subCount)" $subs)"
