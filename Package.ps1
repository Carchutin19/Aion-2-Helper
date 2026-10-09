param([string]$Version = '1.0.0', [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'Invalid release version.' }
$projectRoot = $PSScriptRoot
if (-not $SkipBuild) { & (Join-Path $projectRoot 'Build.ps1') }
$distRoot = Join-Path $projectRoot 'dist'
$packageName = 'Aion-2-Helper-v' + $Version + '-windows-x64'
$packageRoot = Join-Path $distRoot $packageName
# Copy from an explicit allowlist; personal settings and diagnostic data never enter releases.
if (Test-Path -LiteralPath $packageRoot) {
    throw 'Package staging already exists. Choose a new version or archive the previous staging folder.'
}
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'assets') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'protocol') -Force | Out-Null
foreach ($name in @('Aion2Helper.exe','Aion-2-Helper.cmd','README.md','THIRD-PARTY.md','RELEASE-NOTES.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination (Join-Path $packageRoot $name)
}
foreach ($name in @('aion-2-helper.png','aion-2-helper.ico')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('assets\' + $name)) -Destination (Join-Path $packageRoot ('assets\' + $name))
}
foreach ($name in @('sync-opcodes.json','LICENSE-MIT.txt')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('protocol\' + $name)) -Destination (Join-Path $packageRoot ('protocol\' + $name))
}
$archivePath = Join-Path $distRoot ($packageName + '.zip')
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = Join-Path $distRoot ($packageName + '.sha256')
[System.IO.File]::WriteAllText($checksumPath, $hash + '  ' + [System.IO.Path]::GetFileName($archivePath) + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Output $archivePath
Write-Output $checksumPath
