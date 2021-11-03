# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#
.SYNOPSIS
Microsoft Azure IoT SDK Release comparison script

.DESCRIPTION
This script is used to compare the current candidate for release to the previous version that is in the iot-sdk-internals repository

Parameters:
    -AsmToolExecutable: The path to the AsmDiff tool found in the dotnet arcade (ex: c:\tools\asmdifftool)
    -SDKInternalsPath: The path of the iot-sdk-internals repository (ex: c:\repo\iot-sdks-internals)
	-IsPreview indicates you will compare the output to the last preview version

Prerequisites:
    AsmDiff - Used to create the markdown files we use
    dotnet tool install Microsoft.Dotnet.AsmDiff -g --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15

    SDK Internals repository - Place we store our markdown files
    git clone https://github.com/Azure/iot-sdks-internals.git

.EXAMPLE
.\diffapi

Executes the commands assuming everything is cloned relative to the SDK repository and the AsmDiff tool is compiled using the arcade instructions.
.EXAMPLE
.\diffapi -AsmToolExecutable c:\tools\asmdifftool

Specifies the location of the AsmDiff tool executable to run the diff commands.
.EXAMPLE
.\diffapi -SDKInternalsPath c:\repo\iot-sdks-internals

Specifies the location of the iot-sdk-internals repository if not cloned relative to the azure-iot-sdk-csharp repository.
.EXAMPLE
.\diffapi -IsPreview

Executes the commands assuming you have cloned the iot-sdk-internals repository to a directory relative to the azure-iot-sdk-csharp repository, and if you've installed the asmdiff tool.
.LINK
https://github.com/azure/azure-iot-sdk-csharp
#>

[CmdletBinding()]
Param(
    [ValidateScript( {
            if (-Not ($_ | Test-Path -PathType Leaf)) 
            {
                throw "File $_ does not exist."
            } 
            elseif ((Get-Command $_).Extension.ToLower() -ne '.exe') 
            {
                throw "File $_ is not an executable."
            }
            return $true
        })]
    # The executable path to the compiled AsmDiff tool found in the dotnet arcade (ex: c:\tools\asmdifftool\dotnet-asmdiff.exe)
    [System.IO.FileInfo] $AsmToolExecutable = $null,
    [ValidateScript( {
            if (-Not ($_ | Test-Path -PathType Container)) 
            {
                throw "Folder $_ does not exist."
            }
            return $true
        })]
    # The path of the iot-sdk-internals repository (ex: c:\repo\iot-sdks-internals)
    [System.IO.FileInfo] $SDKInternalsPath = $null,
    # Indicates you will compare the output to the last preview version instead of main
    [switch] $IsPreview
)

# Defaults for both the current repository and the repository parent directory
$repoRootPath = (Get-Item $pwd).Parent.Parent.FullName
$baseRootPath = (Get-Item $repoRootPath).Parent.FullName

Write-Verbose "Repository root path: $repoRootPath"
Write-Verbose "Repository base path: $baseRootPath"

# Release log file names
$releaseLogDetailed = "releaselog_detailed.txt"
$releaseLogShort = "releaselog_short.txt"

# First check to see if we've followed the guide
$asmToolExecutableCommand = Get-Command dotnet-asmdiff -ErrorAction SilentlyContinue
if ($null -eq $asmToolExecutableCommand)
{
    Write-Verbose "Unable to locate dotnet-asmdiff on the command line." 
}

if ($null -ne $AsmToolExecutable) 
{
    Write-Verbose "Using user supplied Asm Diff tool executable." 
    $asmToolExecutableCommand = $AsmToolExecutable
}

Write-Verbose "AsmDiff executable: $asmToolExecutableCommand"

