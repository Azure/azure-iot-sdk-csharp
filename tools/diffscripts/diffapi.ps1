$reporoot = (gi $pwd).Parent.Parent.FullName
$baseroot = (gi $reporoot).Parent.FullName
$asmtool = (Join-Path -Path $baseroot -Child "\arcade\artifacts\bin\Microsoft.DotNet.AsmDiff\Debug\netcoreapp3.1\") + "Microsoft.DotNet.AsmDiff.exe"
$internalroot = Join-Path -Path $baseroot -Child "\iot-sdks-internals"
$masterdirectory = Join-Path -Path $internalroot -Child "\sdk_design_docs\CSharp\master"
$previewdirectory = Join-Path -Path $internalroot -Child "\sdk_design_docs\CSharp\preview"

if ((Test-Path $asmtool) -ne $TRUE) {
    Write-Host "Please get the AsmDiff tool from the dotnet arcade https://github.com/dotnet/arcade/tree/main/src/Microsoft.DotNet.AsmDiff. Clone this to the directory containing the SDK repo."
    Write-Host "For example, if this SDK is cloned to c:\repos you should clone https://github.com/dotnet/arcade.git to the c:\repos folder."
    Write-Host ""
    Write-Host "Once you clone the folder run dotnet build in the src/Microsoft.DotNet.AsmDiff folder."
}

# Hardcoded list of assembly names
$filenames = @(
    "Microsoft.Azure.Devices.Shared",
    "Microsoft.Azure.Devices.Client",
    "Microsoft.Azure.Devices",
    "Microsoft.Azure.Devices.Provisioning.Client",
    "Microsoft.Azure.Devices.Provisioning.Service",
    "Microsoft.Azure.Devices.Provisioning.Transport.Amqp",
    "Microsoft.Azure.Devices.Provisioning.Transport.Mqtt",
    "Microsoft.Azure.Devices.Provisioning.Transport.Http",
    "Microsoft.Azure.Devices.Provisioning.Security.Tpm"
)

$oldsetNames = @(
    ((Join-Path -Path $reporoot -ChildPath "\shared\src\bin\Release\netstandard2.1\")+($filenames[0]+".dll")),
    ((Join-Path -Path $reporoot -ChildPath "\iothub\device\src\bin\Release\netstandard2.1\")+($filenames[1]+".dll")),
    ((Join-Path -Path $reporoot -ChildPath "\iothub\service\src\bin\Release\netstandard2.1\")+($filenames[2]+".dll")),
    ((Join-Path -Path $reporoot -ChildPath "\provisioning\device\src\bin\Release\netstandard2.1\")+($filenames[3]+".dll")),
    ((Join-Path -Path $reporoot -ChildPath "\provisioning\service\src\bin\Release\netstandard2.1\")+($filenames[4]+".dll")),
    ((Join-Path -Path $reporoot -ChildPath "\provisioning\transport\amqp\src\bin\Release\netstandard2.1\")+($filenames[5]+".dll")),
    ((Join-Path -Path $reporoot -ChildPath "\provisioning\transport\mqtt\src\bin\Release\netstandard2.1\")+($filenames[6]+".dll")),
    ((Join-Path -Path $reporoot -ChildPath "\provisioning\transport\http\src\bin\Release\netstandard2.1\")+($filenames[7]+".dll")),
    ((Join-Path -Path $reporoot -ChildPath "\security\tpm\src\bin\Release\netstandard2.1\")+($filenames[8]+".dll"))
)

$markdownoutput = @(
    ($masterdirectory+ "\" + ($filenames[0]+".md")),
    ($masterdirectory+ "\" + ($filenames[1]+".md")),
    ($masterdirectory+ "\" + ($filenames[2]+".md")),
    ($masterdirectory+ "\" + ($filenames[3]+".md")),
    ($masterdirectory+ "\" + ($filenames[4]+".md")),
    ($masterdirectory+ "\" + ($filenames[5]+".md")),
    ($masterdirectory+ "\" + ($filenames[6]+".md")),
    ($masterdirectory+ "\" + ($filenames[7]+".md")),
    ($masterdirectory+ "\" + ($filenames[8]+".md"))
)

for($i = 0; $i -lt $filenames.length; $i++) { 
    if (Test-Path $oldsetNames[$i]) {
        Write-Host "Getting original header for" $markdownoutput[$i]
        $originalheader = Get-Content $markdownoutput[$i] | select -First 5
        Write-Host "Creating markdown for" $oldsetNames[$i]
        $switches = "-os", $oldsetNames[$i], "-w", "Markdown", "-o", $markdownoutput[$i]
        & $asmtool $switches
        Write-Host "Replacing header for new" $markdownoutput[$i]
        $newbody = Get-Content $markdownoutput[$i] | Select-Object -Skip 5
        .{
            $originalheader
            $newbody
        } | Set-Content $markdownoutput[$i]


        & .\RemoveHeadersFromDiffFile\exe\RemoveHeadersFromDiffFile $markdownoutput[$i]
    } else {
        Write-Host $oldsetNames[$i] "Does not exist!"
    }
}

Set-Location -Path $masterdirectory
& git diff --numstat