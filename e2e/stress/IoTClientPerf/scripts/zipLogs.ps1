$dataFolder = "./"
$files = dir (Join-Path $dataFolder "*.csv")

$i = 0
foreach ($file in $files)
{
    $i++
    $p = $i / $files.Length * 100
    Write-Progress -PercentComplete $p -Activity "Compressing log files"
    
    $outFile = "$($file).zip"

    if (-not (Test-Path $outFile))
    {
        Compress-Archive -Path $file -DestinationPath $outFile
    }
}

$src = Join-Path $dataFolder "*.zip"
