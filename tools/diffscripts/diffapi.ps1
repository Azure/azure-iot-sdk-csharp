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
    exit 1
}

# Get the last tag from the git repo and do the comparison
$lasttag = git describe --tags --abbrev=0  | tee -Variable lasttag
& git log --stat "$lasttag..HEAD" --output releaselog_detailed.txt
& git log --oneline "$lasttag..HEAD" --output releaselog_short.txt

Write-Host ""
Write-Host "Generated release log from tag $lasttag to the current HEAD."
Write-Host "The detailed log can be found by editing: releaselog_detailed.txt"
Write-Host "The short log can be found by editing: releaselog_short.txt"
Write-Host ""
Write-Host "NOTE: If there is a tag that you want to compare to that is earlier than the most recent tag you can run:"
Write-Host "git log --stat <tagversion>..HEAD --output releaselog_detailed.txt"
Write-Host "git log --oneline $lasttag..HEAD --output releaselog_short.txt"
Write-Host ""

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

# All of the files from the build
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

# Create a list of the markdown files so we can compare them to the API doc directory
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

# Create a list of the markdown files so we can compare them to the API doc directory
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

Write-Host ""

# Save old directory so we can return here.
$olddir = $pwd

# Nav to the docs directory to run the comparison
Set-Location -Path $masterdirectory
$diffoutput = git diff --numstat

if ($diffoutput -eq $null) {
    Write-Host "There were no changes in the API Surface related to the comparison of the ASM Tool. Check the solutions to make sure there were not other changes that would affect the release and require a major version update."
} else {
    Write-Host "Changes have been detected. Verify each file listed below to be sure." 
    $added = 0
    $deleted = 0
    foreach ($line in $diffoutput) {
        $_ = $line -match "(?<added>\d)\s(?<deleted>\d)\s(?<file>.*)" 
        $fileadd = $Matches.added
        $filedel = $Matches.deleted
        $file = $Matches.file
        $added = $added + $fileadd
        $deleted = $deleted + $filedel
        Write-Host "There have been $filedel deletions and $fileadd additions to the $file file. We will need to evaluate the changes." 
    }
}

Set-Location -Path $olddir