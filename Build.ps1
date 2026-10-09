$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpfRuntime = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Xaml.dll ('/r:' + (Join-Path $wpfRuntime 'WindowsBase.dll')) ('/r:' + (Join-Path $wpfRuntime 'PresentationFramework.dll')) ('/r:' + (Join-Path $wpfRuntime 'PresentationCore.dll')) ('/r:' + (Join-Path $wpfRuntime 'WindowsFormsIntegration.dll')) ('/resource:' + (Join-Path $projectRoot 'src\SettingsTheme.xaml') + ',SettingsTheme.xaml') ('/win32manifest:' + (Join-Path $projectRoot 'app.manifest')) ('/win32icon:' + (Join-Path $projectRoot 'assets\aion-2-helper.ico')) ('/out:' + (Join-Path $projectRoot 'DashProbe.exe')) (Join-Path $projectRoot 'src\AssemblyInfo.cs') (Join-Path $projectRoot 'src\DashProbe.cs') (Join-Path $projectRoot 'src\DashSignal.cs') (Join-Path $projectRoot 'src\EnergyOverlay.cs') (Join-Path $projectRoot 'src\EnergySettings.cs') (Join-Path $projectRoot 'src\SettingsVisual.cs') (Join-Path $projectRoot 'src\UiLanguage.cs')
if ($LASTEXITCODE -ne 0) { throw 'Could not compile DashProbe.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'DashProbe.exe') -Destination (Join-Path $projectRoot 'AionDash.exe') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'DashProbe.exe') -Destination (Join-Path $projectRoot 'Aion2Helper.exe') -Force
