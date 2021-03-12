# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK Release comparison script

.DESCRIPTION
This script is used to compare the current candidate for release to the previous version that is in the iot-sdk-internals 

Parameters:
    -AsmToolPath: The path to the compiled AsmDiff tool found in the dotnet arcade (ex: c:\tools\asmdifftool)
    -SDKInternalsPath: The path of the iot-sdk-internals repository (ex: c:\repo\iot-sdks-internals)
	-Preview indicates you will compare the output to the last preview version

.EXAMPLE
.\diffapi

Executes the commands assuming everything is cloned relative to the SDK repo and the AsmDiff tool is compiled using the arcade instructions.
.EXAMPLE
.\diffapi -AsmToolPath c:\tools\asmdifftool

Specifies the location of the AsmDiff tool executable to run the diff commands.
.EXAMPLE
.\diffapi -SDKInternalsPath c:\repo\iot-sdks-internals

Specifies the location of the SDK Internals repository if not cloned relative to the SDK repository.
.EXAMPLE
.\diffapi -Preview

Executes the commands assuming everything is cloned relative to the SDK repo but will compare against the preview versions of the documents.
.LINK
https://github.com/azure/azure-iot-sdk-csharp

#>

Param(
    [ValidateScript({
            if (-Not ($_ | Test-Path -PathType Container)){
                throw "Folder $_ does not exist."
            }
            return $true
        })]
    # The path to the compiled AsmDiff tool found in the dotnet arcade (ex: c:\tools\asmdifftool)
    [System.IO.FileInfo] $AsmToolPath = $null,
    [ValidateScript({
            if (-Not ($_ | Test-Path -PathType Container)){
                throw "Folder $_ does not exist."
            }
            return $true
        })]
    # The path of the iot-sdk-internals repository (ex: c:\repo\iot-sdks-internals)
    [System.IO.FileInfo] $SDKInternalsPath = $null,
    # Indicates you will compare the output to the last preview version instead of master
    [switch] $Preview
)

# Defaults for both the current repository and the repository parent directory
$repoRootPath = (Get-Item $pwd).Parent.Parent.FullName
$baseRootPath = (Get-Item $repoRootPath).Parent.FullName

