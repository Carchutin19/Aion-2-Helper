param(
    [Parameter(Mandatory=$true)][string]$ArchivePath,
    [Parameter(Mandatory=$true)][string]$UpdaterExecutable,
    [switch]$ExpectRejected
)
$ErrorActionPreference='Stop'
# A separate process prevents a previously loaded assembly from masking the
# selected updater version. Only isolated fixtures under build are changed.
if (-not $env:AION_PACKAGE_VERIFY_CHILD) {
    $previous=$env:AION_PACKAGE_VERIFY_CHILD
    try {
        $env:AION_PACKAGE_VERIFY_CHILD='1'
        $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',$PSCommandPath,'-ArchivePath',[IO.Path]::GetFullPath($ArchivePath),'-UpdaterExecutable',[IO.Path]::GetFullPath($UpdaterExecutable))
        if ($ExpectRejected) { $arguments+='-ExpectRejected' }
        & powershell.exe @arguments
        if ($LASTEXITCODE -ne 0) { throw 'Real update-package validation failed.' }
    } finally { $env:AION_PACKAGE_VERIFY_CHILD=$previous }
    return
}
$archive=[IO.Path]::GetFullPath($ArchivePath)
$client=[IO.Path]::GetFullPath($UpdaterExecutable)
Add-Type -TypeDefinition @'
using System;
using System.Reflection;
using System.Management.Automation;
public static class UpdatePackageReflection {
    public static object Call(MethodInfo method, object[] arguments) {
        for (int i=0; i<arguments.Length; i++) {
            while (arguments[i] is PSObject) arguments[i]=((PSObject)arguments[i]).BaseObject;
        }
        return method.Invoke(null, arguments);
    }
}
'@
$fixture=Join-Path $PSScriptRoot ('build\update-package-validation\'+[Guid]::NewGuid().ToString('N'))
$install=Join-Path $fixture 'installation'
$stage=Join-Path $fixture 'stage'
New-Item -ItemType Directory -Path $install,$stage -Force | Out-Null
Copy-Item -LiteralPath $client -Destination (Join-Path $install 'Aion2Helper.exe')
Copy-Item -LiteralPath $client -Destination (Join-Path $install 'AionDash.exe')
$preserved=@{}
foreach ($name in @('overlay-settings.json','hotbar-profiles.json','pets-state.json','combat-history.json','update-settings.json','game-tweaks.json')) {
    $path=Join-Path $install $name
    [IO.File]::WriteAllText($path,'{"preserve":"'+$name+'"}')
    $preserved[$name]=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}
$assembly=[Reflection.Assembly]::LoadFile($client)
$flags=[Reflection.BindingFlags]'Static,NonPublic'
$installer=$assembly.GetType('UpdateInstaller',$true)
$cancel=[Activator]::CreateInstance($assembly.GetType('UpdateCancellation',$true),$true)
if ([IO.Path]::GetFileName($archive) -notmatch '^Aion-2-Helper-v(\d+\.\d+\.\d+)-windows-x64\.zip$') { throw 'Expected a normal release ZIP name.' }
$version=[Version]$Matches[1]
$job=$null
try {
    $job=[UpdatePackageReflection]::Call($installer.GetMethod('Stage',$flags),@($archive,$stage,$install,$version,$cancel))
} catch {
    $cause=$_.Exception
    while ($cause.InnerException) { $cause=$cause.InnerException }
    if ($ExpectRejected -and $cause -is [IO.InvalidDataException] -and $cause.Message.StartsWith('Unexpected or unsafe update file: ')) {
        Write-Output ('PASS: updater '+$assembly.GetName().Version+' reproduced rejection: '+$cause.Message)
        return
    }
    throw $cause
}
if ($ExpectRejected) { throw 'The incompatible package was unexpectedly accepted.' }
$expected=(Get-FileHash -LiteralPath (Join-Path $stage 'payload\Aion2Helper.exe') -Algorithm SHA256).Hash
$progress=[Action[int,string]]{param($percent,$message)}
$health=[Action]{ [void][UpdatePackageReflection]::Call($installer.GetMethod('VerifyInstalled',$flags),@($job)) }
[void][UpdatePackageReflection]::Call($installer.GetMethod('Apply',$flags),@($job,$progress,$health,$null))
foreach ($name in $preserved.Keys) {
    if ((Get-FileHash -LiteralPath (Join-Path $install $name) -Algorithm SHA256).Hash -ne $preserved[$name]) { throw ('Preferences changed: '+$name) }
}
foreach ($name in @('Aion2Helper.exe','AionDash.exe')) {
    if ((Get-FileHash -LiteralPath (Join-Path $install $name) -Algorithm SHA256).Hash -ne $expected) { throw ('Installed binary differs: '+$name) }
}
Write-Output ('PASS: updater '+$assembly.GetName().Version+' staged and installed '+$version+'; startup health check passed; settings preserved; '+$job.Files.Count+' program files. Fixture: '+$fixture)
