# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

function GetAssemblyVersionFromFile($filename) {
    $regex = 'AssemblyInformationalVersion\("(\d{1,3}\.\d{1,3}\.\d{1,3}(?:-[A-Za-z0-9-\.]+)?)"\)'
    $values = select-string -Path $filename -Pattern $regex | % { $_.Matches } | % { $_.Groups } | % { $_.Value }
    if( $values.Count -eq 2 ) {
        return $values[1]
    }
    Write-Host "Error: Unable to find AssemblyInformationalVersion in $filename" -foregroundcolor "red"
    exit
}

if (-Not (Test-Path 'NuGet.exe')) {
    # this gets the latest nuget.exe version
    Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile 'NuGet.exe'
}

# Get the assembly versions from all files, make sure they match, and use that as the package version
$dotNetFile = "..\Microsoft.Azure.Devices\Properties\AssemblyInfo.cs"
$uwpFile = "..\Microsoft.Azure.Devices.Uwp\Properties\AssemblyInfo.cs"
$dotNetStandardFile = "..\Microsoft.Azure.Devices.NetStandard\Properties\AssemblyInfo.cs"

# Delete existing packages to force rebuild
ls Microsoft.Azure.Devices.*.nupkg | % { del $_ }

$v1 = GetAssemblyVersionFromFile($dotNetFile)
$v2 = GetAssemblyVersionFromFile($uwpFile)
$v3 = GetAssemblyVersionFromFile($dotNetStandardFile)

if($v1 -ne $v2) {
    Write-Host "Error: Mismatching assembly versions in files $dotNetFile and $uwpFile. Check AssemblyInformationalVersion attribute in each file." -foregroundcolor "red"
    return
}

if($v1 -ne $v3) {
    Write-Host "Error: Mismatching assembly versions in files $dotNetFile and $dotNetStandardFile. Check AssemblyInformationalVersion attribute in each file." -foregroundcolor "red"
    return
}

$id='Microsoft.Azure.Devices'

echo "Creating NuGet package $id version $v1"

.\NuGet.exe pack "$id.nuspec" -Prop Configuration=Release -Prop Version=$v1