# Set the path to the AsmDiff tool, if we specify the path on the command line we will set it as such
$asmToolRoot = (Join-Path -Path $baseRootPath -Child "\arcade\artifacts\bin\Microsoft.DotNet.AsmDiff\Debug\netcoreapp3.1\") 
if ($AsmToolPath -ne $null) {
    $asmToolRoot = $AsmToolPath
}
$asmToolExecutable = $asmToolRoot + "Microsoft.DotNet.AsmDiff.exe"


# Set the path to the SDK Internals Repo, if we specify the path on the command line we will set it as such
$internalRootPath = Join-Path -Path $baseRootPath -Child "\iot-sdks-internals"
if ($SDKInternalsPath -ne $null) {
    $internalRootPath = $SDKInternalsPath
}

# If we specify to use the preview directory on the command line we will set it as such
$compareDirectory = Join-Path -Path $internalRootPath -Child "\sdk_design_docs\CSharp\master"
if ($Preview) {
    $compareDirectory = Join-Path -Path $internalRootPath -Child "\sdk_design_docs\CSharp\preview"
}

# Hardcoded list of assembly names
$assemblyRootNames = @(
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
$assemblyFilePath = @(
    ((Join-Path -Path $repoRootPath -ChildPath "\shared\src\bin\Release\netstandard2.1\")+($assemblyRootNames[0]+".dll")),
    ((Join-Path -Path $repoRootPath -ChildPath "\iothub\device\src\bin\Release\netstandard2.1\")+($assemblyRootNames[1]+".dll")),
    ((Join-Path -Path $repoRootPath -ChildPath "\iothub\service\src\bin\Release\netstandard2.1\")+($assemblyRootNames[2]+".dll")),
    ((Join-Path -Path $repoRootPath -ChildPath "\provisioning\device\src\bin\Release\netstandard2.1\")+($assemblyRootNames[3]+".dll")),
    ((Join-Path -Path $repoRootPath -ChildPath "\provisioning\service\src\bin\Release\netstandard2.1\")+($assemblyRootNames[4]+".dll")),
    ((Join-Path -Path $repoRootPath -ChildPath "\provisioning\transport\amqp\src\bin\Release\netstandard2.1\")+($assemblyRootNames[5]+".dll")),
    ((Join-Path -Path $repoRootPath -ChildPath "\provisioning\transport\mqtt\src\bin\Release\netstandard2.1\")+($assemblyRootNames[6]+".dll")),
    ((Join-Path -Path $repoRootPath -ChildPath "\provisioning\transport\http\src\bin\Release\netstandard2.1\")+($assemblyRootNames[7]+".dll")),
    ((Join-Path -Path $repoRootPath -ChildPath "\security\tpm\src\bin\Release\netstandard2.1\")+($assemblyRootNames[8]+".dll"))
)

# Create a list of the markdown files so we can compare them to the API doc directory
$markdownOutputFilePath = @(
    ($compareDirectory+ "\" + ($assemblyRootNames[0]+".md")),
    ($compareDirectory+ "\" + ($assemblyRootNames[1]+".md")),
    ($compareDirectory+ "\" + ($assemblyRootNames[2]+".md")),
    ($compareDirectory+ "\" + ($assemblyRootNames[3]+".md")),
    ($compareDirectory+ "\" + ($assemblyRootNames[4]+".md")),
    ($compareDirectory+ "\" + ($assemblyRootNames[5]+".md")),
    ($compareDirectory+ "\" + ($assemblyRootNames[6]+".md")),
    ($compareDirectory+ "\" + ($assemblyRootNames[7]+".md")),
    ($compareDirectory+ "\" + ($assemblyRootNames[8]+".md"))
)

# Set this to exit after we've displayed one or more error messages
$hasAFault = $false 

if ((Test-Path $compareDirectory) -ne $TRUE) {
    Write-Host "You have not cloned the sdk internals repository. Or it is not in the same root repository collection location."
    Write-Host "For example, if this SDK is cloned to c:\repos you should clone https://github.com/Azure/iot-sdks-internals.git to the c:\repos folder."
    Write-Host
    
    $hasAFault = $true
}

if ((Test-Path $asmToolExecutable) -ne $TRUE) {
    Write-Host "You do not have the required tool to check for SDK differences."
    Write-Host "Please get the AsmDiff tool from the dotnet arcade https://github.com/dotnet/arcade/tree/main/src/Microsoft.DotNet.AsmDiff. Clone this to the directory containing the SDK repo."
    Write-Host "For example, if this SDK is cloned to c:\repos you should clone https://github.com/dotnet/arcade.git to the c:\repos folder."
    Write-Host
    Write-Host "Once you clone the folder run dotnet build in the src/Microsoft.DotNet.AsmDiff folder and rerun this tool."
    Write-Host
    $hasAFault = $true
}

if ($hasAFault) {
    Write-Host "Please correct the above and rerun this tool."
    exit 1
}

# Get the last tag from the git repo and do the comparison
$lasttag = git describe --tags --abbrev=0  | tee -Variable lasttag
& git log --stat "$lasttag..HEAD" --output releaselog_detailed.txt
& git log --oneline "$lasttag..HEAD" --output releaselog_short.txt

Write-Host
Write-Host "Generated release log from tag $lasttag to the current HEAD."
Write-Host "The detailed log can be found by editing: releaselog_detailed.txt"
Write-Host "The short log can be found by editing: releaselog_short.txt"
Write-Host
Write-Host "NOTE: If there is a tag that you want to compare to that is earlier than the most recent tag you can run:"
Write-Host "git log --stat <tagversion>..HEAD --output releaselog_detailed.txt"
Write-Host "git log --oneline <tagversion>..HEAD --output releaselog_short.txt"
Write-Host


# Create a list of the markdown files so we can compare them to the API doc directory
for($assemblyIndex = 0; $assemblyIndex -lt $assemblyRootNames.length; $assemblyIndex++) { 
    if (Test-Path $assemblyFilePath[$assemblyIndex]) {
        
        $assemblyFileToUse = $assemblyFilePath[$assemblyIndex]
        $markdownOutputFileToUse = $markdownOutputFilePath[$assemblyIndex]
        
        # Get the original header from the file so we can apply it to the newly generated file.
        # Grabs the first 5 lines of the file which generally looks like this...
        # Azure SDK .NET Public API
        #
        # ## Microsoft.Azure.Devices.Client 1.35.*
        #
        # ```C
        $originalMarkdownHeader = Get-Content $markdownOutputFilePath[$assemblyIndex] | select -First 5

        # Permalink for AsmDiff README is: https://github.com/dotnet/arcade/blob/3aea914072c2f8844d7cf74c41c759b497e59b16/src/Microsoft.DotNet.AsmDiff/README.md
        #
        # These asmToolSwitches do the following
        # -os <filename>    Specifies the dll we want to generate the markdown for
        # -w Markdown       Tells the tool to generate Markdown output
        # -o <filename>     Specifies the name of the markdown file to output
        # -gba              Flattens the name spaces and removes the namespace headers from the output (ex. ## Microsoft.Azure.Devices.Client)
        Write-Host "Creating markdown for" $assemblyFileToUse
        $asmToolSwitches = "-os", $assemblyFileToUse, "-w", "Markdown", "-o", $markdownOutputFileToUse, "-gba"
        & $asmToolExecutable $asmToolSwitches
        
        # Replace the header for this file using the original header
        $newMarkdownBodyContent = Get-Content $markdownOutputFileToUse | Select-Object -Skip 5
        .{
            $originalMarkdownHeader
            $newMarkdownBodyContent
        } | Set-Content $markdownOutputFileToUse
    } else {
        Write-Host $assemblyFileToUse "does not exist. Skipping."
    }
}

## Add a blank line to make the output readable.
Write-Host

# Save old directory so we can return here when done running the compare
Push-Location

# Nav to the docs directory to run the comparison
Set-Location -Path $compareDirectory
 
# git diff --ignore-all-space --numstat generates the following output that will be parsed below
# This ignores any white space changes and will allow us to see a more concise view of the changes
# making the first pass analysis easier.
#
# The first number is the number of changes that were added to the file, the second number is the number of deletions from the file
#
# # https://git-scm.com/docs/git-diff 
#
# 9       3       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.Client.md
# 2       0       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.Provisioning.Client.md
# 2       0       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.Provisioning.Security.Tpm.md
# 2       0       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.Provisioning.Service.md
# 2       0       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.Provisioning.Transport.Amqp.md
# 2       0       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.Provisioning.Transport.Http.md
# 2       0       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.md
# 2       0       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.Shared.md
# 7       9       sdk_design_docs/CSharp/master/Microsoft.Azure.Devices.md
$gitDiffOutput = git diff --ignore-all-space --numstat

# If there is no output then the git diff command is run then we 
if ($gitDiffOutput -eq $null) {
    Write-Host "There were no changes in the API surface related to the comparison of the AsmDiff tool. Check the solutions to make sure there were not other changes that would affect the release and require a version update."
} else {
    Write-Host "Changes have been detected. Verify each file listed below to be sure of the scope of changes." 
    $changesAddedToFile = 0
    $changesDeletedFromFile = 0

    # Loop through all files and match the format above to detect if changes are made.
    foreach ($lineFromDiffOutput in $gitDiffOutput) {
        $_ = $lineFromDiffOutput -match "(?<changesAddedToFile>\d+)\s+(?<changesDeletedFromFile>\d+)\s+(?<fileName>.*)" 
        Write-Host "There have been" $Matches.changesDeletedFromFile "deletions and" $Matches.changesAddedToFile "additions to" $Matches.fileName 
    }
}
Write-Host
Write-Host "Finished generating the markdown files for comparison. Review the output above for release notes and to determine if there are version changes."

# Return to the old folder path
Pop-Location