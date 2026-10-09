$ErrorActionPreference = 'Stop'
$dependencyRoot = Join-Path $PSScriptRoot 'tools\presentmon'
$executable = Join-Path $dependencyRoot 'PresentMon.exe'
$expectedHash = 'b2a706bc6ad475749e3b7e3409263aa1e6906d45bdcf993f6dbc0f660188f1af'
New-Item -ItemType Directory -Path $dependencyRoot -Force | Out-Null
if ((Test-Path -LiteralPath $executable) -and (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant() -eq $expectedHash) {
    Write-Output 'PresentMon 2.6.0 is ready.'
    return
}
# Pin the standalone console, not the optional service/GUI installer.
$download = Join-Path $dependencyRoot 'PresentMon.exe.download'
Invoke-WebRequest -Uri 'https://github.com/GameTechDev/PresentMon/releases/download/v2.6.0/PresentMon-2.6.0-x64.exe' -OutFile $download
if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) {
    throw 'PresentMon download failed SHA-256 verification.'
}
$signature = Get-AuthenticodeSignature -LiteralPath $download
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Intel Corporation') {
    throw 'PresentMon download did not have a valid Intel signature.'
}
Move-Item -LiteralPath $download -Destination $executable -Force
Write-Output 'PresentMon 2.6.0 downloaded and verified.'
