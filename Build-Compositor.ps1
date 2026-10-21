param([string]$OutputPath)
$ErrorActionPreference='Stop'
if(-not $OutputPath){$OutputPath=Join-Path $PSScriptRoot 'build\OverlayBackdrop.dll'}
$sdkRoot=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\UnionMetadata'
$contract=Get-ChildItem -LiteralPath $sdkRoot -Filter Windows.winmd -Recurse -ErrorAction SilentlyContinue | Where-Object {$_.Directory.Name -ne 'Facade'} | Sort-Object FullName -Descending | Select-Object -First 1
if(-not $contract){throw 'Building optional Windows composition support requires a Windows 10/11 SDK (1903 or newer). This is a build dependency, not an application install requirement.'}
New-Item -ItemType Directory -Path (Split-Path $OutputPath -Parent) -Force | Out-Null
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:library /platform:x64 /optimize+ /r:System.Numerics.dll /r:System.Numerics.Vectors.dll /r:System.Runtime.dll /r:System.Runtime.WindowsRuntime.dll ('/r:'+$contract.FullName) ('/out:'+$OutputPath) (Join-Path $PSScriptRoot 'src\Composition\Backdrop.cs')
if($LASTEXITCODE -ne 0){throw 'Could not compile the embedded Windows composition adapter.'}
