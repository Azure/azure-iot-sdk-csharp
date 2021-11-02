# CSharp SDK Diff Automation

## Overview
During the release process we need to examine the changes to the SDK surface to make sure we're not making breaking or unintended changes. This tool will automate the process of generating the mark down files so we do not need to use the GUI version which is time consuming.


## Quickstart
This quickstart assumes that you clone your repositories to a central repository root folder `c:\repos` and that you've already cloned the CSharp SDK to `c:\repos\azure-iot-sdk-csharp`. You can change out `c:\repos` with whatever central repository root folder you prefer.

```ps
cd c:\repos
git clone https://github.com/Azure/iot-sdks-internals.git
dotnet tool install Microsoft.Dotnet.AsmDiff -g --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15
cd c:\repos\azure-iot-sdk-csharp\tools\difftools
.\diffapi.ps1
```

# Prerequisites

## git
You most likely have this if you're working with a cloned repository.

### Links
[git](https://git-scm.com/download/win)

## Cloned SDK Internals Repository
In order to make the comparisons you will need to clone the [internals repo](https://github.com/Azure/iot-sdks-internals) to a location on your local machine.

## dotnet core SDK (>=2.1)
You should already have this as part of your dev build since we use this to build our sources. However you can download them from the following locations.

### Links

[dotnetcore LTS](https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-3.1.407-windows-x64-installer)

## AsmDiff
This is the tool that is used to create the mark down and it is written by the dotnet team and is located in the [dotnet/arcade](https://github.com/dotnet/arcade) repository. To install this tool run the following:

```
dotnet tool install Microsoft.Dotnet.AsmDiff -g --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15
```

### Links

[AsmDiff GitHub](https://github.com/dotnet/arcade/tree/main/src/Microsoft.DotNet.AsmDiff)

[AsmDiff NuGet](https://dev.azure.com/dnceng/public/_packaging?_a=package&feed=dotnet-eng&view=overview&package=Microsoft.DotNet.AsmDiff&version=6.0.0-beta.21161.15&protocolType=NuGet)


# Examples

## Using Specific SDK Internals Repository Directory
If you don't clone your repositories to a central location you can specify the directory with the `-SDKInternalsPath` command line.

```
dotnet tool install Microsoft.Dotnet.AsmDiff -g --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15

git clone https://github.com/Azure/iot-sdks-internals.git d:\internals
cd c:\repos\azure-iot-sdk-csharp\tools\difftools 
.\diffapi.ps1 -SDKInternalsPath d:\internals
```

## Using Specific SDK Internals Repository Directory Against Preview
If you don't clone your repositories to a central location you can specify the directory with the `-SDKInternalsPath` command line and the `-IsPreview` command line.

```
dotnet tool install Microsoft.Dotnet.AsmDiff -g --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15

git clone https://github.com/Azure/iot-sdks-internals.git d:\internals
cd c:\repos\azure-iot-sdk-csharp\tools\difftools 
.\diffapi.ps1 -SDKInternalsPath d:\internals -IsPreview
```

## Using Specific SDK Internals Repository Directory and Custom AsmDiff location
If you don't clone your repositories to a central location you can specify the directory with the `-SDKInternalsPath` command line, the `-AsmToolExecutable` command line, and the `-IsPreview` command line.

```
dotnet tool install Microsoft.Dotnet.AsmDiff -g --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15 --tool-path c:\tools\asmdiff

git clone https://github.com/Azure/iot-sdks-internals.git d:\internals
cd c:\repos\azure-iot-sdk-csharp\tools\difftools 
.\diffapi.ps1 -SDKInternalsPath d:\internals -AsmToolExecutable c:\tools\asmdiff\dotnet-asmdiff.exe
```

# Debugging and Troubleshooting

## Start Here

1. Run `Get-Help diffapi`
    - Review the help doc and examples
2. AsmDiff tool is not installed globally
    - Revisit the section above on how to install
    - Install globally
    - Use the `-AsmToolExecutable` option
3. The internals repository is not relative to the sdk repository
    - Clone the repo relative to your CSharp SDK
        - c:\repos\azure-iot-sdk-csharp
        - c:\repos\iot-sdks-internals
    - Use the `-SDKInternalsPath` option

## Running the command with Verbose output
Use this to explore the output of the script without debugging it. This will be very verbose but should help you understand where the tool is looking.

```
❯ .\diffapi.ps1 -SDKInternalsPath C:\adtexplorer\ -Verbose
VERBOSE: Repository root path: C:\repos\azure-iot-sdk-csharp
VERBOSE: Repository base path: C:\repos
VERBOSE: AsmDiff executable: dotnet-asmdiff.exe
VERBOSE: Using user supplied iot-sdk-internals repository.
VERBOSE: Using C:\adtexplorer\ for the internals sdk repository base directory.
VERBOSE: Directory where the SDK markdown files will be generated: C:\adtexplorer\sdk_design_docs\CSharp\main
...
...
```

## Internals Directory Not Found

If you see this you've specified a directory that doesn't exist. Specify a directory that exists and has the internals repository cloned to it.

```
❯ .\diffapi.ps1 -SDKInternalsPath c:\baddirectory\
C:\repos\azure-iot-sdk-csharp\tools\diffscripts\diffapi.ps1 : Cannot validate argument on parameter 'SDKInternalsPath'. Folder c:\baddirectory\ does not exist.
At line:1 char:33
+ .\diffapi.ps1 -SDKInternalsPath c:\baddirectory\
+                                 ~~~~~~~~~~~~~~~~
    + CategoryInfo          : InvalidData: (:) [diffapi.ps1], ParameterBindingValidationException
    + FullyQualifiedErrorId : ParameterArgumentValidationError,diffapi.ps1
```

## Internals Directory Can't Find Markdown Location

If you see this you've specified a directory that does exist but does not have the internals repository cloned to it. Try cloning the repository again or specify the correct location.

```
❯ .\diffapi.ps1 -SDKInternalsPath C:\adtexplorer\

The internals sdk repository does not have the expected directories.
Please clone the internals repository or specify a location with the sdk_design_docs/CSharp/* path.
NOTE: You can clone the folder to the relative common repository root and you will not need to specify the path location.

git clone https://github.com/Azure/iot-sdks-internals.git C:\repos\iot-sdks-internals

NOTE: You can also specify a location that is not relative to this repository and use the -SDKInternalsPath parameter when running this script.

git clone https://github.com/Azure/iot-sdks-internals.git c:\mycustomfolder\iot-sdks-internals

.\diffapi.ps1 -SDKInternalsPath c:\mycustomfolder\iot-sdks-internals

Please correct the above and rerun this tool.
```

## AsmDiff Tool Missing 
```
❯ .\diffapi.ps1
You do not have the required tool to check for SDK differences.
Please get the AsmDiff tool from the dotnet arcade by installing it using the following command line:

dotnet tool install Microsoft.Dotnet.AsmDiff -g --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15

NOTE: The command above will install the AsmDiff tool globally and will allow you to run the script without parameters.
NOTE: If you don't want it to be installed globally remove the -g flag from the above command and specify the tool location with -AsmToolExecutable parameter when running this script.

dotnet tool install Microsoft.Dotnet.AsmDiff --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json --version 6.0.0-beta.21161.15 --tool-path c:\tools\asmdiff
.\diffapi.ps1 -AsmToolExecutable c:\tools\asmdiff\dotnet-asmdiff.exe

NOTE: This requires .NET Core 2.1 SDK or higher, but it is recommended to use .NET Core 3.1
NOTE: Install .NET Core 3.1 from here: https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-3.1.407-windows-x64-installer
NOTE: dotnet tool install help: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install

Please correct the above and rerun this tool.
```

## Good Run
```
❯ .\diffapi.ps1

Generated release log from tag 2021-02-08 to the current HEAD.
The detailed log can be found by editing: C:\repos\azure-iot-sdk-csharp\tools\diffscripts\releaselog_detailed.txt
The short log can be found by editing: C:\repos\azure-iot-sdk-csharp\tools\diffscripts\releaselog_short.txt

NOTE: If there is a tag that you want to compare to that is earlier than the most recent tag you can run:
git log --stat <tagversion>..HEAD --output releaselog_detailed.txt
git log --oneline <tagversion>..HEAD --output releaselog_short.txt

Creating markdown for C:\repos\azure-iot-sdk-csharp\shared\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.Shared.dll
Creating markdown for C:\repos\azure-iot-sdk-csharp\iothub\device\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.Client.dll
Creating markdown for C:\repos\azure-iot-sdk-csharp\iothub\service\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.dll
Creating markdown for C:\repos\azure-iot-sdk-csharp\provisioning\device\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.Provisioning.Client.dll
Creating markdown for C:\repos\azure-iot-sdk-csharp\provisioning\service\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.Provisioning.Service.dll
Creating markdown for C:\repos\azure-iot-sdk-csharp\provisioning\transport\amqp\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.Provisioning.Transport.Amqp.dll
Creating markdown for C:\repos\azure-iot-sdk-csharp\provisioning\transport\mqtt\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.dll
Creating markdown for C:\repos\azure-iot-sdk-csharp\provisioning\transport\http\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.Provisioning.Transport.Http.dll
Creating markdown for C:\repos\azure-iot-sdk-csharp\security\tpm\src\bin\Release\netstandard2.1\Microsoft.Azure.Devices.Provisioning.Security.Tpm.dll

Changes have been detected. Verify each file listed below to be sure of the scope of changes.
There have been 3 deletions and 9 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Client.md
There have been 0 deletions and 2 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Client.md
There have been 0 deletions and 2 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Security.Tpm.md
There have been 0 deletions and 2 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Service.md
There have been 0 deletions and 2 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Transport.Amqp.md
There have been 0 deletions and 2 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Transport.Http.md
There have been 0 deletions and 2 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.md
There have been 0 deletions and 2 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.Shared.md
There have been 9 deletions and 7 additions to sdk_design_docs/CSharp/main/Microsoft.Azure.Devices.md

Finished generating the markdown files for comparison. Review the output above for release notes and to determine if there are version changes.
```