# If the AsmDiff tool is not found we should explain how to get it and show how to specify the parameter
if ($null -eq $asmToolExecutableCommand)
{
    Write-Host -ForegroundColor Red "You do not have the required tool to check for SDK differences."
    Write-Host -ForegroundColor Red "Please get the AsmDiff tool from the dotnet arcade by installing it using the following command line:"
    Write-Host
    Write-Host -ForegroundColor Yellow "dotnet tool install Microsoft.Dotnet.AsmDiff -g --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15"
    Write-Host
    Write-Host -ForegroundColor Cyan -NoNewline "NOTE: The command above will install the AsmDiff tool " 
    Write-Host -ForegroundColor Black -BackgroundColor Cyan -NoNewline "globally"
    Write-Host -ForegroundColor Cyan " and will allow you to run the script without parameters." 
    Write-Host -ForegroundColor Cyan -NoNewline "NOTE: If you don't want it to be installed globally remove the "
    Write-Host -ForegroundColor White -NoNewline "-g " 
    Write-Host -ForegroundColor Cyan  -NoNewline "flag from the above command and specify the tool location with " 
    Write-Host -ForegroundColor White -NoNewline "-AsmToolExecutable" 
    Write-Host -ForegroundColor Cyan " parameter when running this script."
    Write-Host
    Write-Host -ForegroundColor Yellow "dotnet tool install Microsoft.Dotnet.AsmDiff --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15 --tool-path c:\tools\asmdiff"
    Write-Host -ForegroundColor Yellow -NoNewline ".\" 
    Write-Host -ForegroundColor Yellow $MyInvocation.MyCommand "-AsmToolExecutable c:\tools\asmdiff\dotnet-asmdiff.exe"
    Write-Host
    Write-Host -ForegroundColor Cyan "NOTE: This requires .NET Core 2.1 SDK or higher, but it is recommended to use .NET Core 3.1"
    Write-Host -ForegroundColor Cyan "NOTE: Install .NET Core 3.1 from here: https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-3.1.407-windows-x64-installer"
    Write-Host -ForegroundColor Cyan "NOTE: dotnet tool install help: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install"
    Write-Host
    $hasAFault = $TRUE;
}

# Set the path to the SDK Internals Repo, if we specify the path on the command line we will set it as such
$internalRootPath = ''
if ($null -ne $SDKInternalsPath) 
{
    Write-Verbose "Using user supplied iot-sdk-internals repository." 
    $internalRootPath = $SDKInternalsPath
} 
else 
{
    $internalRootPath = Join-Path -Path $baseRootPath -Child "\iot-sdks-internals"
}

Write-Verbose "Using $internalRootPath for the internals sdk repository base directory."

# If we specify to use the preview directory on the command line we will set it as such
$compareDirectory = Join-Path -Path $internalRootPath -Child "\sdk_design_docs\CSharp\main"
if ($IsPreview) 
{
    $compareDirectory = Join-Path -Path $internalRootPath -Child "\sdk_design_docs\CSharp\preview"
}

Write-Verbose "Directory where the SDK markdown files will be generated: $compareDirectory"

if ((Test-Path $compareDirectory) -ne $TRUE) 
{
    Write-Host
    Write-Host -ForegroundColor Red "The internals sdk repository does not have the expected directories."
    Write-Host -ForegroundColor Red "Please clone the internals repository or specify a location with the sdk_design_docs/CSharp/* path." 
    Write-Host -ForegroundColor Cyan "NOTE: You can clone the folder to the relative common repository root and you will not need to specify the path location."
    Write-Host
    Write-Host -ForegroundColor Yellow "git clone https://github.com/Azure/iot-sdks-internals.git $baseRootPath\iot-sdks-internals"
    Write-Host
    Write-Host -ForegroundColor Cyan -NoNewline "NOTE: You can also specify a location that is not relative to this repository and use the "
    Write-Host -ForegroundColor White -NoNewline "-SDKInternalsPath"
    Write-Host -ForegroundColor Cyan " parameter when running this script."
    Write-Host
    Write-Host -ForegroundColor Yellow "git clone https://github.com/Azure/iot-sdks-internals.git c:\mycustomfolder\iot-sdks-internals"
    Write-Host
    Write-Host -ForegroundColor Yellow -NoNewline ".\" 
    Write-Host -ForegroundColor Yellow $MyInvocation.MyCommand "-SDKInternalsPath c:\mycustomfolder\iot-sdks-internals"
    Write-Host
    $hasAFault = $TRUE;
}

