param([Parameter(Mandatory=$true)][int]$HelperProcessId,[int]$Seconds=20)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$helperProcess = Get-Process -Id $HelperProcessId
if ($helperProcess.Path -ne (Join-Path $projectRoot 'Aion2Helper.exe')) { throw 'The process ID does not belong to Aion 2 Helper in this project.' }
$watch = [System.Diagnostics.Stopwatch]::StartNew()
$beforeCpu = $helperProcess.TotalProcessorTime.TotalMilliseconds
$observations = [System.Collections.Generic.List[object]]::new()
$peakWorkingSet = 0L
while ($watch.Elapsed.TotalSeconds -lt $Seconds) {
    $helperProcess.Refresh()
    $peakWorkingSet = [Math]::Max($peakWorkingSet, $helperProcess.WorkingSet64)
    try {
        $state = Get-Content -LiteralPath (Join-Path $projectRoot 'live-status.json') -Raw | ConvertFrom-Json
        $observations.Add($state)
    } catch {}
    Start-Sleep -Milliseconds 500
}
$helperProcess.Refresh()
$cpuMs = $helperProcess.TotalProcessorTime.TotalMilliseconds - $beforeCpu
$result = [pscustomobject]@{
    elapsedSeconds = $watch.Elapsed.TotalSeconds
    cpuMs = $cpuMs
    oneCorePercent = $cpuMs / ($watch.Elapsed.TotalSeconds * 10)
    peakWorkingSetBytes = $peakWorkingSet
    privateBytes = $helperProcess.PrivateMemorySize64
    threads = $helperProcess.Threads.Count
    handles = $helperProcess.HandleCount
    observations = $observations.ToArray()
}
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'live-game.json')
$result | Select-Object elapsedSeconds,cpuMs,oneCorePercent,peakWorkingSetBytes,privateBytes,threads,handles
$observations | Select-Object -Last 1
