param([string]$Version = '1.31.0', [switch]$SkipBuild, [string]$ExecutablePath, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'Invalid release version.' }
$projectRoot = $PSScriptRoot
if (-not $SkipBuild) { & (Join-Path $projectRoot 'Build.ps1') }
if (-not $ExecutablePath) { $ExecutablePath = Join-Path $projectRoot 'Aion2Helper.exe' }
if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) { throw 'Compiled executable not found.' }
$distRoot = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $projectRoot 'dist' }
$packageName = 'Aion-2-Helper-v' + $Version + '-windows-x64'
$packageRoot = Join-Path $distRoot $packageName
# Copy from an explicit allowlist; personal settings and diagnostic data never enter releases.
if (Test-Path -LiteralPath $packageRoot) {
    throw 'Package staging already exists. Choose a new version or archive the previous staging folder.'
}
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'assets') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'protocol') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'assets\sounds') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'assets\skills') -Force | Out-Null
# Keep the ZIP compatible with installed updaters. Effect data, icons and the
# HTML parser are embedded; bundle source notices in the existing THIRD-PARTY
# path rather than adding unsupported installation paths.
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'assets\npcs') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'assets\licenses') -Force | Out-Null
foreach ($name in @('LICENSE-MIT.txt','README.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('assets\npcs\' + $name)) -Destination (Join-Path $packageRoot ('assets\npcs\' + $name))
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\licenses\Aion2ProtocolResearch-MIT.txt') -Destination (Join-Path $packageRoot 'assets\licenses\Aion2ProtocolResearch-MIT.txt')
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\skills\LICENSE-MIT.txt') -Destination (Join-Path $packageRoot 'assets\skills\LICENSE-MIT.txt')
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\skills\README.md') -Destination (Join-Path $packageRoot 'assets\skills\README.md')
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\sounds\party-invite.mp3') -Destination (Join-Path $packageRoot 'assets\sounds\party-invite.mp3')
foreach ($name in @('Aion2Helper.exe','Aion-2-Helper.cmd','README.md','THIRD-PARTY.md','RELEASE-NOTES.md')) {
    $packageSource = if ($name -eq 'Aion2Helper.exe') { $ExecutablePath } else { Join-Path $projectRoot $name }
    Copy-Item -LiteralPath $packageSource -Destination (Join-Path $packageRoot $name)
}
foreach ($name in @('aion-2-helper.png','aion-2-helper.ico')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('assets\' + $name)) -Destination (Join-Path $packageRoot ('assets\' + $name))
}
$notices = [Text.StringBuilder]::new([IO.File]::ReadAllText((Join-Path $packageRoot 'THIRD-PARTY.md')))
[void]$notices.AppendLine("`n## Bundled source notices and documentation`n")
[void]$notices.AppendLine('The following source files are included in full in this document for portable/update compatibility. Their original paths refer to the public source repository; no extra installation files are required.')
foreach ($notice in @('assets/effects/README.md','assets/effects/sources.json','assets/effects/LICENSE-GPL-3.0.txt','assets/effects/LICENSE-MIT.txt','assets/licenses/HtmlAgilityPack-MIT.txt','assets/licenses/Aion2Api-MIT.txt','assets/news/README.md','assets/news/sources.example.json')) {
    [void]$notices.AppendLine("`n### $notice`n")
    [void]$notices.AppendLine([IO.File]::ReadAllText((Join-Path $projectRoot $notice)))
}
[IO.File]::WriteAllText((Join-Path $packageRoot 'THIRD-PARTY.md'), $notices.ToString(), [Text.UTF8Encoding]::new($false))
$packageAssembly = [System.Reflection.AssemblyName]::GetAssemblyName((Join-Path $packageRoot 'Aion2Helper.exe'))
if ($packageAssembly.Version.ToString(3) -ne $Version) { throw 'Package version does not match the compiled executable.' }
foreach ($name in @('sync-opcodes.json','LICENSE-MIT.txt')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('protocol\' + $name)) -Destination (Join-Path $packageRoot ('protocol\' + $name))
}
& (Join-Path $projectRoot 'Download-Dependencies.ps1')
$fpsDependencyRoot = Join-Path $packageRoot 'tools\presentmon'
New-Item -ItemType Directory -Path $fpsDependencyRoot -Force | Out-Null
foreach ($name in @('PresentMon.exe','LICENSE.txt','THIRD_PARTY.txt','README.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('tools\presentmon\' + $name)) -Destination (Join-Path $fpsDependencyRoot $name)
}
$archivePath = Join-Path $distRoot ($packageName + '.zip')
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = Join-Path $distRoot ($packageName + '.sha256')
[System.IO.File]::WriteAllText($checksumPath, $hash + '  ' + [System.IO.Path]::GetFileName($archivePath) + "`n", [System.Text.UTF8Encoding]::new($false))
# Check the actual release ZIP, not a reduced synthetic installer fixture.
& (Join-Path $projectRoot 'Verify-UpdatePackage.ps1') -ArchivePath $archivePath -UpdaterExecutable $ExecutablePath
Write-Output $archivePath
Write-Output $checksumPath