# We can have TWO faults so instead of bailing out after each one we can show both
if ($hasAFault) 
{
    Write-Host -ForegroundColor Yellow "Please correct the above and rerun this tool."
    exit 1
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
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\shared\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[0] + ".dll"))),
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\iothub\device\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[1] + ".dll"))),
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\iothub\service\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[2] + ".dll"))),
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\provisioning\device\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[3] + ".dll"))),
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\provisioning\service\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[4] + ".dll"))),
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\provisioning\transport\amqp\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[5] + ".dll"))),
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\provisioning\transport\mqtt\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[6] + ".dll"))),
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\provisioning\transport\http\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[7] + ".dll"))),
    (Join-Path -Path $repoRootPath -ChildPath (Join-Path -Path "\security\tpm\src\bin\Release\netstandard2.1\" -ChildPath ($assemblyRootNames[8] + ".dll")))
)

# Get the last tag from the git repository and do the comparison
$lastTag = git describe --tags --abbrev=0  | Tee-Object -Variable lastTag

# Generate a couple of simple reports so we don't have to run these commands by hand
$detailedLog = git log --stat "$lastTag..HEAD"
$shortLog = git log --oneline "$lastTag..HEAD"

Out-File -InputObject $detailedLog $releaseLogDetailed
Out-File -InputObject $shortLog $releaseLogShort

Write-Verbose "Output from git describe --tags --abbrev=0"
Write-Verbose $lastTag

Write-Verbose "Output from git log --stat $lastTag..HEAD"
foreach ($outLine in $detailedLog) 
{
    Write-Verbose $outLine
}

Write-Verbose "Output from git log --oneline $lastTag..HEAD" 
foreach ($outLine in $shortLog) 
{
    Write-Verbose $outLine
}

Write-Host
Write-Host -ForegroundColor Magenta "Generated release log from tag $lastTag to the current HEAD."
Write-Host -ForegroundColor White "The detailed log can be found by editing:" (Get-ChildItem $releaseLogDetailed).FullName
Write-Host -ForegroundColor White "The short log can be found by editing:"  (Get-ChildItem $releaseLogShort).FullName
Write-Host
Write-Host -ForegroundColor Cyan "NOTE: If there is a tag that you want to compare to that is earlier than the most recent tag you can run:"
Write-Host -ForegroundColor Yellow "git log --stat <tagversion>..HEAD --output releaselog_detailed.txt"
Write-Host -ForegroundColor Yellow "git log --oneline <tagversion>..HEAD --output releaselog_short.txt"
Write-Host

# Create a list of the markdown files so we can compare them to the API doc directory
for ($assemblyIndex = 0; $assemblyIndex -lt $assemblyRootNames.length; $assemblyIndex++) 
{ 
    # Get assembly file names from array above
    $assemblyFileToUse = $assemblyFilePath[$assemblyIndex]
    # Create a the markdown file path so we can compare them to the API doc directory
    $markdownOutputFileToUse = Join-Path -Path $compareDirectory -ChildPath ($assemblyRootNames[$assemblyIndex] + ".md")
    
    if ((Test-Path $assemblyFileToUse) -eq $FALSE) 
    { 
        Write-Host $assemblyFileToUse "does not exist. Skipping."
        continue;
    } 
        
    # Get the original header from the file so we can apply it to the newly generated file.
    # Grabs the first 5 lines of the file which generally looks like this...
    # Azure SDK .NET Public API
    #
    # ## Microsoft.Azure.Devices.Client 1.35.*
    #
    # ```C
    $originalMarkdownHeader = Get-Content $markdownOutputFileToUse | Select-Object -First 5
    Write-Verbose "Original markdown header to replace in new file"

    foreach ($outLine in $originalMarkdownHeader) 
    {
        Write-Verbose $outLine
    }
        
    # Permalink for AsmDiff README is: https://github.com/dotnet/arcade/blob/3aea914072c2f8844d7cf74c41c759b497e59b16/src/Microsoft.DotNet.AsmDiff/README.md
    #
    # These asmToolSwitches do the following
    # -os <filename>    Specifies the dll we want to generate the markdown for
    # -w Markdown       Tells the tool to generate Markdown output
    # -o <filename>     Specifies the name of the markdown file to output
    # -gba              Flattens the name spaces and removes the namespace headers from the output (ex. ## Microsoft.Azure.Devices.Client)
    Write-Host -ForegroundColor Magenta "Creating markdown for $assemblyFileToUse"
    
    $asmToolSwitches = "-os", $assemblyFileToUse, "-w", "Markdown", "-o", $markdownOutputFileToUse, "-gba"
    & $asmToolExecutableCommand $asmToolSwitches
        
    # Replace the header for this file using the original header
    $newMarkdownBodyContent = Get-Content $markdownOutputFileToUse | Select-Object -Skip 5
    . {
        $originalMarkdownHeader
        $newMarkdownBodyContent
    } | Set-Content $markdownOutputFileToUse
    
}

## Add a blank line to make the output readable.
Write-Host

# Save old directory so we can return here when done running the compare
Push-Location

# Nav to the docs directory to run the comparison
Write-Verbose "Changing from $pwd to $compareDirectory"
Set-Location -Path $compareDirectory
 
# git diff --ignore-all-space --numstat generates the following output that will be parsed below
# This ignores any white space changes and will allow us to see a more concise view of the changes
# making the first pass analysis easier.
#
# The first number is the number of changes that were added to the file, the second number is the number of deletions from the file
#
# # https://git-scm.com/docs/git-diff 
#
# 9       3       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Client.md
# 2       0       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Client.md
# 2       0       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Security.Tpm.md
# 2       0       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Service.md
# 2       0       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Transport.Amqp.md
# 2       0       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Transport.Http.md
# 2       0       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.md
# 2       0       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Shared.md
# 7       9       sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.md
$gitDiffOutput = git diff --ignore-all-space --numstat
Write-Verbose "Output off git diff --ignore-all-space --numstat"

foreach ($outLine in $gitDiffOutput) 
{
    Write-Verbose $outLine
}

# If there is no output then the git diff command is run then we 
if ($null -eq $gitDiffOutput) 
{
    Write-Host -ForegroundColor Green "There were no changes in the API surface related to the comparison of the AsmDiff tool. Check the solutions to make sure there were not other changes that would affect the release and require a version update."
} 
else 
{
    Write-Host -ForegroundColor White "Changes have been detected. Verify each file listed below to be sure of the scope of changes." 

    # Loop through all files and match the format above to detect if changes are made.
    foreach ($lineFromDiffOutput in $gitDiffOutput) 
    {
        $lineFromDiffOutput -match "(?<changesAddedToFile>\d+)\s+(?<changesDeletedFromFile>\d+)\s+(?<fileName>.*)" | Out-Null
        Write-Host -NoNewline "There have been " 
        Write-Host -NoNewline -ForegroundColor Red $Matches.changesDeletedFromFile "deletions" 
        Write-Host -NoNewline " and "
        Write-Host -NoNewline -ForegroundColor Green $Matches.changesAddedToFile "additions"
        Write-Host " to" $Matches.fileName 
    }
}

# Display ending message
Write-Host
Write-Host -ForegroundColor Cyan "Finished generating the markdown files for comparison. Review the output above for release notes and to determine if there are version changes."

# Return to the old folder path
Pop-Location
Write-Verbose "Changed back to $pwd"
Write-Host