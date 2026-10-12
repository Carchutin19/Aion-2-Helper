param([Parameter(Mandatory=$true)][string]$Encoder)
$ErrorActionPreference = 'Stop'
# Independently generated image; no source video or assets from another mod.
Add-Type -AssemblyName System.Drawing
$sourcePath = Join-Path $PSScriptRoot 'black.bmp'
$outputPath = Join-Path $PSScriptRoot 'blank-intro.bk2'
$bitmap = New-Object System.Drawing.Bitmap 64,64
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.Clear([System.Drawing.Color]::Black)
    $bitmap.Save($sourcePath,[System.Drawing.Imaging.ImageFormat]::Bmp)
} finally { $graphics.Dispose(); $bitmap.Dispose() }
# Bink2ForUnreal is an existing Unreal development tool, not redistributed.
$arguments = @('Bink2c',('"'+$sourcePath+'"'),('"'+$outputPath+'"'),'/V200','/F60','/E1','/L-1','/O')
$process = Start-Process -FilePath $Encoder -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Encoder started: '+$process.Id+'. Close its finished progress window after encoding.')
