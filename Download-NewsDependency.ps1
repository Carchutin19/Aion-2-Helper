$ErrorActionPreference = 'Stop'
# Build-time only. The runtime parser is embedded; users install no new runtime.
$newsDirectory = Join-Path $PSScriptRoot 'build\news'
$newsDll = Join-Path $newsDirectory 'HtmlAgilityPack.dll'
$newsDllHash = '26861dfe06f54b21dfdc67f5e81e7fe20ee6ff32fb12b07f3635da7eca728c54'
if ((Test-Path -LiteralPath $newsDll) -and (Get-FileHash -LiteralPath $newsDll -Algorithm SHA256).Hash.ToLowerInvariant() -eq $newsDllHash) { return }
New-Item -ItemType Directory -Path $newsDirectory -Force | Out-Null
$newsPackage = Join-Path $newsDirectory 'htmlagilitypack.1.13.1.nupkg.download'
Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/htmlagilitypack/1.13.1/htmlagilitypack.1.13.1.nupkg' -OutFile $newsPackage
if ((Get-FileHash -LiteralPath $newsPackage -Algorithm SHA256).Hash.ToLowerInvariant() -ne 'fc3f549e1c86eff1ee6e7ad872a7c0f5ac21cee093d0f606f8270b3e937afcce') { throw 'News parser package failed SHA-256 verification.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$newsArchive = [System.IO.Compression.ZipFile]::OpenRead($newsPackage)
try { [System.IO.Compression.ZipFileExtensions]::ExtractToFile($newsArchive.GetEntry('lib/Net40/HtmlAgilityPack.dll'), $newsDll, $true) }
finally { $newsArchive.Dispose() }
if ((Get-FileHash -LiteralPath $newsDll -Algorithm SHA256).Hash.ToLowerInvariant() -ne $newsDllHash) { throw 'News parser DLL failed SHA-256 verification.' }
Remove-Item -LiteralPath $newsPackage
